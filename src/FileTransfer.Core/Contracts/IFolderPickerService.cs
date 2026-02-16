namespace FileTransfer.Core.Contracts;

public interface IFolderPickerService
{
    Task<string?> PickFolderAsync(string title, CancellationToken cancellationToken);
}
