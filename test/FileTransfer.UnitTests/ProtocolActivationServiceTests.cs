using FileTransfer.Core.Contracts;
using FileTransfer.Core.Models;
using FileTransfer.Infrastructure.Activation;

namespace FileTransfer.UnitTests;

public sealed class ProtocolActivationServiceTests
{
    [Fact]
    public async Task ApplyActivationAsync_WithTargetQuery_StoresTargetIp()
    {
        InMemorySettingsService settingsService = new();
        ProtocolActivationService service = new(settingsService);

        bool handled = await service.ApplyActivationAsync(
            ["filetransfer://open?target=192.168.1.44"],
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal("192.168.1.44", settingsService.Current.LastSelectedTargetIp);
    }

    [Fact]
    public async Task ApplyActivationAsync_WithHost_StoresHostAsTarget()
    {
        InMemorySettingsService settingsService = new();
        ProtocolActivationService service = new(settingsService);

        bool handled = await service.ApplyActivationAsync(
            ["filetransfer://192.168.1.99/send"],
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal("192.168.1.99", settingsService.Current.LastSelectedTargetIp);
    }

    [Fact]
    public async Task ApplyActivationAsync_WithNonProtocolArg_DoesNothing()
    {
        InMemorySettingsService settingsService = new();
        ProtocolActivationService service = new(settingsService);

        bool handled = await service.ApplyActivationAsync(
            ["--normal-startup"],
            CancellationToken.None);

        Assert.False(handled);
        Assert.Null(settingsService.Current.LastSelectedTargetIp);
    }

    private sealed class InMemorySettingsService : ISettingsService
    {
        public AppSettings Current { get; private set; } = new()
        {
            Version = 2,
            DownloadFolder = "C:\\Temp",
            MaximumParallelUploads = 2,
            ThemeMode = AppThemeMode.System,
            TrustedPeerFingerprints = [],
            TrustedPeers = []
        };

        public Task<AppSettings> GetAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Current);
        }

        public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken)
        {
            Current = settings;
            return Task.CompletedTask;
        }
    }
}
