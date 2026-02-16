workspace "File Transfer App - Transfer Services Components" "Component context for Uploading, Progress tracking, and Download services." {

    model {
        fileTransferApp = softwareSystem "File Transfer App" "Desktop file transfer application." {
            uploadingService = container "Uploading service" "Manage uploads." ".NET 10" {
                userInteraction = component "User interaction component" "Add picked files/folders to queue. Upload files to selected IP. Cancel uploading." ""
            }
            progressTrackingService = container "Progress tracking service" "Track upload and download progress." ".NET 10" {
                progressTracking = component "Progress tracking component" "Show progress on UI. Handle progress messages." ""
            }
            downloadService = container "Download service" "Manage downloads." ".NET 10" {
                receiveHandler = component "Receive handler component" "Save received files to selected folder. Handle receive messages." ""
            }
            userInteraction -> progressTrackingService "Reports progress"
            receiveHandler -> progressTrackingService "Reports progress"
        }
    }

    views {
        container fileTransferApp "TransferContainers" "Uploading, Progress tracking, and Download services (draft 4 overview)" {
            include uploadingService progressTrackingService downloadService
            autolayout lr
        }
        component uploadingService "UploadingServiceComponents" "Uploading service component context" {
            include *
            autolayout lr
        }
        component progressTrackingService "ProgressTrackingServiceComponents" "Progress tracking service component context" {
            include *
            autolayout lr
        }
        component downloadService "DownloadServiceComponents" "Download service component context" {
            include *
            autolayout lr
        }
    }
}
