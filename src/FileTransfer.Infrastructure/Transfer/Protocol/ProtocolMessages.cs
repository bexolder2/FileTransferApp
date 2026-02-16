namespace FileTransfer.Infrastructure.Transfer.Protocol;

internal sealed class HandshakeMessage
{
    public int ProtocolVersion { get; init; } = 1;
}

internal sealed class ManifestMessage
{
    public required string RelativePath { get; init; }

    public required long Length { get; init; }
}
