using FileTransfer.Core.Models;

namespace FileTransfer.Core.Contracts;

public interface IThemeController
{
    void ApplyTheme(AppThemeMode themeMode);
}
