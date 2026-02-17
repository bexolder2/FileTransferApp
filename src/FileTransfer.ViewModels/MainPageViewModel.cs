using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
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
    private readonly NavigationCommandsHolder _navigationCommands;
    private readonly HashSet<string> _queuedFilePaths = new(StringComparer.OrdinalIgnoreCase);
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
        ITransferOrchestrator transferOrchestrator,
        NavigationCommandsHolder navigationCommands)
    {
        _filePickerService = filePickerService;
        _folderPickerService = folderPickerService;
        _settingsService = settingsService;
        _transferOrchestrator = transferOrchestrator;
        _navigationCommands = navigationCommands;
        Queue = new ObservableCollection<TransferQueueItem>();
        QueueView = new ObservableCollection<TransferQueueItem>();
        Queue.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasQueueItems));
        TotalItems = 0;
        CompletedItems = 0;
    }

    public ObservableCollection<TransferQueueItem> Queue { get; }
    public ObservableCollection<TransferQueueItem> QueueView { get; }

    public bool HasQueueItems => Queue.Count > 0;

    public ICommand? NavigateToSettingsCommand => _navigationCommands.NavigateToSettingsCommand;

    public string UploadButtonText => IsUploading ? "Cancel" : "Start upload";

    public string ProgressText => $"{CompletedItems}/{TotalItems}";

    public string ByteProgressText => $"{FormatBytes(TransferredBytes)} / {FormatBytes(TotalBytes)}";

    [RelayCommand]
    private async Task AddFileAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<string> files = await _filePickerService.PickFilesAsync(cancellationToken);
        int addedCount = 0;
        foreach (string filePath in files)
        {
            if (_queuedFilePaths.Contains(filePath))
            {
                continue;
            }

            Queue.Add(new TransferQueueItem
            {
                DisplayName = Path.GetFileName(filePath),
                FullPath = filePath,
                IsFolder = false,
                IsSelected = true,
                RelativePath = Path.GetFileName(filePath)
            });
            RegisterSelectionTracking(Queue[^1]);
            _queuedFilePaths.Add(filePath);
            addedCount++;
        }

        RebuildQueueView();
        StatusText = addedCount > 0 ? "Queue updated." : "No new files selected.";
    }

    [RelayCommand]
    private async Task AddFolderAsync(CancellationToken cancellationToken)
    {
        string? folderPath = await _folderPickerService.PickFolderAsync("Select folder to upload", cancellationToken);
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return;
        }

        string folderName = Path.GetFileName(folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        TransferQueueItem folderNode = new()
        {
            DisplayName = folderName,
            FullPath = folderPath,
            IsFolder = true,
            IsSelected = true
        };

        int addedCount = 0;
        foreach (string filePath in Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories))
        {
            if (_queuedFilePaths.Contains(filePath))
            {
                continue;
            }

            string relativeInsideFolder = Path.GetRelativePath(folderPath, filePath);
            folderNode.Children.Add(new TransferQueueItem
            {
                DisplayName = relativeInsideFolder,
                FullPath = filePath,
                IsFolder = false,
                IsSelected = true,
                RelativePath = Path.Combine(folderName, relativeInsideFolder)
            });
            _queuedFilePaths.Add(filePath);
            addedCount++;
        }

        if (addedCount == 0)
        {
            StatusText = "No new files found in selected folder.";
            return;
        }

        RegisterSelectionTracking(folderNode);
        Queue.Add(folderNode);
        RebuildQueueView();
        StatusText = "Queue updated.";
    }

    [RelayCommand]
    private void ClearQueue()
    {
        foreach (TransferQueueItem rootItem in Queue)
        {
            UnregisterSelectionTracking(rootItem);
        }

        Queue.Clear();
        QueueView.Clear();
        _queuedFilePaths.Clear();
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

        TransferQueueItem selectedItem = SelectedQueueItem;
        if (!Queue.Remove(selectedItem))
        {
            RemoveFromTree(Queue, selectedItem);
        }

        UnregisterSelectionTracking(selectedItem);
        RemovePathsForNode(selectedItem);
        RemoveEmptyFolders();
        RebuildQueueView();
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

        IReadOnlyList<TransferQueueItem> selectedFiles = GetSelectedFiles().ToArray();
        if (selectedFiles.Count == 0)
        {
            StatusText = "Select at least one file to upload.";
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
                selectedFiles,
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

    private IEnumerable<TransferQueueItem> EnumerateQueueItems()
    {
        foreach (TransferQueueItem rootItem in Queue)
        {
            yield return rootItem;
            foreach (TransferQueueItem descendant in EnumerateChildren(rootItem))
            {
                yield return descendant;
            }
        }
    }

    private static IEnumerable<TransferQueueItem> EnumerateChildren(TransferQueueItem node)
    {
        foreach (TransferQueueItem child in node.Children)
        {
            yield return child;
            foreach (TransferQueueItem descendant in EnumerateChildren(child))
            {
                yield return descendant;
            }
        }
    }

    private IEnumerable<TransferQueueItem> GetSelectedFiles() =>
        EnumerateQueueItems().Where(item => !item.IsFolder && item.IsSelected);

    private static bool RemoveFromTree(IEnumerable<TransferQueueItem> nodes, TransferQueueItem toRemove)
    {
        foreach (TransferQueueItem node in nodes)
        {
            if (node.Children.Remove(toRemove))
            {
                return true;
            }

            if (RemoveFromTree(node.Children, toRemove))
            {
                return true;
            }
        }

        return false;
    }

    private void RemovePathsForNode(TransferQueueItem node)
    {
        if (!node.IsFolder)
        {
            _queuedFilePaths.Remove(node.FullPath);
            return;
        }

        foreach (TransferQueueItem child in EnumerateChildren(node))
        {
            if (!child.IsFolder)
            {
                _queuedFilePaths.Remove(child.FullPath);
            }
        }
    }

    private void RemoveEmptyFolders()
    {
        foreach (TransferQueueItem folder in Queue.Where(item => item.IsFolder && item.Children.Count == 0).ToArray())
        {
            UnregisterSelectionTracking(folder);
            Queue.Remove(folder);
        }
    }

    private void RebuildQueueView()
    {
        QueueView.Clear();
        foreach (TransferQueueItem root in Queue)
        {
            QueueView.Add(root);
            foreach (TransferQueueItem descendant in EnumerateChildren(root))
            {
                QueueView.Add(descendant);
            }
        }
    }

    private void RegisterSelectionTracking(TransferQueueItem rootItem)
    {
        rootItem.PropertyChanged += OnQueueItemPropertyChanged;
        foreach (TransferQueueItem child in EnumerateChildren(rootItem))
        {
            child.PropertyChanged += OnQueueItemPropertyChanged;
        }
    }

    private void UnregisterSelectionTracking(TransferQueueItem rootItem)
    {
        rootItem.PropertyChanged -= OnQueueItemPropertyChanged;
        foreach (TransferQueueItem child in EnumerateChildren(rootItem))
        {
            child.PropertyChanged -= OnQueueItemPropertyChanged;
        }
    }

    private void OnQueueItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not TransferQueueItem item ||
            e.PropertyName != nameof(TransferQueueItem.IsSelected) ||
            !item.IsFolder)
        {
            return;
        }

        // Folder toggle acts as a bulk selector for its files.
        foreach (TransferQueueItem child in EnumerateChildren(item))
        {
            child.IsSelected = item.IsSelected;
        }
    }
}
