using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;

namespace FileTransfer.Infrastructure.Transfer;

public sealed class LocalCertificateStore
{
    private readonly string _certificatePath;

    public LocalCertificateStore()
        : this(Path.Combine(AppContext.BaseDirectory, "tls-server.pfx"))
    {
    }

    public LocalCertificateStore(string certificatePath)
    {
        _certificatePath = certificatePath;
    }

    public X509Certificate2 GetOrCreateServerCertificate()
    {
        if (File.Exists(_certificatePath))
        {
            try
            {
                byte[] existing = File.ReadAllBytes(_certificatePath);
                X509Certificate2 loaded = X509CertificateLoader.LoadPkcs12(
                    existing,
                    password: null,
                    X509KeyStorageFlags.Exportable | X509KeyStorageFlags.UserKeySet);

                bool supportsServerAuth = loaded.Extensions
                    .OfType<X509EnhancedKeyUsageExtension>()
                    .SelectMany(extension => extension.EnhancedKeyUsages.Cast<Oid>())
                    .Any(oid => string.Equals(oid.Value, "1.3.6.1.5.5.7.3.1", StringComparison.Ordinal));

                if (loaded.HasPrivateKey && supportsServerAuth)
                {
                    return loaded;
                }
            }
            catch
            {
                // Corrupted or incompatible certificate. Regenerate below.
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(_certificatePath)!);
        X509Certificate2 created = TlsCertificateProvider.CreateSelfSignedCertificate();
        byte[] exported = created.Export(X509ContentType.Pkcs12);
        File.WriteAllBytes(_certificatePath, exported);
        return created;
    }
}
