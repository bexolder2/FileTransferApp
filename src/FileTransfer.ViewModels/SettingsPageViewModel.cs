using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileTransfer.Core.Contracts;
using FileTransfer.Core.Models;

namespace FileTransfer.ViewModels;

public sealed partial class SettingsPageViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly ILocalDeviceScanner _localDeviceScanner;
    private readonly IThemeController _themeController;
    private readonly IFolderPickerService _folderPickerService;
    private readonly ITrustOnFirstUseService _trustOnFirstUseService;

    [ObservableProperty]
    private DeviceInfo? _selectedDevice;

    [ObservableProperty]
    private int _maximumParallelUploads = 2;

    [ObservableProperty]
    private string _downloadFolder = string.Empty;

    [ObservableProperty]
    private AppThemeMode _selectedThemeMode = AppThemeMode.System;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _securityStatus = string.Empty;

    private IReadOnlyList<string> _trustedPeerFingerprints = [];
    private IReadOnlyList<TrustedPeer> _trustedPeers = [];
    private string? _lastSelectedTargetIp;

    public SettingsPageViewModel(
        ISettingsService settingsService,
        ILocalDeviceScanner localDeviceScanner,
        IThemeController themeController,
        IFolderPickerService folderPickerService,
        ITrustOnFirstUseService trustOnFirstUseService)
    {
        _settingsService = settingsService;
        _localDeviceScanner = localDeviceScanner;
        _themeController = themeController;
        _folderPickerService = folderPickerService;
        _trustOnFirstUseService = trustOnFirstUseService;
        DetectedDevices = new ObservableCollection<DeviceInfo>();
    }

    public ObservableCollection<DeviceInfo> DetectedDevices { get; }

    public bool IsSystemThemeSelected
    {
        get => SelectedThemeMode == AppThemeMode.System;
        set
        {
            if (value)
            {
                SelectedThemeMode = AppThemeMode.System;
            }
        }
    }

    public bool IsLightThemeSelected
    {
        get => SelectedThemeMode == AppThemeMode.Light;
        set
        {
            if (value)
            {
                SelectedThemeMode = AppThemeMode.Light;
            }
        }
    }

    public bool IsDarkThemeSelected
    {
        get => SelectedThemeMode == AppThemeMode.Dark;
        set
        {
            if (value)
            {
                SelectedThemeMode = AppThemeMode.Dark;
            }
        }
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        AppSettings settings = await _settingsService.GetAsync(cancellationToken);
        _lastSelectedTargetIp = settings.LastSelectedTargetIp;
        MaximumParallelUploads = settings.MaximumParallelUploads;
        DownloadFolder = settings.DownloadFolder;
        SelectedThemeMode = settings.ThemeMode;
        _trustedPeerFingerprints = settings.TrustedPeerFingerprints;
        _trustedPeers = settings.TrustedPeers;

        _themeController.ApplyTheme(SelectedThemeMode);
    }

    [RelayCommand]
    private async Task DetectDevicesAsync(CancellationToken cancellationToken)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            IReadOnlyList<DeviceInfo> devices = await _localDeviceScanner.ScanAsync(cancellationToken);
            DetectedDevices.Clear();
            foreach (DeviceInfo device in devices)
            {
                DetectedDevices.Add(device);
            }

            SelectedDevice = DetectedDevices.FirstOrDefault(device =>
                string.Equals(device.IpAddress, _lastSelectedTargetIp, StringComparison.Ordinal));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task BrowseDownloadFolderAsync(CancellationToken cancellationToken)
    {
        string? selectedFolder = await _folderPickerService.PickFolderAsync("Select download folder", cancellationToken);
        if (!string.IsNullOrWhiteSpace(selectedFolder))
        {
            DownloadFolder = selectedFolder;
            await SaveAsync(cancellationToken);
        }
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        AppSettings settings = new()
        {
            Version = 2,
            DownloadFolder = DownloadFolder,
            MaximumParallelUploads = Math.Max(1, MaximumParallelUploads),
            LastSelectedTargetIp = SelectedDevice?.IpAddress,
            ThemeMode = SelectedThemeMode,
            TrustedPeerFingerprints = _trustedPeerFingerprints,
            TrustedPeers = _trustedPeers
        };

        _lastSelectedTargetIp = settings.LastSelectedTargetIp;
        await _settingsService.SaveAsync(settings, cancellationToken);
    }

    [RelayCommand]
    private async Task RetrustSelectedDeviceAsync(CancellationToken cancellationToken)
    {
        if (SelectedDevice is null)
        {
            SecurityStatus = "Select a device to re-trust.";
            return;
        }

        await _trustOnFirstUseService.RemoveTrustForPeerAsync(SelectedDevice.IpAddress, cancellationToken);
        AppSettings refreshed = await _settingsService.GetAsync(cancellationToken);
        _trustedPeerFingerprints = refreshed.TrustedPeerFingerprints;
        _trustedPeers = refreshed.TrustedPeers;
        SecurityStatus = $"Trust entry removed for {SelectedDevice.IpAddress}. Next connection will trust on first use.";
    }

    partial void OnSelectedThemeModeChanged(AppThemeMode value)
    {
        _themeController.ApplyTheme(value);
        OnPropertyChanged(nameof(IsSystemThemeSelected));
        OnPropertyChanged(nameof(IsLightThemeSelected));
        OnPropertyChanged(nameof(IsDarkThemeSelected));
    }
}
