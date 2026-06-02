$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $root

$project = "src\MCAddonErsteller\MCAddonErsteller.csproj"
$publishDir = "src\MCAddonErsteller\bin\Release\net10.0-windows\win-x64\publish"
$releaseDir = "release"
$finalExe = Join-Path $releaseDir "MC Addon Ersteller.exe"

Write-Host "Publishing MC Addon Ersteller as single EXE for win-x64..."

dotnet publish $project -c Release -r win-x64 --self-contained true `
  /p:PublishSingleFile=true `
  /p:IncludeNativeLibrariesForSelfExtract=true `
  /p:EnableCompressionInSingleFile=true `
  /p:DebugType=None `
  /p:DebugSymbols=false

if (!(Test-Path $releaseDir)) {
  New-Item -ItemType Directory -Path $releaseDir | Out-Null
}

Copy-Item -Force (Join-Path $publishDir "MCAddonErsteller.exe") $finalExe

Write-Host ""
Write-Host "Fertig. EXE liegt hier:"
Write-Host (Join-Path $root $finalExe)
