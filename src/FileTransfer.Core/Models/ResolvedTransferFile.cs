namespace FileTransfer.Core.Models;

public sealed class ResolvedTransferFile
{
    public required string SourcePath { get; init; }

    public required string RelativePath { get; init; }

    public required long Length { get; init; }
}
