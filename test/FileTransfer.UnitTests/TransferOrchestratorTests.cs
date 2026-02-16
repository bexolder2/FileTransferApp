using FileTransfer.Core.Contracts;
using FileTransfer.Core.Models;
using FileTransfer.Infrastructure.Transfer;

namespace FileTransfer.UnitTests;

public sealed class TransferOrchestratorTests
{
    [Fact]
    public async Task UploadAsync_WithSingleFile_ReportsCompletedProgress()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), "FileTransfer.Orchestrator.Unit." + Guid.NewGuid());
        Directory.CreateDirectory(tempRoot);
        try
        {
            string sourcePath = Path.Combine(tempRoot, "sample.bin");
            byte[] data = new byte[4096];
            Random.Shared.NextBytes(data);
            await File.WriteAllBytesAsync(sourcePath, data);

            CapturingTransferClient client = new();
            TransferOrchestrator orchestrator = new(client, new TransferProtocolOptions { Port = 50505 });

            List<TransferProgressSnapshot> snapshots = [];
            Progress<TransferProgressSnapshot> progress = new(snapshot => snapshots.Add(snapshot));

            TransferProgressSnapshot result = await orchestrator.UploadAsync(
                "127.0.0.1",
                [new TransferQueueItem { DisplayName = "sample.bin", FullPath = sourcePath, IsFolder = false }],
                1,
                progress,
                CancellationToken.None);

            Assert.Equal(1, result.TotalFiles);
            Assert.Equal(1, result.CompletedFiles);
            Assert.Equal(4096, result.TotalBytes);
            Assert.Equal(4096, result.TransferredBytes);
            Assert.NotEmpty(snapshots);
            Assert.Contains(snapshots, snapshot => snapshot.CompletedFiles == 1);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, true);
            }
        }
    }

    [Fact]
    public async Task UploadAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), "FileTransfer.Orchestrator.Cancel.Unit." + Guid.NewGuid());
        Directory.CreateDirectory(tempRoot);
        try
        {
            string sourcePath = Path.Combine(tempRoot, "sample.bin");
            await File.WriteAllBytesAsync(sourcePath, new byte[1024]);

            CancellableTransferClient client = new();
            TransferOrchestrator orchestrator = new(client, new TransferProtocolOptions { Port = 50505 });

            using CancellationTokenSource cts = new();
            cts.CancelAfter(50);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => orchestrator.UploadAsync(
                "127.0.0.1",
                [new TransferQueueItem { DisplayName = "sample.bin", FullPath = sourcePath, IsFolder = false }],
                1,
                new Progress<TransferProgressSnapshot>(_ => { }),
                cts.Token));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, true);
            }
        }
    }

    private sealed class CapturingTransferClient : ITransferClient
    {
        public async Task SendFileAsync(string targetIpAddress, int port, ResolvedTransferFile file, IProgress<long> bytesProgress, CancellationToken cancellationToken)
        {
            await Task.Yield();
            bytesProgress.Report(file.Length);
        }
    }

    private sealed class CancellableTransferClient : ITransferClient
    {
        public async Task SendFileAsync(string targetIpAddress, int port, ResolvedTransferFile file, IProgress<long> bytesProgress, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            bytesProgress.Report(file.Length);
        }
    }
}
