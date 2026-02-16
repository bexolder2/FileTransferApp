using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FileTransfer.Core.Contracts;
using FileTransfer.Core.Models;
using FileTransfer.Infrastructure.Security;

namespace FileTransfer.UnitTests;

public sealed class TrustOnFirstUseServiceTests
{
    [Fact]
    public void ValidateServerCertificate_FirstUse_PersistsTrust()
    {
        InMemorySettingsService settingsService = new();
        TrustOnFirstUseService service = new(settingsService);
        X509Certificate2 cert = CreateCertificate("CN=PeerOne");

        bool accepted = service.ValidateServerCertificate("10.0.0.20", cert, out string? error);

        Assert.True(accepted);
        Assert.Null(error);
        Assert.Single(settingsService.Current.TrustedPeers);
        Assert.Equal("10.0.0.20", settingsService.Current.TrustedPeers[0].PeerId);
    }

    [Fact]
    public void ValidateServerCertificate_Mismatch_BlocksTransfer()
    {
        InMemorySettingsService settingsService = new();
        TrustOnFirstUseService service = new(settingsService);
        X509Certificate2 first = CreateCertificate("CN=PeerOne");
        X509Certificate2 second = CreateCertificate("CN=PeerOne-New");

        bool firstAccepted = service.ValidateServerCertificate("10.0.0.20", first, out _);
        bool secondAccepted = service.ValidateServerCertificate("10.0.0.20", second, out string? mismatchError);

        Assert.True(firstAccepted);
        Assert.False(secondAccepted);
        Assert.Contains("mismatch", mismatchError, StringComparison.OrdinalIgnoreCase);
    }

    private static X509Certificate2 CreateCertificate(string subjectName)
    {
        using RSA rsa = RSA.Create(2048);
        CertificateRequest request = new(subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return request.CreateSelfSigned(now.AddMinutes(-1), now.AddHours(1));
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
