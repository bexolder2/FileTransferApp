namespace FileTransfer.Core.Contracts;

public interface ITransferReceiverHost : IAsyncDisposable
{
    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}
