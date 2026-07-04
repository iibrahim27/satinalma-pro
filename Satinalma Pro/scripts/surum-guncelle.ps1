# Tum surum dosyalarini tek surume esitler: csproj, version.json, SatinalmaPro.iss
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [string]$GitHubKullanici = "iibrahim27",
    [string]$RepoAdi = "satinalma-pro",
    [string]$Notes = ""
)

$ErrorActionPreference = "Stop"

if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    throw "Surum formati gecersiz: $Version (ornek: 1.2.0)"
}

$projeKok = Split-Path $PSScriptRoot -Parent
$assemblySurum = "$Version.0"

Write-Host "Surum guncelleniyor: $Version" -ForegroundColor Cyan

# SatinalmaPro.csproj
$csproj = Join-Path $projeKok "SatinalmaPro.csproj"
$csprojIcerik = Get-Content $csproj -Raw -Encoding UTF8
$csprojIcerik = $csprojIcerik -replace '(<Version>)[^<]+(</Version>)', "`${1}$Version`${2}"
$csprojIcerik = $csprojIcerik -replace '(<AssemblyVersion>)[^<]+(</AssemblyVersion>)', "`${1}$assemblySurum`${2}"
$csprojIcerik = $csprojIcerik -replace '(<FileVersion>)[^<]+(</FileVersion>)', "`${1}$assemblySurum`${2}"
$csprojIcerik = $csprojIcerik -replace '(<InformationalVersion>)[^<]+(</InformationalVersion>)', "`${1}$Version`${2}"
Set-Content $csproj -Value $csprojIcerik -Encoding UTF8 -NoNewline
Write-Host "  - SatinalmaPro.csproj"

# installer/SatinalmaPro.iss
$iss = Join-Path $projeKok "installer\SatinalmaPro.iss"
$issIcerik = Get-Content $iss -Raw -Encoding UTF8
$tfm = [regex]::Match($csprojIcerik, '<TargetFramework>([^<]+)</TargetFramework>').Groups[1].Value
if (-not $tfm) { throw "SatinalmaPro.csproj icinde TargetFramework bulunamadi." }
$publishRel = "..\bin\Release\$tfm\win-x64\publish"
$issIcerik = $issIcerik -replace '(#define MyAppVersion ")[^"]+(")', "`${1}$Version`${2}"
$issIcerik = $issIcerik -replace '(#define MyPublishDir ")[^"]+(")', "`${1}$publishRel`${2}"
Set-Content $iss -Value $issIcerik -Encoding UTF8 -NoNewline
Write-Host "  - installer\SatinalmaPro.iss (publish: $publishRel)"

# version.json
$kurulumAdi = "SatinalmaPro_Kurulum.exe"
$zipAdi = "SatinalmaPro.zip"
$apkAdi = "SatinalmaPro.apk"
$indirmeUrl = "https://github.com/$GitHubKullanici/$RepoAdi/releases/download/v$Version/$kurulumAdi"
$zipUrl = "https://github.com/$GitHubKullanici/$RepoAdi/releases/download/v$Version/$zipAdi"
$apkUrl = "https://github.com/$GitHubKullanici/$RepoAdi/releases/download/v$Version/$apkAdi"
$notMetni = if ($Notes) { $Notes } else { "Surum $Version" }

$repoKok = Split-Path $projeKok -Parent
$mobilCsproj = Join-Path $repoKok "SatinalmaPro.Mobile\SatinalmaPro.Mobile.csproj"
$yeniBuild = 1
if (Test-Path $mobilCsproj) {
    $mobilIcerik = Get-Content $mobilCsproj -Raw -Encoding UTF8
    $mobilIcerik = $mobilIcerik -replace '(<ApplicationDisplayVersion>)[^<]+(</ApplicationDisplayVersion>)', "`${1}$Version`${2}"
    $eskiBuild = [regex]::Match($mobilIcerik, '<ApplicationVersion>(\d+)</ApplicationVersion>').Groups[1].Value
    $yeniBuild = if ($eskiBuild) { [int]$eskiBuild + 1 } else { 1 }
    $mobilIcerik = $mobilIcerik -replace '(<ApplicationVersion>)\d+(</ApplicationVersion>)', "`${1}$yeniBuild`${2}"
    Set-Content $mobilCsproj -Value $mobilIcerik -Encoding UTF8 -NoNewline
    Write-Host "  - SatinalmaPro.Mobile.csproj (build $yeniBuild)"
}

$manifest = [ordered]@{
    version        = $Version
    build          = $yeniBuild
    downloadUrl    = $indirmeUrl
    downloadUrlZip = $zipUrl
    downloadUrlApk = $apkUrl
    notes          = $notMetni
    zorunlu        = $false
}
$manifestYol = Join-Path $projeKok "version.json"
$manifest | ConvertTo-Json | Set-Content $manifestYol -Encoding UTF8
Write-Host "  - version.json (build $yeniBuild)"

# Repo kokunde de manifest (otomatik guncelleme URL'si icin)
$kokManifest = Join-Path $repoKok "version.json"
$manifest | ConvertTo-Json | Set-Content $kokManifest -Encoding UTF8
Write-Host "  - ..\version.json (repo kok)"

# Native Android Compose uygulamasi
$androidCompose = Join-Path $repoKok "SatinalmaPro.Android\app\build.gradle.kts"
if (Test-Path $androidCompose) {
    $g = Get-Content $androidCompose -Raw -Encoding UTF8
    $g = $g -replace '(versionCode\s*=\s*)\d+', "`${1}$yeniBuild"
    $g = $g -replace '(versionName\s*=\s*")[^"]+(")', "`${1}$Version`${2}"
    Set-Content $androidCompose -Value $g -Encoding UTF8 -NoNewline
    Write-Host "  - SatinalmaPro.Android\app\build.gradle.kts (v$yeniBuild / $Version)"
}

# Native Android Compose uygulamalari (5 modul)
$androidModuller = @(
    "SatinalmaPro.SatinAlma",
    "SatinalmaPro.Depo",
    "SatinalmaPro.Saha",
    "SatinalmaPro.Atolye",
    "SatinalmaPro.Admin"
)
foreach ($modul in $androidModuller) {
    $gradle = Join-Path $repoKok "$modul\app\build.gradle.kts"
    if (-not (Test-Path $gradle)) { continue }
    $g = Get-Content $gradle -Raw -Encoding UTF8
    $g = $g -replace '(versionCode\s*=\s*)\d+', "`${1}$yeniBuild"
    $g = $g -replace '(versionName\s*=\s*")[^"]+(")', "`${1}$Version`${2}"
    Set-Content $gradle -Value $g -Encoding UTF8 -NoNewline
    Write-Host "  - $modul\app\build.gradle.kts (v$yeniBuild / $Version)"
}

# SatinalmaPro.Mobile.csproj - yukarida guncellendi

return @{
    Version     = $Version
    DownloadUrl = $indirmeUrl
}
