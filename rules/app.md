## File Transfer app

App allow transfer files between 2 devices (Windows or Linux), user able to select any amount of files/folders to trasfer. App show uploading progress on UI.  
During code generation use files from /rules folder to get information about app/code style, etc. Use /design/design-concept.png for design reference generated code should follow it. Use files from /diagrams to understand app architecture. 

### UX

1. App has 2 pages: Main and Settings
2. Main page scenarious:
- User click **'add file'** button: default system file picker opened, after selecting file added to TreeView on main page
- User click **'add folder'** button: default system folder picker opened, after selecting folder added to TreeView on main page
- User click **'clear queue'** button: all selected files removed from upload queue and TreeView on main page
- User unselect file/folder on TreeView: unselected file/folder removed from upload queue
- User click **'start upload'** button: uploading started, button changed to **'cancel'** button, progress showed on progress bar from 0 to N, where N number of files to upload.
> - User click **'cancel'**: upload stopped, button changed back to **'start upload'**
- User click **'settings'** icon: app navigated to settings page

3. Settings page scenarious:
- User click **'detect devices'**: local network scanned, all IPs added to combobox. Selected IP in combobox will used for uploading
- User enter number in **'maximum parallel uploads'**: this value used for concurrent uploading
- User click **'browse'**: default system folder picker opened, after selecting folder added to TextBox, and will be used for saving incoming files
- User click **'light'** , **'dark'** or **'system'** RadioButton: app theme updated in realtime for all pages/controls

4. App have adaptive UI, minimum window contant size 500x500

### UI

1. App used default AvaloniaUI controls
2. Settings page content centered by horizontal and can be scrolled by vertical

### App logic, implementation details

1. By default app is client/server app it can send and receive files.
2. App use Raw TCP + Streaming for files transferring. Files larger than 10Mb should be devided to chunks by 5Mb each. App should avoid loading large files into memory and use buffered streams. After transfering memory should be disposed. File transfering must use async pattern. File transfer should be secured using SslStream.
3. For crossplatform implementation app must use Avalonia UI v11.3.12 and .NET 10. App must follow MVVM pattern, models/view models/view should be placed in different projects, use sdk style projects and CPM (Central Package Management).
4. App packaging: for Windows version WiX installer used, for linux .deb packages used (initially Debian support is mandatory).
5. Settings should be saved in json file in app directory
6. Default folder for downloads is User/Downloads full path depends on system (Windows/Linux)
7. App should have own protocol and support activation by protocol

### General folder structure

/src - for source code  
/test - for tests projects  
/installer - for installer project  
/bin - for packed installers  
/scripts - for any scripts  