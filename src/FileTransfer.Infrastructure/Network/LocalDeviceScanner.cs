using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Collections.Concurrent;
using FileTransfer.Core.Contracts;
using FileTransfer.Core.Models;

namespace FileTransfer.Infrastructure.Network;

public sealed class LocalDeviceScanner : ILocalDeviceScanner
{
    private const int MaxConcurrentProbes = 16;

    private readonly TransferProtocolOptions _protocolOptions;

    public LocalDeviceScanner(TransferProtocolOptions protocolOptions)
    {
        _protocolOptions = protocolOptions;
    }

    public async Task<IReadOnlyList<DeviceInfo>> ScanAsync(CancellationToken cancellationToken, IReadOnlyCollection<string>? additionalCandidateIps = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        List<IPAddress> localIps = NetworkInterface.GetAllNetworkInterfaces()
            .Where(interfaceInfo =>
                interfaceInfo.OperationalStatus == OperationalStatus.Up
                && interfaceInfo.NetworkInterfaceType != NetworkInterfaceType.Loopback
                && interfaceInfo.NetworkInterfaceType != NetworkInterfaceType.Tunnel
                && interfaceInfo.NetworkInterfaceType != NetworkInterfaceType.Unknown)
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

        if (additionalCandidateIps is not null)
        {
            foreach (string candidateIp in additionalCandidateIps)
            {
                if (IPAddress.TryParse(candidateIp, out IPAddress? parsed)
                    && parsed.AddressFamily == AddressFamily.InterNetwork
                    && !IPAddress.IsLoopback(parsed))
                {
                    candidates.Add(parsed.ToString());
                }
            }
        }

        HashSet<string> localIpStrings = localIps
            .Select(ip => ip.ToString())
            .ToHashSet(StringComparer.Ordinal);
        string[] targets = candidates
            .Where(candidateIp => !localIpStrings.Contains(candidateIp))
            .ToArray();

        ConcurrentBag<DeviceInfo> devices = [];
        ParallelOptions options = new()
        {
            MaxDegreeOfParallelism = Math.Min(MaxConcurrentProbes, targets.Length == 0 ? 1 : targets.Length),
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(targets, options, async (candidateIp, ct) =>
        {
            if (await ProbeHostAsync(candidateIp, ct))
            {
                devices.Add(new DeviceInfo
                {
                    IpAddress = candidateIp,
                    DisplayName = candidateIp
                });
            }
        });

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
            PingReply reply = await ping.SendPingAsync(ipAddress, 120);
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
            timeout.CancelAfter(TimeSpan.FromMilliseconds(180));
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
