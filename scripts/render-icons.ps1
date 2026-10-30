# Renders the package PNGs in BookmarkSearch\Assets from Icon.light.svg.
# Re-run after changing the SVG. Uses Microsoft Edge (headless) to rasterize.

param(
    [string]$Svg = (Join-Path (Split-Path $PSScriptRoot -Parent) 'BookmarkSearch\Assets\Icon.light.svg')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$assets = Split-Path $Svg -Parent
$edge = @(
    "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe",
    "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $edge) { throw 'Microsoft Edge not found.' }

# Rasterize once at 1024px on a transparent background.
$work = Join-Path ([IO.Path]::GetTempPath()) "bookmarksearch-icons-$PID"
New-Item -ItemType Directory -Force $work | Out-Null
$svgUri = ([Uri](Resolve-Path $Svg).Path).AbsoluteUri
$html = Join-Path $work 'icon.html'
"<html><body style='margin:0;background:transparent'><img src='$svgUri' width='1024' height='1024'></body></html>" |
    Set-Content $html -Encoding UTF8
$master = Join-Path $work 'master.png'
& $edge --headless=new --disable-gpu --hide-scrollbars --default-background-color=00000000 `
    --window-size=1024,1024 "--screenshot=$master" ([Uri]$html).AbsoluteUri | Out-Null
for ($i = 0; $i -lt 50 -and -not (Test-Path $master); $i++) { Start-Sleep -Milliseconds 100 }
if (-not (Test-Path $master)) { throw 'Edge did not produce a screenshot.' }

$source = [System.Drawing.Image]::FromFile($master)

# File name, canvas width, canvas height, icon size (centered).
$targets = @(
    @('StoreLogo.png', 50, 50, 50),
    @('Square44x44Logo.scale-200.png', 88, 88, 88),
    @('Square44x44Logo.targetsize-16_altform-unplated.png', 16, 16, 16),
    @('Square44x44Logo.targetsize-24_altform-unplated.png', 24, 24, 24),
    @('Square44x44Logo.targetsize-32_altform-unplated.png', 32, 32, 32),
    @('Square44x44Logo.targetsize-48_altform-unplated.png', 48, 48, 48),
    @('Square44x44Logo.targetsize-256_altform-unplated.png', 256, 256, 256),
    @('Square150x150Logo.scale-200.png', 300, 300, 200),
    @('Wide310x150Logo.scale-200.png', 620, 300, 200),
    @('LockScreenLogo.scale-200.png', 48, 48, 48),
    @('SplashScreen.scale-200.png', 1240, 600, 400)
)

try {
    foreach ($t in $targets) {
        $name, $width, $height, $size = $t
        $bitmap = New-Object System.Drawing.Bitmap $width, $height
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $graphics.DrawImage($source, [int](($width - $size) / 2), [int](($height - $size) / 2), $size, $size)
        $graphics.Dispose()
        $bitmap.Save((Join-Path $assets $name), [System.Drawing.Imaging.ImageFormat]::Png)
        $bitmap.Dispose()
        Write-Host "$name  ${width}x$height"
    }
}
finally {
    $source.Dispose()
    Remove-Item $work -Recurse -Force
}
