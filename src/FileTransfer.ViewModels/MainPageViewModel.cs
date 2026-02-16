using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileTransfer.Core.Contracts;
using FileTransfer.Core.Models;

namespace FileTransfer.ViewModels;

public sealed partial class MainPageViewModel : ViewModelBase
{
    private readonly IFilePickerService _filePickerService;
    private readonly IFolderPickerService _folderPickerService;
    private readonly ISettingsService _settingsService;
    private readonly ITransferOrchestrator _transferOrchestrator;
    private CancellationTokenSource? _uploadCancellation;

    [ObservableProperty]
    private bool _isUploading;

    [ObservableProperty]
    private int _completedItems;

    [ObservableProperty]
    private int _totalItems;

    [ObservableProperty]
    private long _transferredBytes;

    [ObservableProperty]
    private long _totalBytes;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private TransferQueueItem? _selectedQueueItem;

    public MainPageViewModel(
        IFilePickerService filePickerService,
        IFolderPickerService folderPickerService,
        ISettingsService settingsService,
        ITransferOrchestrator transferOrchestrator)
    {
        _filePickerService = filePickerService;
        _folderPickerService = folderPickerService;
        _settingsService = settingsService;
        _transferOrchestrator = transferOrchestrator;
        Queue = new ObservableCollection<TransferQueueItem>();
        Queue.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasQueueItems));
        TotalItems = 0;
        CompletedItems = 0;
    }

    public ObservableCollection<TransferQueueItem> Queue { get; }

    public bool HasQueueItems => Queue.Count > 0;

    public string UploadButtonText => IsUploading ? "Cancel" : "Start upload";

    public string ProgressText => $"{CompletedItems}/{TotalItems}";

    public string ByteProgressText => $"{FormatBytes(TransferredBytes)} / {FormatBytes(TotalBytes)}";

    [RelayCommand]
    private async Task AddFileAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<string> files = await _filePickerService.PickFilesAsync(cancellationToken);
        foreach (string filePath in files)
        {
            if (Queue.Any(item => string.Equals(item.FullPath, filePath, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            Queue.Add(new TransferQueueItem
            {
                DisplayName = Path.GetFileName(filePath),
                FullPath = filePath,
                IsFolder = false
            });
        }

        StatusText = Queue.Count > 0 ? "Queue updated." : "No files selected.";
    }

    [RelayCommand]
    private async Task AddFolderAsync(CancellationToken cancellationToken)
    {
        string? folderPath = await _folderPickerService.PickFolderAsync("Select folder to upload", cancellationToken);
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return;
        }

        if (Queue.Any(item => string.Equals(item.FullPath, folderPath, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        Queue.Add(new TransferQueueItem
        {
            DisplayName = Path.GetFileName(folderPath),
            FullPath = folderPath,
            IsFolder = true
        });
        StatusText = "Queue updated.";
    }

    [RelayCommand]
    private void ClearQueue()
    {
        Queue.Clear();
        CompletedItems = 0;
        TotalItems = 0;
        TransferredBytes = 0;
        TotalBytes = 0;
        StatusText = "Queue cleared.";
        OnPropertyChanged(nameof(HasQueueItems));
    }

    [RelayCommand]
    private void RemoveSelected()
    {
        if (SelectedQueueItem is null)
        {
            return;
        }

        Queue.Remove(SelectedQueueItem);
        SelectedQueueItem = null;
        StatusText = "Removed selected item.";
    }

    [RelayCommand]
    private async Task ToggleUploadAsync(CancellationToken cancellationToken)
    {
        if (IsUploading)
        {
            _uploadCancellation?.Cancel();
            StatusText = "Cancelling upload...";
            return;
        }

        if (Queue.Count == 0)
        {
            StatusText = "Add files or folders before starting upload.";
            return;
        }

        AppSettings settings = await _settingsService.GetAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(settings.LastSelectedTargetIp))
        {
            StatusText = "Select a target device IP in Settings first.";
            return;
        }

        IsUploading = true;
        _uploadCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        try
        {
            Progress<TransferProgressSnapshot> progress = new(snapshot =>
            {
                TotalItems = snapshot.TotalFiles;
                CompletedItems = snapshot.CompletedFiles;
                TotalBytes = snapshot.TotalBytes;
                TransferredBytes = snapshot.TransferredBytes;
                StatusText = snapshot.CurrentFile is null
                    ? "Uploading..."
                    : $"Uploading {snapshot.CurrentFile}";
            });

            TransferProgressSnapshot completed = await _transferOrchestrator.UploadAsync(
                settings.LastSelectedTargetIp,
                Queue.ToArray(),
                settings.MaximumParallelUploads,
                progress,
                _uploadCancellation.Token);

            TotalItems = completed.TotalFiles;
            CompletedItems = completed.CompletedFiles;
            TotalBytes = completed.TotalBytes;
            TransferredBytes = completed.TransferredBytes;
            StatusText = "Upload completed.";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Upload cancelled.";
        }
        catch (Exception ex)
        {
            StatusText = $"Upload failed: {ex.Message}";
        }
        finally
        {
            _uploadCancellation?.Dispose();
            _uploadCancellation = null;
            IsUploading = false;
        }
    }

    partial void OnIsUploadingChanged(bool value)
    {
        OnPropertyChanged(nameof(UploadButtonText));
    }

    partial void OnCompletedItemsChanged(int value)
    {
        OnPropertyChanged(nameof(ProgressText));
    }

    partial void OnTotalItemsChanged(int value)
    {
        OnPropertyChanged(nameof(ProgressText));
    }

    partial void OnTransferredBytesChanged(long value)
    {
        OnPropertyChanged(nameof(ByteProgressText));
    }

    partial void OnTotalBytesChanged(long value)
    {
        OnPropertyChanged(nameof(ByteProgressText));
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        int suffixIndex = 0;
        while (value >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            value /= 1024;
            suffixIndex++;
        }

        return $"{value:0.##} {suffixes[suffixIndex]}";
    }
}
