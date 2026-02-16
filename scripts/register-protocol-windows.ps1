$scheme = "filetransfer"
$applicationPath = Join-Path $PSScriptRoot "..\src\FileTransfer.App\bin\Debug\net10.0\FileTransfer.App.exe"
$resolvedPath = (Resolve-Path $applicationPath).Path

$baseKey = "Registry::HKEY_CURRENT_USER\Software\Classes\$scheme"
New-Item -Path $baseKey -Force | Out-Null
Set-ItemProperty -Path $baseKey -Name "(Default)" -Value "URL:File Transfer Protocol"
Set-ItemProperty -Path $baseKey -Name "URL Protocol" -Value ""

$commandKey = Join-Path $baseKey "shell\open\command"
New-Item -Path $commandKey -Force | Out-Null
Set-ItemProperty -Path $commandKey -Name "(Default)" -Value "`"$resolvedPath`" `"%1`""

Write-Host "Registered protocol '$scheme' for current user."
