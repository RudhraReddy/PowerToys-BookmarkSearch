# Builds Release MSIX packages for x64 and ARM64 and bundles them into one
# .msixbundle for Microsoft Store upload (Partner Center signs it).
#
# First Store build: pass the values from Partner Center > Product identity.
# They are written into Package.appxmanifest, so later builds don't need them.
#
#   .\scripts\package-store.ps1 -IdentityName "12345RudhraReddy.BookmarkSearch" `
#       -Publisher "CN=XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX" -Version 1.0.0.0

param(
    [string]$IdentityName,
    [string]$Publisher,
    [ValidatePattern('^\d+\.\d+\.\d+\.0$')]
    [string]$Version
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root 'BookmarkSearch\BookmarkSearch.csproj'
$manifestPath = Join-Path $root 'BookmarkSearch\Package.appxmanifest'
$outDir = Join-Path $root 'AppPackages'

# Update identity and version in the manifest when given.
[xml]$manifest = Get-Content $manifestPath -Raw
$identity = $manifest.Package.Identity
if ($IdentityName) { $identity.Name = $IdentityName }
if ($Publisher) { $identity.Publisher = $Publisher }
if ($Version) { $identity.Version = $Version }
if ($IdentityName -or $Publisher -or $Version) {
    $manifest.Save($manifestPath)
}
$Version = $identity.Version
Write-Host "Packaging $($identity.Name) $Version ($($identity.Publisher))"

if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }
$msixDir = Join-Path $outDir 'msix'
New-Item -ItemType Directory -Force $msixDir | Out-Null

foreach ($platform in 'x64', 'ARM64') {
    $platformDir = Join-Path $outDir $platform
    dotnet build $project -c Release -p:Platform=$platform -p:GenerateAppxPackageOnBuild=true `
        -p:AppxPackageSigningEnabled=false "-p:AppxPackageDir=$platformDir\" -nologo -v:minimal
    if ($LASTEXITCODE -ne 0) { throw "Build failed for $platform." }

    $msix = Get-ChildItem $platformDir -Recurse -Filter '*.msix' | Select-Object -First 1
    if (-not $msix) { throw "No .msix produced for $platform." }
    Copy-Item $msix.FullName $msixDir
}

# makeappx ships with the Windows SDK; use the newest installed version.
$makeappx = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\makeappx.exe" |
    Sort-Object { [version]$_.Directory.Parent.Name } -Descending |
    Select-Object -First 1
if (-not $makeappx) { throw 'makeappx.exe not found. Install the Windows SDK.' }

$bundle = Join-Path $outDir "$($identity.Name)_$($Version)_x64_arm64.msixbundle"
& $makeappx.FullName bundle /d $msixDir /p $bundle /bv $Version /o
if ($LASTEXITCODE -ne 0) { throw 'makeappx bundle failed.' }

Write-Host "`nUpload this to Partner Center:`n  $bundle"
