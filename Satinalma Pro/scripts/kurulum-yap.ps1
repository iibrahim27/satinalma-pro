# Satinalma Pro — publish + Inno Setup kurulum exe + zip
# Kullanim: .\scripts\kurulum-yap.ps1 -Version "1.2.0"
# Surum verilmezse SatinalmaPro.csproj icindeki surum kullanilir.

param(
    [string]$Version = "",
    [string]$GitHubKullanici = "iibrahim27",
    [string]$RepoAdi = "satinalma-pro",
    [string]$Notes = ""
)

$ErrorActionPreference = "Stop"
$projeKok = Split-Path $PSScriptRoot -Parent
Set-Location $projeKok

if (-not $Version) {
    $csproj = Join-Path $projeKok "SatinalmaPro.csproj"
    $Version = [regex]::Match((Get-Content $csproj -Raw), '<Version>([\d.]+)</Version>').Groups[1].Value
    if (-not $Version) { throw "Surum bulunamadi. Kullanim: kurulum-yap.ps1 -Version 1.2.0" }
    Write-Host "Surum csproj'den okundu: $Version" -ForegroundColor Yellow
}

Write-Host "=== Satinalma Pro kurulum paketi ===" -ForegroundColor Cyan
Write-Host "Surum: $Version`n"

Write-Host "[1/5] Surum dosyalari guncelleniyor..."
& (Join-Path $PSScriptRoot "surum-guncelle.ps1") -Version $Version -GitHubKullanici $GitHubKullanici -RepoAdi $RepoAdi -Notes $Notes

$csprojYol = Join-Path $projeKok "SatinalmaPro.csproj"
$csprojRaw = Get-Content $csprojYol -Raw -Encoding UTF8
$tfm = [regex]::Match($csprojRaw, '<TargetFramework>([^<]+)</TargetFramework>').Groups[1].Value
if (-not $tfm) { throw "TargetFramework bulunamadi: $csprojYol" }

Write-Host "`n[2/5] Release derleniyor ($tfm, win-x64, self-contained)..."
dotnet publish $csprojYol -c Release -r win-x64 --self-contained true `
    -p:UseAppHost=true `
    -p:Version=$Version `
    -p:AssemblyVersion="$Version.0" `
    -p:FileVersion="$Version.0" `
    -p:InformationalVersion=$Version
if ($LASTEXITCODE -ne 0) { throw "dotnet publish basarisiz (cikis: $LASTEXITCODE)" }

$publish = Join-Path $projeKok "bin\Release\$tfm\win-x64\publish"
if (-not (Test-Path $publish)) {
    throw "Publish klasoru bulunamadi: $publish"
}

Write-Host "`n[3/5] Firebase dosyalari kopyalaniyor..."
$firebaseYol = Join-Path $projeKok "firebase_ayarlar.json"
if (Test-Path $firebaseYol) {
    Copy-Item $firebaseYol (Join-Path $publish "firebase_ayarlar.json") -Force
}
$fcmYol = Join-Path $projeKok "fcm-service-account.json"
if (Test-Path $fcmYol) {
    Copy-Item $fcmYol (Join-Path $publish "fcm-service-account.json") -Force
    Write-Host "  fcm-service-account.json eklendi" -ForegroundColor Green
} else {
    Write-Host "  UYARI: fcm-service-account.json yok - push bildirimleri calismaz." -ForegroundColor Yellow
}
$googleYol = Join-Path $projeKok "google-services.json"
if (Test-Path $googleYol) {
    Copy-Item $googleYol (Join-Path $publish "google-services.json") -Force
    Write-Host "  google-services.json eklendi" -ForegroundColor Green
}

Write-Host "`n[4/5] Zip olusturuluyor..."
$zipAdi = "SatinalmaPro.zip"
$zipYol = Join-Path $projeKok $zipAdi
if (Test-Path $zipYol) { Remove-Item $zipYol -Force }
Compress-Archive -Path "$publish\*" -DestinationPath $zipYol -CompressionLevel Optimal

Write-Host "`n[5/5] Inno Setup kurulum exe derleniyor..."
$kurulumExe = Join-Path $projeKok "SatinalmaPro_Kurulum.exe"
$iscc = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $iscc) {
    Write-Host "HATA: Inno Setup 6 bulunamadi." -ForegroundColor Red
    Write-Host "Kurun: https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
    exit 1
}

$issYol = Join-Path $projeKok "installer\SatinalmaPro.iss"
& $iscc $issYol
if ($LASTEXITCODE -ne 0) { throw "Inno Setup derlemesi basarisiz (cikis: $LASTEXITCODE)" }

if (-not (Test-Path $kurulumExe)) {
    throw "Kurulum exe olusturulamadi: $kurulumExe"
}

Write-Host "`n[6/6] Android APK derleniyor (3-8 dakika)..."
$apkScript = Join-Path (Split-Path $projeKok -Parent) "SatinalmaPro.Mobile\scripts\apk-olustur.ps1"
if (-not (Test-Path $apkScript)) {
    throw "APK script bulunamadi: $apkScript"
}

& $apkScript
if ($LASTEXITCODE -ne 0) { throw "APK derlemesi basarisiz (cikis: $LASTEXITCODE)" }

$apkKaynak = Join-Path ([Environment]::GetFolderPath('Desktop')) "SatinalmaPro.apk"
$apkHedef = Join-Path $projeKok "SatinalmaPro.apk"
if (-not (Test-Path $apkKaynak)) {
    throw "APK bulunamadi: $apkKaynak"
}
Copy-Item $apkKaynak $apkHedef -Force
Write-Host "APK kopyalandi: $apkHedef"

Write-Host "`n=== TAMAMLANDI ===" -ForegroundColor Green
Write-Host "Surum:           $Version"
Write-Host "Kurulum exe:     $kurulumExe"
Write-Host "Zip:             $zipYol"
Write-Host "APK:             $apkHedef"
Write-Host "version.json:    $(Join-Path $projeKok 'version.json')"
Write-Host ""
Write-Host "GitHub Release v$Version icin yukleyin:" -ForegroundColor Yellow
Write-Host "  - version.json (repoda guncelle)"
Write-Host "  - SatinalmaPro_Kurulum.exe"
Write-Host "  - SatinalmaPro.zip (istege bagli)"
Write-Host "  - SatinalmaPro.apk"
