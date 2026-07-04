# Satinalma Pro - derle ve calistir (PowerShell, Turkce yol guvenli)
$ErrorActionPreference = "Stop"
$kok = Split-Path -Parent $MyInvocation.MyCommand.Path
$proje = Join-Path $kok "Satinalma Pro\SatinalmaPro.csproj"

Get-Process SatinalmaPro -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host ""
Write-Host "Satinalma Pro derleniyor..." -ForegroundColor Cyan
dotnet build $proje -c Release --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    Write-Host "Derleme basarisiz." -ForegroundColor Red
    Read-Host "Devam icin Enter"
    exit 1
}

$exe = Join-Path $kok "Satinalma Pro\bin\Release\net9.0-windows10.0.17763.0\SatinalmaPro.exe"
if (-not (Test-Path $exe)) {
    $exe = Join-Path $kok "Satinalma Pro\bin\Debug\net9.0-windows10.0.17763.0\SatinalmaPro.exe"
}
if (-not (Test-Path $exe)) {
    Write-Host "Hata: SatinalmaPro.exe bulunamadi." -ForegroundColor Red
    Read-Host "Devam icin Enter"
    exit 1
}

Start-Process $exe
Write-Host "Uygulama acildi: $exe" -ForegroundColor Green
