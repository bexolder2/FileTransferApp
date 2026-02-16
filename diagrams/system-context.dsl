workspace "File Transfer App - System Context" "High-level system context: User and Desktop app." {

    model {
        user = person "User" "The primary user interacting with the file transfer application."

        desktopApp = softwareSystem "Desktop app" "Provide ability to send and receive files/folders." {
            tags ".NET 10" "Avalonia UI"
        }

        user -> desktopApp "Pick files/folders for send"
    }

    views {
        systemContext desktopApp "SystemContext" "System context diagram" {
            include *
            autolayout lr
        }
    }
}
