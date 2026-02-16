namespace FileTransfer.Infrastructure.Transfer.Protocol;

public enum TransferFrameType : byte
{
    Handshake = 1,
    Manifest = 2,
    Chunk = 3,
    FileComplete = 4,
    SessionComplete = 5,
    Cancel = 6,
    Error = 7
}
