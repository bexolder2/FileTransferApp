using FileTransfer.Core.Models;

namespace FileTransfer.Core.Contracts;

public interface ITransferOrchestrator
{
    Task<TransferProgressSnapshot> UploadAsync(
        string targetIpAddress,
        IReadOnlyList<TransferQueueItem> queueItems,
        int maximumParallelUploads,
        IProgress<TransferProgressSnapshot> progress,
        CancellationToken cancellationToken);
}
