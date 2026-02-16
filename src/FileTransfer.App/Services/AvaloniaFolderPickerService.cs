using Avalonia.Controls;
using Avalonia.Platform.Storage;
using FileTransfer.Core.Contracts;

namespace FileTransfer.App.Services;

public sealed class AvaloniaFolderPickerService : IFolderPickerService
{
    private readonly WindowContext _windowContext;

    public AvaloniaFolderPickerService(WindowContext windowContext)
    {
        _windowContext = windowContext;
    }

    public async Task<string?> PickFolderAsync(string title, CancellationToken cancellationToken)
    {
        Window? window = _windowContext.MainWindow;
        if (window?.StorageProvider is null)
        {
            return null;
        }

        IReadOnlyList<IStorageFolder> pickedFolders = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        });

        IStorageFolder? selected = pickedFolders.FirstOrDefault();
        return selected?.Path.LocalPath;
    }
}
