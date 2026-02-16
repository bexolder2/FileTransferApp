using Avalonia;
using Avalonia.Styling;
using FileTransfer.Core.Contracts;
using FileTransfer.Core.Models;

namespace FileTransfer.App.Services;

public sealed class AvaloniaThemeController : IThemeController
{
    private readonly Application _application;
    private readonly WindowContext _windowContext;

    public AvaloniaThemeController(Application application, WindowContext windowContext)
    {
        _application = application;
        _windowContext = windowContext;
    }

    public void ApplyTheme(AppThemeMode themeMode)
    {
        _application.RequestedThemeVariant = themeMode switch
        {
            AppThemeMode.Light => ThemeVariant.Light,
            AppThemeMode.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };

        bool useDarkTitleBar = themeMode switch
        {
            AppThemeMode.Dark => true,
            AppThemeMode.Light => false,
            _ => _application.ActualThemeVariant == ThemeVariant.Dark
        };

        WindowsTitleBarThemeSync.Apply(_windowContext.MainWindow, useDarkTitleBar);
    }
}
