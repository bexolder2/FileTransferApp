using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FileTransfer.Core.Models;

public sealed class TransferQueueItem : INotifyPropertyChanged
{
    private string _displayName = string.Empty;
    private string _fullPath = string.Empty;
    private bool _isFolder;
    private string? _relativePath;
    private bool _isSelected = true;

    public required string DisplayName
    {
        get => _displayName;
        set => SetField(ref _displayName, value);
    }

    public required string FullPath
    {
        get => _fullPath;
        set => SetField(ref _fullPath, value);
    }

    public required bool IsFolder
    {
        get => _isFolder;
        set => SetField(ref _isFolder, value);
    }

    public string? RelativePath
    {
        get => _relativePath;
        set => SetField(ref _relativePath, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    public bool CanSelectForUpload => true;

    public ObservableCollection<TransferQueueItem> Children { get; } = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
