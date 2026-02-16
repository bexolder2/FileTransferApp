using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using FileTransfer.Core.Contracts;
using FileTransfer.Core.Models;
using FileTransfer.Infrastructure.Transfer.Protocol;

namespace FileTransfer.Infrastructure.Transfer;

public sealed class TransferClient : ITransferClient
{
    private readonly TransferProtocolSerializer _serializer;
    private readonly ITrustOnFirstUseService _trustService;

    public TransferClient(TransferProtocolSerializer serializer, ITrustOnFirstUseService trustService)
    {
        _serializer = serializer;
        _trustService = trustService;
    }

    public async Task SendFileAsync(
        string targetIpAddress,
        int port,
        ResolvedTransferFile file,
        IProgress<long> bytesProgress,
        CancellationToken cancellationToken)
    {
        using TcpClient tcpClient = new();
        await tcpClient.ConnectAsync(targetIpAddress, port, cancellationToken);

        await using NetworkStream networkStream = tcpClient.GetStream();
        await using SslStream sslStream = new(networkStream, false, ValidateRemoteCertificate);
        SslClientAuthenticationOptions options = new()
        {
            TargetHost = targetIpAddress,
            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
            CertificateRevocationCheckMode = X509RevocationMode.NoCheck
        };
        await sslStream.AuthenticateAsClientAsync(options, cancellationToken);

        await _serializer.WriteJsonFrameAsync(sslStream, TransferFrameType.Handshake, new HandshakeMessage(), cancellationToken);

        ManifestMessage manifest = new()
        {
            RelativePath = file.RelativePath.Replace('\\', '/'),
            Length = file.Length
        };
        await _serializer.WriteJsonFrameAsync(sslStream, TransferFrameType.Manifest, manifest, cancellationToken);

        int chunkSize = file.Length > TransferProtocolOptions.LargeFileThresholdBytes
            ? TransferProtocolOptions.LargeFileChunkBytes
            : TransferProtocolOptions.DefaultChunkBytes;

        byte[] buffer = new byte[chunkSize];
        await using FileStream fileStream = new(
            file.SourcePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: chunkSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        while (true)
        {
            int read = await fileStream.ReadAsync(buffer.AsMemory(0, chunkSize), cancellationToken);
            if (read <= 0)
            {
                break;
            }

            await _serializer.WriteFrameAsync(sslStream, TransferFrameType.Chunk, buffer.AsMemory(0, read), cancellationToken);
            bytesProgress.Report(read);
        }

        await _serializer.WriteFrameAsync(sslStream, TransferFrameType.FileComplete, ReadOnlyMemory<byte>.Empty, cancellationToken);
        await _serializer.WriteFrameAsync(sslStream, TransferFrameType.SessionComplete, ReadOnlyMemory<byte>.Empty, cancellationToken);
        await sslStream.FlushAsync(cancellationToken);

        bool ValidateRemoteCertificate(object _, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors policyErrors)
        {
            X509Certificate2? certificate2 = certificate switch
            {
                null => null,
                X509Certificate2 direct => direct,
                _ => X509CertificateLoader.LoadCertificate(certificate.Export(X509ContentType.Cert))
            };
            bool isTrusted = _trustService.ValidateServerCertificate(targetIpAddress, certificate2, out string? errorMessage);
            if (!isTrusted)
            {
                throw new AuthenticationException(errorMessage ?? "TLS trust validation failed.");
            }

            return true;
        }
    }
}
