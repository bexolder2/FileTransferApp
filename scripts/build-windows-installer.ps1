param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$Root = Resolve-Path (Join-Path $PSScriptRoot "..")
$PublishDir = Join-Path $Root "artifacts\publish\win-x64"
$BinDir = Join-Path $Root "bin"
$WixSource = Join-Path $Root "installer\windows\Product.wxs"
$MsiOutput = Join-Path $BinDir "file-transfer-app-windows-x64.msi"
$ZipOutput = Join-Path $BinDir "file-transfer-app-windows-x64.zip"

New-Item -ItemType Directory -Force $PublishDir | Out-Null
New-Item -ItemType Directory -Force $BinDir | Out-Null

dotnet publish (Join-Path $Root "src\FileTransfer.App\FileTransfer.App.csproj") `
    -c $Configuration `
    -r win-x64 `
    --self-contained false `
    -o $PublishDir

$wix = Get-Command wix -ErrorAction SilentlyContinue
if ($null -ne $wix) {
    wix build $WixSource -d PublishDir=$PublishDir -o $MsiOutput
    Write-Host "Created MSI: $MsiOutput"
} else {
    if (Test-Path $ZipOutput) {
        Remove-Item $ZipOutput -Force
    }
    Compress-Archive -Path (Join-Path $PublishDir "*") -DestinationPath $ZipOutput
    Write-Host "WiX not found, created ZIP fallback: $ZipOutput"
}
