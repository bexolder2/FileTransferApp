workspace "File Transfer App - Settings Service Components" "Component context for Settings service." {

    model {
        fileTransferApp = softwareSystem "File Transfer App" "Desktop file transfer application." {
            settingService = container "Setting service" "Get/set user settings. Store settings." ".NET 10" {
                localDevicesScanning = component "Local devices Scanning component" "Scan devices in local network." ""
                themeController = component "Theme controller component" "Change UI theme for app in realtime." ""
                settingsStore = component "Settings store component" "Get/set user settings in json file." ""

                localDevicesScanning -> settingsStore "Read/write settings"
                themeController -> settingsStore "Read/write theme settings"
            }
        }

        avaloniaApis = softwareSystem "Avalonia UI crossplatform APIs" "Cross-platform UI and system APIs." "External"
        settingsStore -> avaloniaApis "Uses"
    }

    views {
        component settingService "SettingsServiceComponents" "Settings service component context" {
            include *
            autolayout lr
        }
    }
}
