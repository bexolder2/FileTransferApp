using FileTransfer.Core.Models;

namespace FileTransfer.Core.Contracts;

public interface ITransferClient
{
    Task SendFileAsync(
        string targetIpAddress,
        int port,
        ResolvedTransferFile file,
        IProgress<long> bytesProgress,
        CancellationToken cancellationToken);
}
