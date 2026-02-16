namespace FileTransfer.Core.Models;

public sealed class DeviceInfo
{
    public required string IpAddress { get; init; }

    public required string DisplayName { get; init; }
}
