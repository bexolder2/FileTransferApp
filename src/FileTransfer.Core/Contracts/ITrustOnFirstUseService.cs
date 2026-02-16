using System.Security.Cryptography.X509Certificates;

namespace FileTransfer.Core.Contracts;

public interface ITrustOnFirstUseService
{
    bool ValidateServerCertificate(string peerId, X509Certificate2? certificate, out string? errorMessage);

    Task RemoveTrustForPeerAsync(string peerId, CancellationToken cancellationToken);
}
