namespace FileTransfer.Core.Models;

public sealed class TransferProgressSnapshot
{
    public required int TotalFiles { get; init; }

    public required int CompletedFiles { get; init; }

    public required long TotalBytes { get; init; }

    public required long TransferredBytes { get; init; }

    public string? CurrentFile { get; init; }
}
