Import-Module "d:\_Backup\Configuration\SSL\Tools\app_signModule.ps1" -Force

[string[]]$appFiles = @(
    "..\E2EETool\bin\Release\publish\JocysCom.Tools.E2EETool.exe"
)
[string]$appName = "Jocys.com End-to-End Encryption Tool"
[string]$appLink = "https://www.jocys.com"

ProcessFiles $appName $appLink $appFiles
pause
