using FileTransfer.Core.Models;

namespace FileTransfer.Core.Contracts;

public interface ILocalDeviceScanner
{
    Task<IReadOnlyList<DeviceInfo>> ScanAsync(CancellationToken cancellationToken, IReadOnlyCollection<string>? additionalCandidateIps = null);
}
