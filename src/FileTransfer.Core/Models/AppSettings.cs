namespace FileTransfer.Core.Models;

public sealed class AppSettings
{
    public int Version { get; init; } = 2;

    public string DownloadFolder { get; init; } = string.Empty;

    public int MaximumParallelUploads { get; init; } = 2;

    public string? LastSelectedTargetIp { get; init; }

    public AppThemeMode ThemeMode { get; init; } = AppThemeMode.System;

    public IReadOnlyList<string> TrustedPeerFingerprints { get; init; } = [];

    public IReadOnlyList<TrustedPeer> TrustedPeers { get; init; } = [];
}
