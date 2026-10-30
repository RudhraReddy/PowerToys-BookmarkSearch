# Builds the extension and registers it with Windows so Command Palette can load it.
# Requires Developer Mode (Settings > System > For developers).
# After it finishes, run "Reload" in Command Palette.

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',

    [ValidateSet('x64', 'ARM64')]
    [string]$Platform = 'x64'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root 'BookmarkSearch\BookmarkSearch.csproj'

# Stop a running instance first; it locks the files the build replaces.
Get-Process BookmarkSearch -ErrorAction SilentlyContinue | Stop-Process -Force

dotnet build $project -c $Configuration -p:Platform=$Platform -nologo
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

$rid = if ($Platform -eq 'ARM64') { 'win-arm64' } else { 'win-x64' }
$manifest = Get-ChildItem (Join-Path $root "BookmarkSearch\bin\$Platform\$Configuration") -Recurse -Filter AppxManifest.xml |
    Where-Object { $_.DirectoryName -like "*\$rid" } |
    Select-Object -First 1
if (-not $manifest) { throw 'AppxManifest.xml not found in build output.' }

Add-AppxPackage -Register $manifest.FullName -ForceApplicationShutdown
Write-Host 'Deployed. Run "Reload" in Command Palette to pick up the changes.'
