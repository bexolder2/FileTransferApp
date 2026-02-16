using FileTransfer.Core.Contracts;
using FileTransfer.Core.Models;

namespace FileTransfer.Infrastructure.Transfer;

public sealed class TransferOrchestrator : ITransferOrchestrator
{
    private readonly ITransferClient _transferClient;
    private readonly TransferProtocolOptions _options;

    public TransferOrchestrator(ITransferClient transferClient, TransferProtocolOptions options)
    {
        _transferClient = transferClient;
        _options = options;
    }

    public async Task<TransferProgressSnapshot> UploadAsync(
        string targetIpAddress,
        IReadOnlyList<TransferQueueItem> queueItems,
        int maximumParallelUploads,
        IProgress<TransferProgressSnapshot> progress,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ResolvedTransferFile> files = ResolveFiles(queueItems);
        long totalBytes = files.Sum(file => file.Length);
        int totalFiles = files.Count;

        long transferredBytes = 0;
        int completedFiles = 0;
        ReportProgress(progress, totalFiles, completedFiles, totalBytes, transferredBytes, null);

        int degreeOfParallelism = Math.Max(1, maximumParallelUploads);
        using SemaphoreSlim semaphore = new(degreeOfParallelism, degreeOfParallelism);

        Task[] tasks = files.Select(async file =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                long fileTransferred = 0;
                Progress<long> bytesProgress = new(readBytes =>
                {
                    fileTransferred += readBytes;
                    long aggregate = Interlocked.Add(ref transferredBytes, readBytes);
                    ReportProgress(progress, totalFiles, Volatile.Read(ref completedFiles), totalBytes, aggregate, file.RelativePath);
                });

                await _transferClient.SendFileAsync(
                    targetIpAddress,
                    _options.Port,
                    file,
                    bytesProgress,
                    cancellationToken);

                int doneFiles = Interlocked.Increment(ref completedFiles);
                ReportProgress(progress, totalFiles, doneFiles, totalBytes, Volatile.Read(ref transferredBytes), file.RelativePath);
            }
            finally
            {
                semaphore.Release();
            }
        }).ToArray();

        await Task.WhenAll(tasks);

        TransferProgressSnapshot completed = new()
        {
            TotalFiles = totalFiles,
            CompletedFiles = totalFiles,
            TotalBytes = totalBytes,
            TransferredBytes = totalBytes,
            CurrentFile = null
        };
        progress.Report(completed);
        return completed;
    }

    private static IReadOnlyList<ResolvedTransferFile> ResolveFiles(IReadOnlyList<TransferQueueItem> queueItems)
    {
        List<ResolvedTransferFile> files = [];
        foreach (TransferQueueItem queueItem in queueItems)
        {
            if (!queueItem.IsFolder)
            {
                if (!File.Exists(queueItem.FullPath))
                {
                    continue;
                }

                FileInfo info = new(queueItem.FullPath);
                files.Add(new ResolvedTransferFile
                {
                    SourcePath = queueItem.FullPath,
                    RelativePath = Path.GetFileName(queueItem.FullPath),
                    Length = info.Length
                });
                continue;
            }

            if (!Directory.Exists(queueItem.FullPath))
            {
                continue;
            }

            string folderName = Path.GetFileName(queueItem.FullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            foreach (string filePath in Directory.EnumerateFiles(queueItem.FullPath, "*", SearchOption.AllDirectories))
            {
                FileInfo info = new(filePath);
                string relativeInsideFolder = Path.GetRelativePath(queueItem.FullPath, filePath);
                string relativePath = Path.Combine(folderName, relativeInsideFolder);

                files.Add(new ResolvedTransferFile
                {
                    SourcePath = filePath,
                    RelativePath = relativePath,
                    Length = info.Length
                });
            }
        }

        return files;
    }

    private static void ReportProgress(
        IProgress<TransferProgressSnapshot> progress,
        int totalFiles,
        int completedFiles,
        long totalBytes,
        long transferredBytes,
        string? currentFile)
    {
        progress.Report(new TransferProgressSnapshot
        {
            TotalFiles = totalFiles,
            CompletedFiles = completedFiles,
            TotalBytes = totalBytes,
            TransferredBytes = transferredBytes,
            CurrentFile = currentFile
        });
    }
}
