using Avalonia.Controls;
using Avalonia.Platform.Storage;
using FileTransfer.Core.Contracts;

namespace FileTransfer.App.Services;

public sealed class AvaloniaFilePickerService : IFilePickerService
{
    private readonly WindowContext _windowContext;

    public AvaloniaFilePickerService(WindowContext windowContext)
    {
        _windowContext = windowContext;
    }

    public async Task<IReadOnlyList<string>> PickFilesAsync(CancellationToken cancellationToken)
    {
        Window? window = _windowContext.MainWindow;
        if (window?.StorageProvider is null)
        {
            return [];
        }

        IReadOnlyList<IStorageFile> pickedFiles = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select files to upload",
            AllowMultiple = true
        });

        return pickedFiles
            .Select(file => file.Path.LocalPath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToArray();
    }
}
