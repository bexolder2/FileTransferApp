using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using FileTransfer.Core.Contracts;
using FileTransfer.Core.Models;

namespace FileTransfer.Infrastructure.Network;

public sealed class LocalDeviceScanner : ILocalDeviceScanner
{
    private readonly TransferProtocolOptions _protocolOptions;

    public LocalDeviceScanner(TransferProtocolOptions protocolOptions)
    {
        _protocolOptions = protocolOptions;
    }

    public async Task<IReadOnlyList<DeviceInfo>> ScanAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        List<IPAddress> localIps = NetworkInterface.GetAllNetworkInterfaces()
            .Where(interfaceInfo =>
                interfaceInfo.OperationalStatus == OperationalStatus.Up
                && interfaceInfo.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(interfaceInfo => interfaceInfo.GetIPProperties().UnicastAddresses)
            .Select(addressInfo => addressInfo.Address)
            .Where(address => address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address))
            .Distinct()
            .ToList();

        HashSet<string> candidates = [];
        foreach (IPAddress localIp in localIps)
        {
            foreach (string ip in EnumerateSubnetCandidates(localIp))
            {
                candidates.Add(ip);
            }
        }

        List<DeviceInfo> devices = [];
        SemaphoreSlim semaphore = new(initialCount: 64, maxCount: 64);
        List<Task> scanTasks = [];
        foreach (string candidateIp in candidates)
        {
            if (localIps.Any(ip => ip.ToString() == candidateIp))
            {
                continue;
            }

            scanTasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    if (await ProbeHostAsync(candidateIp, cancellationToken))
                    {
                        lock (devices)
                        {
                            devices.Add(new DeviceInfo
                            {
                                IpAddress = candidateIp,
                                DisplayName = candidateIp
                            });
                        }
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken));
        }

        await Task.WhenAll(scanTasks);
        semaphore.Dispose();

        return devices
            .OrderBy(device => device.IpAddress, StringComparer.Ordinal)
            .ToArray();
    }

    private async Task<bool> ProbeHostAsync(string ipAddress, CancellationToken cancellationToken)
    {
        bool pingReachable = false;
        try
        {
            using Ping ping = new();
            PingReply reply = await ping.SendPingAsync(ipAddress, 150);
            pingReachable = reply.Status == IPStatus.Success;
        }
        catch
        {
            pingReachable = false;
        }

        bool portOpen = false;
        try
        {
            using TcpClient client = new();
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromMilliseconds(300));
            await client.ConnectAsync(ipAddress, _protocolOptions.Port, timeout.Token);
            portOpen = true;
        }
        catch
        {
            portOpen = false;
        }

        return pingReachable || portOpen;
    }

    private static IEnumerable<string> EnumerateSubnetCandidates(IPAddress localIp)
    {
        byte[] bytes = localIp.GetAddressBytes();
        if (bytes.Length != 4)
        {
            yield break;
        }

        // Limit scanning scope to /24 for responsiveness.
        for (int host = 1; host <= 254; host++)
        {
            if (host == bytes[3])
            {
                continue;
            }

            yield return $"{bytes[0]}.{bytes[1]}.{bytes[2]}.{host}";
        }
    }
}
