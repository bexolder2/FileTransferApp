using FileTransfer.Core.Contracts;
using FileTransfer.Core.Models;
using System.Net;
using System.Net.Sockets;

namespace FileTransfer.Infrastructure.Settings;

public sealed class SettingsService : ISettingsService
{
    private readonly ISettingsStore _settingsStore;

    public SettingsService(ISettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
    }

    public async Task<AppSettings> GetAsync(CancellationToken cancellationToken)
    {
        AppSettings? stored = await _settingsStore.LoadAsync(cancellationToken);
        AppSettings normalized = Normalize(stored);

        if (stored is null || !AreEquivalent(stored, normalized))
        {
            await _settingsStore.SaveAsync(normalized, cancellationToken);
        }

        return normalized;
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        AppSettings normalized = Normalize(settings);
        await _settingsStore.SaveAsync(normalized, cancellationToken);
    }

    private static bool AreEquivalent(AppSettings first, AppSettings second)
    {
        return first.Version == second.Version
            && string.Equals(first.DownloadFolder, second.DownloadFolder, StringComparison.Ordinal)
            && first.MaximumParallelUploads == second.MaximumParallelUploads
            && string.Equals(first.LastSelectedTargetIp, second.LastSelectedTargetIp, StringComparison.Ordinal)
            && first.ThemeMode == second.ThemeMode
            && first.PreviouslyScannedIps.SequenceEqual(second.PreviouslyScannedIps)
            && first.TrustedPeerFingerprints.SequenceEqual(second.TrustedPeerFingerprints)
            && first.TrustedPeers.Count == second.TrustedPeers.Count
            && first.TrustedPeers.Zip(second.TrustedPeers, (a, b) => a.PeerId == b.PeerId && a.Fingerprint == b.Fingerprint).All(x => x);
    }

    private static AppSettings Normalize(AppSettings? settings)
    {
        string defaultDownloads = GetDefaultDownloadsFolder();
        string? downloadFolder = settings?.DownloadFolder;

        if (string.IsNullOrWhiteSpace(downloadFolder))
        {
            downloadFolder = defaultDownloads;
        }

        int maxParallelUploads = settings?.MaximumParallelUploads ?? 2;
        if (maxParallelUploads < 1)
        {
            maxParallelUploads = 1;
        }

        return new AppSettings
        {
            Version = Math.Max(2, settings?.Version ?? 2),
            DownloadFolder = downloadFolder,
            MaximumParallelUploads = maxParallelUploads,
            LastSelectedTargetIp = settings?.LastSelectedTargetIp,
            ThemeMode = settings?.ThemeMode ?? AppThemeMode.System,
            PreviouslyScannedIps = NormalizeScannedIps(settings),
            TrustedPeerFingerprints = settings?.TrustedPeerFingerprints?.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? [],
            TrustedPeers = NormalizeTrustedPeers(settings)
        };
    }

    private static IReadOnlyList<string> NormalizeScannedIps(AppSettings? settings)
    {
        if (settings?.PreviouslyScannedIps is null)
        {
            return [];
        }

        List<string> normalized = [];
        HashSet<string> seen = new(StringComparer.Ordinal);

        foreach (string ip in settings.PreviouslyScannedIps)
        {
            if (string.IsNullOrWhiteSpace(ip))
            {
                continue;
            }

            if (!IPAddress.TryParse(ip, out IPAddress? parsed) || parsed.AddressFamily != AddressFamily.InterNetwork)
            {
                continue;
            }

            string canonical = parsed.ToString();
            if (seen.Add(canonical))
            {
                normalized.Add(canonical);
            }
        }

        return normalized;
    }

    private static IReadOnlyList<TrustedPeer> NormalizeTrustedPeers(AppSettings? settings)
    {
        List<TrustedPeer> normalized = [];
        HashSet<string> seenPeerIds = new(StringComparer.OrdinalIgnoreCase);

        if (settings?.TrustedPeers is not null)
        {
            foreach (TrustedPeer peer in settings.TrustedPeers)
            {
                if (string.IsNullOrWhiteSpace(peer.PeerId) || string.IsNullOrWhiteSpace(peer.Fingerprint))
                {
                    continue;
                }

                if (seenPeerIds.Add(peer.PeerId))
                {
                    normalized.Add(new TrustedPeer
                    {
                        PeerId = peer.PeerId,
                        Fingerprint = peer.Fingerprint.ToUpperInvariant()
                    });
                }
            }
        }

        return normalized;
    }

    private static string GetDefaultDownloadsFolder()
    {
        string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(profile, "Downloads");
    }
}
