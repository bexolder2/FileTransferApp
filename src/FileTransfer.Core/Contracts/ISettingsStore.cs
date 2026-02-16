using FileTransfer.Core.Models;

namespace FileTransfer.Core.Contracts;

public interface ISettingsStore
{
    Task<AppSettings?> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken);
}
