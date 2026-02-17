using System.Windows.Input;

namespace FileTransfer.ViewModels;

/// <summary>
/// Holds navigation commands so page view models can trigger shell navigation without depending on ShellViewModel.
/// ShellViewModel sets these; MainPageViewModel and SettingsPageViewModel expose them for binding.
/// </summary>
public sealed class NavigationCommandsHolder
{
    public ICommand? NavigateToMainCommand { get; set; }
    public ICommand? NavigateToSettingsCommand { get; set; }
}
