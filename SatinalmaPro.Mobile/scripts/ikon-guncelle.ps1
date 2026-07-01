# Masaüstü app.ico dosyasindan Android ikonlarini gunceller.
# Kaynak: ..\Satinalma Pro\Assets\app.ico

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$root = Split-Path $PSScriptRoot -Parent
$icoPath = Join-Path $root "..\Satinalma Pro\Assets\app.ico"
$outDir = Join-Path $root "Resources\AppIcon"
$assetsDir = Join-Path $root "Assets"
$imagesDir = Join-Path $root "Resources\Images"

if (-not (Test-Path $icoPath)) {
    Write-Error "Bulunamadi: $icoPath"
}

New-Item -ItemType Directory -Force -Path $outDir, $assetsDir, $imagesDir | Out-Null
Copy-Item $icoPath (Join-Path $assetsDir "app.ico") -Force

$icon = New-Object System.Drawing.Icon($icoPath)
$bmp = $icon.ToBitmap()
$target = 1024
$scaled = New-Object System.Drawing.Bitmap($target, $target)
$g = [System.Drawing.Graphics]::FromImage($scaled)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$g.Clear([System.Drawing.Color]::White)
$pad = [int]($target * 0.08)
$inner = $target - 2 * $pad
$g.DrawImage($bmp, $pad, $pad, $inner, $inner)

$pngPath = Join-Path $outDir "appicon.png"
$scaled.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)
Copy-Item $pngPath (Join-Path $imagesDir "app_logo.png") -Force

$g.Dispose(); $scaled.Dispose(); $bmp.Dispose(); $icon.Dispose()
Write-Host "Ikon guncellendi: $pngPath"
