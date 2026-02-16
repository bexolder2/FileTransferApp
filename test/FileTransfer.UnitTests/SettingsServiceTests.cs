using FileTransfer.Core.Contracts;
using FileTransfer.Core.Models;
using FileTransfer.Infrastructure.Settings;

namespace FileTransfer.UnitTests;

public sealed class SettingsServiceTests
{
    [Fact]
    public async Task GetAsync_WhenStoreEmpty_ReturnsNormalizedDefaults()
    {
        InMemorySettingsStore store = new(null);
        SettingsService service = new(store);

        AppSettings settings = await service.GetAsync(CancellationToken.None);

        Assert.Equal(2, settings.Version);
        Assert.Equal(2, settings.MaximumParallelUploads);
        Assert.EndsWith("Downloads", settings.DownloadFolder, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(settings.TrustedPeers);
    }

    [Fact]
    public async Task SaveAsync_WhenParallelUploadsBelowOne_NormalizesValue()
    {
        InMemorySettingsStore store = new(null);
        SettingsService service = new(store);

        await service.SaveAsync(new AppSettings
        {
            Version = 2,
            DownloadFolder = "C:\\Temp",
            MaximumParallelUploads = 0,
            LastSelectedTargetIp = "127.0.0.1",
            ThemeMode = AppThemeMode.Dark,
            TrustedPeerFingerprints = [],
            TrustedPeers = []
        }, CancellationToken.None);

        AppSettings? saved = store.Saved;
        Assert.NotNull(saved);
        Assert.Equal(1, saved.MaximumParallelUploads);
    }

    [Fact]
    public async Task GetAsync_WhenDuplicateTrustedPeers_KeepsSingleByPeerId()
    {
        InMemorySettingsStore store = new(new AppSettings
        {
            Version = 1,
            DownloadFolder = "C:\\Temp",
            MaximumParallelUploads = 2,
            ThemeMode = AppThemeMode.System,
            TrustedPeerFingerprints = ["ABC", "abc"],
            TrustedPeers =
            [
                new TrustedPeer { PeerId = "10.0.0.5", Fingerprint = "A1" },
                new TrustedPeer { PeerId = "10.0.0.5", Fingerprint = "A2" }
            ]
        });
        SettingsService service = new(store);

        AppSettings settings = await service.GetAsync(CancellationToken.None);

        Assert.Equal(2, settings.Version);
        Assert.Single(settings.TrustedPeers);
        Assert.Equal("10.0.0.5", settings.TrustedPeers[0].PeerId);
        Assert.Single(settings.TrustedPeerFingerprints);
    }

    private sealed class InMemorySettingsStore : ISettingsStore
    {
        public InMemorySettingsStore(AppSettings? initial)
        {
            Saved = initial;
        }

        public AppSettings? Saved { get; private set; }

        public Task<AppSettings?> LoadAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Saved);
        }

        public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken)
        {
            Saved = settings;
            return Task.CompletedTask;
        }
    }
}
