# Satinalma Pro - derle ve calistir (PowerShell, Turkce yol guvenli)
$ErrorActionPreference = "Stop"
$kok = Split-Path -Parent $MyInvocation.MyCommand.Path
$proje = Join-Path $kok "Satinalma Pro\SatinalmaPro.csproj"

Get-Process SatinalmaPro, TalepPro -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host ""
Write-Host "Satinalma Pro derleniyor..." -ForegroundColor Cyan
dotnet build $proje -c Release --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    Write-Host "Derleme basarisiz." -ForegroundColor Red
    Read-Host "Devam icin Enter"
    exit 1
}

$talepProje = Join-Path $kok "TalepPro\TalepPro.csproj"
Write-Host "Talep Pro derleniyor..." -ForegroundColor Cyan
dotnet build $talepProje -c Release --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    Write-Host "Talep Pro derleme basarisiz." -ForegroundColor Red
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

# Dev: Talep Pro cikisini Pro yanina kopyala (exe + dll + runtimeconfig)
$talepDir = Join-Path $kok "TalepPro\bin\Release\net9.0-windows10.0.17763.0"
$talepExe = Join-Path $talepDir "TalepPro.exe"
$proDir = Split-Path $exe -Parent
if (Test-Path $talepExe) {
    foreach ($ad in @("TalepPro.exe", "TalepPro.dll", "TalepPro.runtimeconfig.json", "TalepPro.deps.json")) {
        $src = Join-Path $talepDir $ad
        if (Test-Path $src) { Copy-Item $src (Join-Path $proDir $ad) -Force }
    }
    # Talep Pro TP ikonu (kısayol / yan yana kurulum için ayrı dosya)
    $tpIco = Join-Path $kok "TalepPro\Assets\app.ico"
    if (Test-Path $tpIco) { Copy-Item $tpIco (Join-Path $proDir "TalepPro.ico") -Force }
}

Start-Process $exe
Write-Host "Uygulama acildi: $exe" -ForegroundColor Green
Write-Host "Talep Pro: $talepExe" -ForegroundColor Green
