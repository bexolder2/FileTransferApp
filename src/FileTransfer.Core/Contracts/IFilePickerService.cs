namespace FileTransfer.Core.Contracts;

public interface IFilePickerService
{
    Task<IReadOnlyList<string>> PickFilesAsync(CancellationToken cancellationToken);
}
