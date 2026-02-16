namespace FileTransfer.Core.Models;

public sealed class TrustedPeer
{
    public required string PeerId { get; init; }

    public required string Fingerprint { get; init; }
}
