using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using FileTransfer.Core.Contracts;
using FileTransfer.Core.Models;
using FileTransfer.Infrastructure.Transfer.Protocol;

namespace FileTransfer.Infrastructure.Transfer;

public sealed class TransferReceiverHost : ITransferReceiverHost
{
    private readonly TransferProtocolSerializer _serializer;
    private readonly ISettingsService _settingsService;
    private readonly TransferProtocolOptions _options;
    private readonly X509Certificate2 _certificate;
    private readonly List<Task> _connectionTasks;

    private TcpListener? _listener;
    private CancellationTokenSource? _acceptLoopCancellation;
    private Task? _acceptLoopTask;

    public TransferReceiverHost(
        TransferProtocolSerializer serializer,
        ISettingsService settingsService,
        LocalCertificateStore certificateStore,
        TransferProtocolOptions options)
    {
        _serializer = serializer;
        _settingsService = settingsService;
        _options = options;
        _certificate = certificateStore.GetOrCreateServerCertificate();
        _connectionTasks = [];
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_listener is not null)
        {
            return Task.CompletedTask;
        }

        _listener = new TcpListener(IPAddress.Any, _options.Port);
        _listener.Start();

        _acceptLoopCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _acceptLoopTask = Task.Run(() => AcceptLoopAsync(_acceptLoopCancellation.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_listener is null)
        {
            return;
        }

        _acceptLoopCancellation?.Cancel();
        _listener.Stop();
        _listener = null;

        if (_acceptLoopTask is not null)
        {
            await _acceptLoopTask.WaitAsync(cancellationToken);
        }

        Task[] pendingConnections;
        lock (_connectionTasks)
        {
            pendingConnections = _connectionTasks.ToArray();
        }

        await Task.WhenAll(pendingConnections).WaitAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        _acceptLoopCancellation?.Dispose();
        _certificate.Dispose();
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient? client = null;
            try
            {
                client = await _listener!.AcceptTcpClientAsync(cancellationToken);
                Task task = HandleClientAsync(client, cancellationToken);
                lock (_connectionTasks)
                {
                    _connectionTasks.Add(task);
                }

                _ = task.ContinueWith(completedTask =>
                {
                    lock (_connectionTasks)
                    {
                        _connectionTasks.Remove(completedTask);
                    }
                }, CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch
            {
                client?.Dispose();
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        await using (NetworkStream networkStream = client.GetStream())
        await using (SslStream sslStream = new(networkStream, false))
        {
            await sslStream.AuthenticateAsServerAsync(_certificate, clientCertificateRequired: false, checkCertificateRevocation: false);

            HandshakeMessage handshake = await _serializer.ReadJsonFrameAsync<HandshakeMessage>(sslStream, TransferFrameType.Handshake, cancellationToken);
            if (handshake.ProtocolVersion != 1)
            {
                await _serializer.WriteJsonFrameAsync(
                    sslStream,
                    TransferFrameType.Error,
                    new { message = "Unsupported protocol version." },
                    cancellationToken);
                return;
            }

            ManifestMessage manifest = await _serializer.ReadJsonFrameAsync<ManifestMessage>(sslStream, TransferFrameType.Manifest, cancellationToken);
            AppSettings settings = await _settingsService.GetAsync(cancellationToken);
            string destinationPath = BuildSafeDestinationPath(settings.DownloadFolder, manifest.RelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

            await using FileStream destinationStream = new(
                destinationPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: TransferProtocolOptions.DefaultChunkBytes,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            while (true)
            {
                (TransferFrameType frameType, byte[] payload) = await _serializer.ReadFrameAsync(sslStream, cancellationToken);
                switch (frameType)
                {
                    case TransferFrameType.Chunk:
                        await destinationStream.WriteAsync(payload, cancellationToken);
                        break;
                    case TransferFrameType.FileComplete:
                        await destinationStream.FlushAsync(cancellationToken);
                        break;
                    case TransferFrameType.SessionComplete:
                        return;
                    case TransferFrameType.Cancel:
                        return;
                    default:
                        throw new InvalidDataException($"Unexpected frame type {frameType}.");
                }
            }
        }
    }

    private static string BuildSafeDestinationPath(string downloadRoot, string relativePath)
    {
        string normalizedRelativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        string combinedPath = Path.GetFullPath(Path.Combine(downloadRoot, normalizedRelativePath));
        string normalizedRoot = Path.GetFullPath(downloadRoot);

        if (!combinedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Transfer path escapes the configured download directory.");
        }

        return combinedPath;
    }
}
