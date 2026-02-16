namespace FileTransfer.Core.Models;

public sealed class TransferQueueItem
{
    public required string DisplayName { get; init; }

    public required string FullPath { get; init; }

    public required bool IsFolder { get; init; }
}
