workspace "File Transfer App - Containers" "Containers context: UI and backend services." {

    model {
        user = person "User" "The primary user interacting with the file transfer application."

        fileTransferApp = softwareSystem "File Transfer App" "Provide ability to send and receive files/folders." {
            ui = container "UI" "Show files list to send." "Avalonia UI" {
                tags "Avalonia UI"
            }
            settingService = container "Setting service" "Get/set user settings. Store settings." ".NET 10" {
                tags ".NET 10"
            }
            uploadService = container "Upload service" "Manage uploads. Track progress." ".NET 10" {
                tags ".NET 10"
            }
            downloadService = container "Download service" "Manage downloads. Track progress." ".NET 10" {
                tags ".NET 10"
            }
        }

        user -> ui "Uses"
        ui -> settingService "Get/set settings"
        ui -> uploadService "Manage uploads"
        ui -> downloadService "Manage downloads"
    }

    views {
        container fileTransferApp "Containers" "Containers context diagram" {
            include *
            autolayout lr
        }
    }
}
