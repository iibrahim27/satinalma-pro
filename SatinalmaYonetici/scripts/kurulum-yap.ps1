# Satinalma Yonetici — publish + Inno Setup + zip + (opsiyonel) kod imza
# Kullanim: .\scripts\kurulum-yap.ps1 -Version "1.0.44"

param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [string]$GitHubKullanici = "iibrahim27",
    [string]$RepoAdi = "satinalma-pro",
    [string]$Notes = ""
)

$ErrorActionPreference = "Stop"
$projeKok = Split-Path $PSScriptRoot -Parent
$repoKok = Split-Path $projeKok -Parent
Set-Location $projeKok

Write-Host "=== Satinalma Yonetici kurulum paketi ===" -ForegroundColor Cyan
Write-Host "Surum: $Version`n"

& (Join-Path $PSScriptRoot "surum-guncelle.ps1") -Version $Version -GitHubKullanici $GitHubKullanici -RepoAdi $RepoAdi -Notes $Notes

$csproj = Join-Path $projeKok "SatinalmaYonetici.csproj"
$tfm = "net9.0-windows10.0.17763.0"

Write-Host "`n[1/5] Release derleniyor..."
dotnet publish $csproj -c Release -r win-x64 --self-contained true `
    -p:UseAppHost=true `
    -p:Version=$Version `
    -p:AssemblyVersion="$Version.0" `
    -p:FileVersion="$Version.0" `
    -p:InformationalVersion=$Version
if ($LASTEXITCODE -ne 0) { throw "dotnet publish basarisiz" }

$publish = Join-Path $projeKok "bin\Release\$tfm\win-x64\publish"
$publishExe = Join-Path $publish "SatinalmaYonetici.exe"
if (-not (Test-Path $publishExe)) { throw "Publish exe yok: $publishExe" }

$imzaScript = Join-Path $repoKok "scripts\kod-imzala.ps1"
Write-Host "`n[2/5] Publish exe imzalanıyor..."
if (Test-Path $imzaScript) {
    & $imzaScript -Dosyalar @($publishExe)
}

Write-Host "`n[3/5] Zip olusturuluyor..."
$zip = Join-Path $projeKok "SatinalmaYonetici.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path "$publish\*" -DestinationPath $zip -CompressionLevel Optimal

Write-Host "`n[4/5] Inno Setup kurulum exe derleniyor..."
$iscc = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) { throw "Inno Setup 6 bulunamadi" }

& $iscc (Join-Path $projeKok "installer\SatinalmaYonetici.iss")
if ($LASTEXITCODE -ne 0) { throw "Inno Setup basarisiz" }

$kurulumExe = Join-Path $projeKok "SatinalmaYonetici_Kurulum.exe"
if (-not (Test-Path $kurulumExe)) { throw "Kurulum exe yok: $kurulumExe" }

Write-Host "`n[5/5] Kurulum exe imzalanıyor..."
if (Test-Path $imzaScript) {
    & $imzaScript -Dosyalar @($kurulumExe)
}

Write-Host "`n=== YONETICI PAKET TAMAM ===" -ForegroundColor Green
Write-Host "Kurulum: $kurulumExe"
Write-Host "Zip:     $zip"
