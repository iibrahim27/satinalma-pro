# Satinalma Yonetici surum dosyalarini gunceller
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [string]$GitHubKullanici = "iibrahim27",
    [string]$RepoAdi = "satinalma-pro",
    [string]$Notes = ""
)

$ErrorActionPreference = "Stop"
if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    throw "Surum formati gecersiz: $Version (ornek: 1.0.1)"
}

$projeKok = Split-Path $PSScriptRoot -Parent
$repoKok = Split-Path $projeKok -Parent
$assemblySurum = "$Version.0"

Write-Host "Yonetici surum: $Version" -ForegroundColor Cyan

$csproj = Join-Path $projeKok "SatinalmaYonetici.csproj"
$c = Get-Content $csproj -Raw -Encoding UTF8
if ($c -notmatch '<Version>') {
    $c = $c -replace '</PropertyGroup>', @"
    <Version>$Version</Version>
    <AssemblyVersion>$assemblySurum</AssemblyVersion>
    <FileVersion>$assemblySurum</FileVersion>
    <InformationalVersion>$Version</InformationalVersion>
  </PropertyGroup>
"@
} else {
    $c = $c -replace '(<Version>)[^<]+(</Version>)', "`${1}$Version`${2}"
    $c = $c -replace '(<AssemblyVersion>)[^<]+(</AssemblyVersion>)', "`${1}$assemblySurum`${2}"
    $c = $c -replace '(<FileVersion>)[^<]+(</FileVersion>)', "`${1}$assemblySurum`${2}"
    $c = $c -replace '(<InformationalVersion>)[^<]+(</InformationalVersion>)', "`${1}$Version`${2}"
}
Set-Content $csproj -Value $c -Encoding UTF8 -NoNewline
Write-Host "  - SatinalmaYonetici.csproj"

$iss = Join-Path $projeKok "installer\SatinalmaYonetici.iss"
if (Test-Path $iss) {
    $i = Get-Content $iss -Raw -Encoding UTF8
    $i = $i -replace '(#define MyAppVersion ")[^"]+(")', "`${1}$Version`${2}"
    Set-Content $iss -Value $i -Encoding UTF8 -NoNewline
    Write-Host "  - installer\SatinalmaYonetici.iss"
}

$tag = "yonetici-v$Version"
$kurulumUrl = "https://github.com/$GitHubKullanici/$RepoAdi/releases/download/$tag/SatinalmaYonetici_Kurulum.exe"
$zipUrl = "https://github.com/$GitHubKullanici/$RepoAdi/releases/download/$tag/SatinalmaYonetici.zip"
$apkUrl = "https://github.com/$GitHubKullanici/$RepoAdi/releases/download/$tag/SatinalmaYonetici.apk"
$notMetni = if ($Notes) { $Notes } else { "Satinalma Yonetici $Version" }

$yeniBuild = 1
$eskiManifest = Join-Path $repoKok "version-yonetici.json"
if (Test-Path $eskiManifest) {
    try {
        $eski = Get-Content $eskiManifest -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($eski.build) { $yeniBuild = [int]$eski.build + 1 }
    } catch { }
}

$manifest = [ordered]@{
    version           = $Version
    build             = $yeniBuild
    downloadUrl       = $kurulumUrl
    downloadUrlZip    = $zipUrl
    downloadUrlApk    = $apkUrl
    notes             = $notMetni
    zorunlu           = $false
}

$manifestYol = Join-Path $repoKok "version-yonetici.json"
$manifest | ConvertTo-Json | Set-Content $manifestYol -Encoding UTF8
Write-Host "  - version-yonetici.json"

Write-Host ""
Write-Host "Manifest URL: https://raw.githubusercontent.com/$GitHubKullanici/$RepoAdi/main/version-yonetici.json"
Write-Host "Release tag : $tag"
