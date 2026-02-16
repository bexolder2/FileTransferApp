using Avalonia;
using Avalonia.Styling;
using FileTransfer.Core.Contracts;
using FileTransfer.Core.Models;

namespace FileTransfer.App.Services;

public sealed class AvaloniaThemeController : IThemeController
{
    private readonly Application _application;

    public AvaloniaThemeController(Application application)
    {
        _application = application;
    }

    public void ApplyTheme(AppThemeMode themeMode)
    {
        _application.RequestedThemeVariant = themeMode switch
        {
            AppThemeMode.Light => ThemeVariant.Light,
            AppThemeMode.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }
}
