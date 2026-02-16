namespace FileTransfer.Core.Models;

public sealed class TransferProtocolOptions
{
    public const int DefaultPort = 50505;
    public const int LargeFileThresholdBytes = 10 * 1024 * 1024;
    public const int LargeFileChunkBytes = 5 * 1024 * 1024;
    public const int DefaultChunkBytes = 512 * 1024;

    public int Port { get; init; } = DefaultPort;
}
