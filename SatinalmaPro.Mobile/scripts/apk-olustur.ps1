# Satinalma Pro Mobile — APK olustur
# Android derlemesi Turkce karakter / OneDrive yollarinda bozulabildigi icin
# her zaman C:\SatinalmaBuild uzerinden yapilir.

$ErrorActionPreference = "Stop"

$sdk = "C:\Users\pekba\AppData\Local\Android\Sdk"
$java = "C:\Program Files\Android\Android Studio\jbr"
$buildKok = "C:\SatinalmaBuild"

if (-not (Test-Path "$sdk\platform-tools\adb.exe")) {
    Write-Host "Android SDK bulunamadi. Android Studio -> SDK Manager'dan kurun." -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $java)) {
    Write-Host "Java (JBR) bulunamadi: $java" -ForegroundColor Red
    Write-Host "Android Studio kurulu mu kontrol edin." -ForegroundColor Red
    exit 1
}

$kaynakKok = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path

Write-Host "Kaynak proje: $kaynakKok" -ForegroundColor DarkGray
Write-Host "Derleme klasoru: $buildKok (ASCII yol, OneDrive disi)" -ForegroundColor Yellow

if (Test-Path $buildKok) {
    Remove-Item $buildKok -Recurse -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 500
}

New-Item -ItemType Directory -Path $buildKok -Force | Out-Null

$robocopyArgs = @(
    $kaynakKok, $buildKok,
    "/E", "/XD", "bin", "obj", ".git",
    "/NFL", "/NDL", "/NJH", "/NJS", "/nc", "/ns", "/np"
)
& robocopy @robocopyArgs | Out-Null
$robocopyExit = $LASTEXITCODE
if ($robocopyExit -ge 8) {
    Write-Host "Kopyalama basarisiz (robocopy cikis: $robocopyExit)." -ForegroundColor Red
    exit 1
}

$projeYolu = Join-Path $buildKok "SatinalmaPro.Mobile\SatinalmaPro.Mobile.csproj"
if (-not (Test-Path $projeYolu)) {
    Write-Host "Proje dosyasi bulunamadi: $projeYolu" -ForegroundColor Red
    exit 1
}

$env:ANDROID_HOME = $sdk
$env:AndroidSdkDirectory = $sdk
$env:JAVA_HOME = $java
$env:NUGET_PACKAGES = "$env:USERPROFILE\.nuget\packages"

$projeKlasor = Split-Path $projeYolu -Parent

function ApkBul {
    param([string]$Kok)
    Get-ChildItem -Path $Kok -Recurse -Filter "*-Signed.apk" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
}

Write-Host ""
Write-Host "Temizleniyor..." -ForegroundColor DarkGray
& dotnet clean $projeYolu -c Release -f net9.0-android -r android-arm64 --nologo -v q
# clean basarisiz olsa bile devam et

Write-Host "APK derleniyor (Release, 3-8 dk)..." -ForegroundColor Cyan
Write-Host ""

$publishArgs = @(
    "publish", $projeYolu,
    "-c", "Release",
    "-f", "net9.0-android",
    "-r", "android-arm64",
    "-p:AndroidSdkDirectory=$sdk",
    "-p:JavaSdkDirectory=$java",
    "-p:AcceptAndroidSDKLicenses=true"
)

& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "dotnet publish basarisiz (cikis: $LASTEXITCODE)." -ForegroundColor Red
    exit 1
}

$apk = ApkBul $buildKok

if (-not $apk) {
    Write-Host "Publish APK uretmedi, SignAndroidPackage deneniyor..." -ForegroundColor Yellow
    $buildArgs = @(
        "build", $projeYolu,
        "-c", "Release",
        "-f", "net9.0-android",
        "-r", "android-arm64",
        "-t:SignAndroidPackage",
        "-p:AndroidSdkDirectory=$sdk",
        "-p:JavaSdkDirectory=$java",
        "-p:AcceptAndroidSDKLicenses=true"
    )
    & dotnet @buildArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "dotnet build basarisiz (cikis: $LASTEXITCODE)." -ForegroundColor Red
        exit 1
    }
    $apk = ApkBul $buildKok
}

if (-not $apk) {
    Write-Host ""
    Write-Host "APK olusturulamadi. Derleme ciktisini yukarida kontrol edin." -ForegroundColor Red
    exit 1
}

$hedef = Join-Path ([Environment]::GetFolderPath('Desktop')) "SatinalmaPro.apk"
Copy-Item $apk.FullName $hedef -Force
Write-Host ""
Write-Host "APK hazir:" -ForegroundColor Green
Write-Host "  $hedef"
Write-Host "  ($([math]::Round((Get-Item $hedef).Length / 1MB, 1)) MB)"
Write-Host "  Kaynak: $($apk.FullName)"
