using FileTransfer.Core.Contracts;
using FileTransfer.Core.Models;

namespace FileTransfer.Infrastructure.Activation;

public sealed class ProtocolActivationService : IProtocolActivationService
{
    private const string Scheme = "filetransfer";
    private readonly ISettingsService _settingsService;

    public ProtocolActivationService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task<bool> ApplyActivationAsync(string[] args, CancellationToken cancellationToken)
    {
        string? activationArg = args.FirstOrDefault(IsProtocolUriArgument);
        if (activationArg is null)
        {
            return false;
        }

        if (!Uri.TryCreate(activationArg, UriKind.Absolute, out Uri? uri))
        {
            return false;
        }

        string? targetIp = ExtractTargetIp(uri);
        if (string.IsNullOrWhiteSpace(targetIp))
        {
            return false;
        }

        AppSettings settings = await _settingsService.GetAsync(cancellationToken);
        AppSettings updated = new()
        {
            Version = settings.Version,
            DownloadFolder = settings.DownloadFolder,
            MaximumParallelUploads = settings.MaximumParallelUploads,
            LastSelectedTargetIp = targetIp,
            ThemeMode = settings.ThemeMode,
            TrustedPeerFingerprints = settings.TrustedPeerFingerprints,
            TrustedPeers = settings.TrustedPeers
        };

        await _settingsService.SaveAsync(updated, cancellationToken);
        return true;
    }

    private static bool IsProtocolUriArgument(string value)
    {
        return value.StartsWith($"{Scheme}://", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ExtractTargetIp(Uri uri)
    {
        if (!string.Equals(uri.Scheme, Scheme, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string? targetFromQuery = ParseQueryParam(uri.Query, "target");
        if (!string.IsNullOrWhiteSpace(targetFromQuery))
        {
            return targetFromQuery;
        }

        if (!string.IsNullOrWhiteSpace(uri.Host))
        {
            return uri.Host;
        }

        return null;
    }

    private static string? ParseQueryParam(string query, string key)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        string normalized = query.TrimStart('?');
        foreach (string part in normalized.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] pair = part.Split('=', 2);
            if (pair.Length != 2)
            {
                continue;
            }

            if (!string.Equals(Uri.UnescapeDataString(pair[0]), key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string value = Uri.UnescapeDataString(pair[1]);
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        return null;
    }
}
