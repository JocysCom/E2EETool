
# Make sure the output directories exist
$filesDir = Join-Path $PSScriptRoot "Files"
$binDir = Join-Path $PSScriptRoot "..\Resources"

$file1="JocysCom.Tools.E2EETool.exe"
if (-not [System.IO.File]::Exists([System.IO.Path]::Combine($filesDir, $file1))){
    [System.IO.File]::Copy([System.IO.Path]::Combine($PSScriptRoot, "..\App\bin\Release\publish\", $file1), [System.IO.Path]::Combine($filesDir, $file1))
}
& "$binDir\ZipFiles.ps1" $filesDir "$filesDir\JocysCom.Tools.E2EETool.zip" $file1 $true

