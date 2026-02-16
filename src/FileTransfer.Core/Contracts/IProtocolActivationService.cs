namespace FileTransfer.Core.Contracts;

public interface IProtocolActivationService
{
    Task<bool> ApplyActivationAsync(string[] args, CancellationToken cancellationToken);
}
