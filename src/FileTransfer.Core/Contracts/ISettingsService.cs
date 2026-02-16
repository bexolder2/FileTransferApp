using FileTransfer.Core.Models;

namespace FileTransfer.Core.Contracts;

public interface ISettingsService
{
    Task<AppSettings> GetAsync(CancellationToken cancellationToken);

    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken);
}
