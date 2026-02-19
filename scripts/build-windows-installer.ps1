param(
    [string]$Configuration = "Release",
    [ValidateSet("x64", "x86", "all")]
    [string]$Arch = "all"
)

$ErrorActionPreference = "Stop"
$Root = Resolve-Path (Join-Path $PSScriptRoot "..")
$BinDir = Join-Path $Root "bin"
$WixProj = Join-Path $Root "installer\windows\FileTransfer.Installer.wixproj"

New-Item -ItemType Directory -Force $BinDir | Out-Null

$architectures = switch ($Arch) { "x64" { "x64" }; "x86" { "x86" }; "all" { "x64", "x86" } }

foreach ($platform in $architectures) {
    $rid = "win-$platform"
    $PublishDir = Join-Path $Root "artifacts\publish\$rid"
    $MsiOutput = Join-Path $BinDir "file-transfer-app-windows-$platform.msi"
    $ZipOutput = Join-Path $BinDir "file-transfer-app-windows-$platform.zip"

    New-Item -ItemType Directory -Force $PublishDir | Out-Null

    dotnet publish (Join-Path $Root "src\FileTransfer.App\FileTransfer.App.csproj") `
        -c $Configuration `
        -r $rid `
        --self-contained false `
        -o $PublishDir

    dotnet build $WixProj -c $Configuration -p:PublishDir=$PublishDir -p:InstallerPlatform=$platform

    $BuiltMsi = Join-Path $BinDir "$Configuration\$platform\file-transfer-app-windows-$platform.msi"
    if (Test-Path $BuiltMsi) {
        Copy-Item $BuiltMsi $MsiOutput -Force
        Write-Host "Created MSI: $MsiOutput"
    } else {
        if (Test-Path $ZipOutput) {
            Remove-Item $ZipOutput -Force
        }
        Compress-Archive -Path (Join-Path $PublishDir "*") -DestinationPath $ZipOutput
        Write-Host "Installer build failed for $platform, created ZIP fallback: $ZipOutput"
    }
}
