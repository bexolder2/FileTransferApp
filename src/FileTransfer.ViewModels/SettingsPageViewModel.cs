using System.Collections.ObjectModel;
using System.Windows.Input;
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
    private readonly NavigationCommandsHolder _navigationCommands;

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
    private IReadOnlyList<string> _previouslyScannedIps = [];
    private string? _lastSelectedTargetIp;

    public SettingsPageViewModel(
        ISettingsService settingsService,
        ILocalDeviceScanner localDeviceScanner,
        IThemeController themeController,
        IFolderPickerService folderPickerService,
        ITrustOnFirstUseService trustOnFirstUseService,
        NavigationCommandsHolder navigationCommands)
    {
        _settingsService = settingsService;
        _localDeviceScanner = localDeviceScanner;
        _themeController = themeController;
        _folderPickerService = folderPickerService;
        _trustOnFirstUseService = trustOnFirstUseService;
        _navigationCommands = navigationCommands;
        DetectedDevices = new ObservableCollection<DeviceInfo>();
    }

    public ICommand? NavigateToMainCommand => _navigationCommands.NavigateToMainCommand;

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
        _previouslyScannedIps = settings.PreviouslyScannedIps;
        _trustedPeerFingerprints = settings.TrustedPeerFingerprints;
        _trustedPeers = settings.TrustedPeers;
        PopulateDetectedDevicesFromSavedIps(_previouslyScannedIps);
        SelectedDevice = DetectedDevices.FirstOrDefault(device =>
            string.Equals(device.IpAddress, _lastSelectedTargetIp, StringComparison.Ordinal));

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
            IReadOnlyList<DeviceInfo> devices = await _localDeviceScanner.ScanAsync(cancellationToken, _previouslyScannedIps);
            DetectedDevices.Clear();
            foreach (DeviceInfo device in devices)
            {
                DetectedDevices.Add(device);
            }

            SelectedDevice = DetectedDevices.FirstOrDefault(device =>
                string.Equals(device.IpAddress, _lastSelectedTargetIp, StringComparison.Ordinal));
            await SavePreviouslyScannedIpsAsync(devices, cancellationToken);
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
        AppSettings settings = CreateSettingsSnapshot(SelectedDevice?.IpAddress, _previouslyScannedIps);

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

    private async Task SavePreviouslyScannedIpsAsync(IReadOnlyList<DeviceInfo> detectedDevices, CancellationToken cancellationToken)
    {
        HashSet<string> mergedIps = new(_previouslyScannedIps, StringComparer.Ordinal);
        foreach (DeviceInfo detectedDevice in detectedDevices)
        {
            mergedIps.Add(detectedDevice.IpAddress);
        }

        string[] normalizedMergedIps = mergedIps
            .OrderBy(ip => ip, StringComparer.Ordinal)
            .Take(512)
            .ToArray();

        if (_previouslyScannedIps.SequenceEqual(normalizedMergedIps, StringComparer.Ordinal))
        {
            return;
        }

        _previouslyScannedIps = normalizedMergedIps;
        AppSettings updated = CreateSettingsSnapshot(_lastSelectedTargetIp, _previouslyScannedIps);
        await _settingsService.SaveAsync(updated, cancellationToken);
    }

    private AppSettings CreateSettingsSnapshot(string? lastSelectedTargetIp, IReadOnlyList<string> previouslyScannedIps)
    {
        return new AppSettings
        {
            Version = 2,
            DownloadFolder = DownloadFolder,
            MaximumParallelUploads = Math.Max(1, MaximumParallelUploads),
            LastSelectedTargetIp = lastSelectedTargetIp,
            ThemeMode = SelectedThemeMode,
            PreviouslyScannedIps = previouslyScannedIps,
            TrustedPeerFingerprints = _trustedPeerFingerprints,
            TrustedPeers = _trustedPeers
        };
    }

    private void PopulateDetectedDevicesFromSavedIps(IReadOnlyList<string> ips)
    {
        DetectedDevices.Clear();
        foreach (string ip in ips.OrderBy(ip => ip, StringComparer.Ordinal))
        {
            DetectedDevices.Add(new DeviceInfo
            {
                IpAddress = ip,
                DisplayName = ip
            });
        }
    }
}
