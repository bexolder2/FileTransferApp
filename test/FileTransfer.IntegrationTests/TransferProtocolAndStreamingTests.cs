using System.Collections.Concurrent;
using FileTransfer.Core.Contracts;
using FileTransfer.Core.Models;
using FileTransfer.Infrastructure.Transfer;
using FileTransfer.Infrastructure.Transfer.Protocol;

namespace FileTransfer.IntegrationTests;

public sealed class TransferProtocolAndStreamingTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Serializer_WritesAndReadsFrames_RoundTripsPayload()
    {
        TransferProtocolSerializer serializer = new();
        using MemoryStream stream = new();

        await serializer.WriteFrameAsync(stream, TransferFrameType.Chunk, new byte[] { 1, 2, 3 }, CancellationToken.None);
        stream.Position = 0;
        (TransferFrameType frameType, byte[] payload) = await serializer.ReadFrameAsync(stream, CancellationToken.None);

        Assert.Equal(TransferFrameType.Chunk, frameType);
        Assert.Equal(new byte[] { 1, 2, 3 }, payload);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Orchestrator_WithFolderQueue_ResolvesAndStreamsAllFiles()
    {
        string root = Path.Combine(Path.GetTempPath(), "FileTransfer.Integration." + Guid.NewGuid());
        string sourceDir = Path.Combine(root, "source");
        Directory.CreateDirectory(sourceDir);

        try
        {
            string directFile = Path.Combine(sourceDir, "a.txt");
            string nestedDir = Path.Combine(sourceDir, "nested");
            Directory.CreateDirectory(nestedDir);
            string nestedFile = Path.Combine(nestedDir, "b.txt");

            await File.WriteAllTextAsync(directFile, "alpha");
            await File.WriteAllTextAsync(nestedFile, "beta");

            CapturingTransferClient transferClient = new();
            TransferOrchestrator orchestrator = new(transferClient, new TransferProtocolOptions { Port = 50505 });

            ConcurrentQueue<TransferProgressSnapshot> snapshots = new();
            Progress<TransferProgressSnapshot> progress = new(snapshot => snapshots.Enqueue(snapshot));

            TransferProgressSnapshot result = await orchestrator.UploadAsync(
                "127.0.0.1",
                [new TransferQueueItem { DisplayName = "source", FullPath = sourceDir, IsFolder = true }],
                maximumParallelUploads: 2,
                progress,
                CancellationToken.None);

            Assert.Equal(2, transferClient.Files.Count);
            Assert.Contains(transferClient.Files, file => file.RelativePath.Replace('\\', '/') == "source/a.txt");
            Assert.Contains(transferClient.Files, file => file.RelativePath.Replace('\\', '/') == "source/nested/b.txt");
            Assert.Equal(2, result.CompletedFiles);
            bool sawProgressUpdates = SpinWait.SpinUntil(() => snapshots.Count >= 2, TimeSpan.FromSeconds(1));
            Assert.True(sawProgressUpdates);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    private sealed class CapturingTransferClient : ITransferClient
    {
        public List<ResolvedTransferFile> Files { get; } = [];

        public Task SendFileAsync(string targetIpAddress, int port, ResolvedTransferFile file, IProgress<long> bytesProgress, CancellationToken cancellationToken)
        {
            Files.Add(file);
            bytesProgress.Report(file.Length);
            return Task.CompletedTask;
        }
    }
}
