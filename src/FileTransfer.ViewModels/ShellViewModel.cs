using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FileTransfer.ViewModels;

public sealed partial class ShellViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentPageViewModel;

    public ShellViewModel(
        MainPageViewModel mainPageViewModel,
        SettingsPageViewModel settingsPageViewModel,
        NavigationCommandsHolder navigationCommands)
    {
        MainPageViewModel = mainPageViewModel;
        SettingsPageViewModel = settingsPageViewModel;
        CurrentPageViewModel = MainPageViewModel;
        navigationCommands.NavigateToMainCommand = NavigateToMainCommand;
        navigationCommands.NavigateToSettingsCommand = NavigateToSettingsCommand;
    }

    public MainPageViewModel MainPageViewModel { get; }

    public SettingsPageViewModel SettingsPageViewModel { get; }

    public bool IsMainPageActive => ReferenceEquals(CurrentPageViewModel, MainPageViewModel);

    public bool IsSettingsPageActive => ReferenceEquals(CurrentPageViewModel, SettingsPageViewModel);

    [RelayCommand]
    private void NavigateToMain()
    {
        CurrentPageViewModel = MainPageViewModel;
        OnPropertyChanged(nameof(IsMainPageActive));
        OnPropertyChanged(nameof(IsSettingsPageActive));
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        CurrentPageViewModel = SettingsPageViewModel;
        OnPropertyChanged(nameof(IsMainPageActive));
        OnPropertyChanged(nameof(IsSettingsPageActive));
    }
}
