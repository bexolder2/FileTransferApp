using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FileTransfer.Core.Contracts;
using FileTransfer.Core.Models;

namespace FileTransfer.Infrastructure.Security;

public sealed class TrustOnFirstUseService : ITrustOnFirstUseService
{
    private readonly ISettingsService _settingsService;
    private readonly object _syncLock;

    public TrustOnFirstUseService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        _syncLock = new object();
    }

    public bool ValidateServerCertificate(string peerId, X509Certificate2? certificate, out string? errorMessage)
    {
        errorMessage = null;
        if (certificate is null)
        {
            errorMessage = "The remote peer did not provide a certificate.";
            return false;
        }

        string normalizedPeerId = NormalizePeerId(peerId);
        string fingerprint = certificate.GetCertHashString(HashAlgorithmName.SHA256);

        lock (_syncLock)
        {
            AppSettings settings = _settingsService.GetAsync(CancellationToken.None).GetAwaiter().GetResult();

            TrustedPeer? existingPeerTrust = settings.TrustedPeers.FirstOrDefault(peer =>
                string.Equals(peer.PeerId, normalizedPeerId, StringComparison.OrdinalIgnoreCase));

            if (existingPeerTrust is not null)
            {
                if (string.Equals(existingPeerTrust.Fingerprint, fingerprint, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                errorMessage = $"Certificate mismatch for peer {normalizedPeerId}. Explicit re-trust is required.";
                return false;
            }

            bool knownLegacyFingerprint = settings.TrustedPeerFingerprints.Any(value =>
                string.Equals(value, fingerprint, StringComparison.OrdinalIgnoreCase));

            List<string> fingerprints = settings.TrustedPeerFingerprints
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            List<TrustedPeer> peers = settings.TrustedPeers.ToList();
            peers.Add(new TrustedPeer
            {
                PeerId = normalizedPeerId,
                Fingerprint = fingerprint
            });

            if (!knownLegacyFingerprint)
            {
                fingerprints.Add(fingerprint);
            }

            AppSettings updated = new()
            {
                Version = settings.Version,
                DownloadFolder = settings.DownloadFolder,
                MaximumParallelUploads = settings.MaximumParallelUploads,
                LastSelectedTargetIp = settings.LastSelectedTargetIp,
                ThemeMode = settings.ThemeMode,
                TrustedPeerFingerprints = fingerprints.ToArray(),
                TrustedPeers = peers.ToArray()
            };

            _settingsService.SaveAsync(updated, CancellationToken.None).GetAwaiter().GetResult();
            return true;
        }
    }

    public async Task RemoveTrustForPeerAsync(string peerId, CancellationToken cancellationToken)
    {
        string normalizedPeerId = NormalizePeerId(peerId);

        AppSettings settings = await _settingsService.GetAsync(cancellationToken);
        TrustedPeer[] remaining = settings.TrustedPeers
            .Where(peer => !string.Equals(peer.PeerId, normalizedPeerId, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (remaining.Length == settings.TrustedPeers.Count)
        {
            return;
        }

        AppSettings updated = new()
        {
            Version = settings.Version,
            DownloadFolder = settings.DownloadFolder,
            MaximumParallelUploads = settings.MaximumParallelUploads,
            LastSelectedTargetIp = settings.LastSelectedTargetIp,
            ThemeMode = settings.ThemeMode,
            TrustedPeerFingerprints = settings.TrustedPeerFingerprints,
            TrustedPeers = remaining
        };

        await _settingsService.SaveAsync(updated, cancellationToken);
    }

    private static string NormalizePeerId(string peerId)
    {
        return peerId.Trim().ToLowerInvariant();
    }
}
