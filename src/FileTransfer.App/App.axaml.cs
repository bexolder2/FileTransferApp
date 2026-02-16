using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FileTransfer.App.Services;
using FileTransfer.Core.Contracts;
using FileTransfer.Core.Models;
using FileTransfer.Infrastructure.Network;
using FileTransfer.Infrastructure.Security;
using FileTransfer.Infrastructure.Settings;
using FileTransfer.Infrastructure.Activation;
using FileTransfer.Infrastructure.Transfer;
using FileTransfer.Infrastructure.Transfer.Protocol;
using FileTransfer.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FileTransfer.App;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _serviceProvider = BuildServices();

            SettingsPageViewModel settingsPageViewModel = _serviceProvider.GetRequiredService<SettingsPageViewModel>();
            await settingsPageViewModel.InitializeAsync(CancellationToken.None);
            await _serviceProvider.GetRequiredService<IProtocolActivationService>()
                .ApplyActivationAsync(Environment.GetCommandLineArgs().Skip(1).ToArray(), CancellationToken.None);
            await settingsPageViewModel.InitializeAsync(CancellationToken.None);

            MainWindow mainWindow = new()
            {
                DataContext = _serviceProvider.GetRequiredService<ShellViewModel>()
            };

            await _serviceProvider.GetRequiredService<ITransferReceiverHost>().StartAsync(CancellationToken.None);
            _serviceProvider.GetRequiredService<WindowContext>().MainWindow = mainWindow;
            desktop.MainWindow = mainWindow;
            mainWindow.Show();
            _serviceProvider.GetRequiredService<IThemeController>().ApplyTheme(settingsPageViewModel.SelectedThemeMode);
            desktop.Exit += OnDesktopExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async void OnDesktopExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        if (_serviceProvider is null)
        {
            return;
        }

        await _serviceProvider.GetRequiredService<ITransferReceiverHost>().StopAsync(CancellationToken.None);
        _serviceProvider.Dispose();
    }

    private ServiceProvider BuildServices()
    {
        ServiceCollection services = new();
        services.AddSingleton(this);
        services.AddSingleton<Application>(this);
        services.AddSingleton<WindowContext>();

        services.AddSingleton(new TransferProtocolOptions());
        services.AddSingleton<TransferProtocolSerializer>();
        services.AddSingleton<LocalCertificateStore>();

        services.AddSingleton<IThemeController, AvaloniaThemeController>();
        services.AddSingleton<IFilePickerService, AvaloniaFilePickerService>();
        services.AddSingleton<IFolderPickerService, AvaloniaFolderPickerService>();
        services.AddSingleton<ISettingsStore, SettingsJsonStore>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ILocalDeviceScanner, LocalDeviceScanner>();
        services.AddSingleton<ITrustOnFirstUseService, TrustOnFirstUseService>();
        services.AddSingleton<IProtocolActivationService, ProtocolActivationService>();
        services.AddSingleton<ITransferClient, TransferClient>();
        services.AddSingleton<ITransferOrchestrator, TransferOrchestrator>();
        services.AddSingleton<ITransferReceiverHost, TransferReceiverHost>();

        services.AddTransient<MainPageViewModel>();
        services.AddSingleton<SettingsPageViewModel>();
        services.AddSingleton<ShellViewModel>();

        return services.BuildServiceProvider();
    }
}