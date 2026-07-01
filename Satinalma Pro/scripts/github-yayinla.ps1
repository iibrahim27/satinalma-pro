# GitHub surum yayinlama: version.json push + Release vX.Y.Z + dosya yukleme
# Kullanim:
#   .\scripts\github-yayinla.ps1 -Version "1.7.0"
#   $env:GITHUB_TOKEN = "ghp_..."   (repo yetkili token)
#
# Token: GitHub -> Settings -> Developer settings -> Personal access tokens
# Gerekli izinler: repo (veya public_repo)

param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [string]$GitHubKullanici = "iibrahim27",
    [string]$RepoAdi = "satinalma-pro",
    [string]$Notes = "",
    [string]$CloneKlasor = "C:\SatinalmaGithub",
    [switch]$PaketOlustur
)

$ErrorActionPreference = "Stop"
$git = "C:\Program Files\Git\bin\git.exe"
if (-not (Test-Path $git)) { throw "Git bulunamadi: $git" }

$projeKok = Split-Path $PSScriptRoot -Parent
$tag = "v$Version"
$repoUrl = "https://github.com/$GitHubKullanici/$RepoAdi.git"
$apiBase = "https://api.github.com/repos/$GitHubKullanici/$RepoAdi"

if ($PaketOlustur) {
    & (Join-Path $PSScriptRoot "yayin-paketi.ps1") -Version $Version -GitHubKullanici $GitHubKullanici -Notes $Notes
}

$exe = Join-Path $projeKok "SatinalmaPro_Kurulum.exe"
$zip = Join-Path $projeKok "SatinalmaPro.zip"
$apk = Join-Path $projeKok "SatinalmaPro.apk"
$manifest = Join-Path $projeKok "version.json"

foreach ($f in @($exe, $zip, $manifest)) {
    if (-not (Test-Path $f)) { throw "Dosya eksik: $f`nOnce: .\scripts\yayin-paketi.ps1 -Version `"$Version`"" }
}

# APK masaustunde olabilir
if (-not (Test-Path $apk)) {
    $apkMasaustu = Join-Path ([Environment]::GetFolderPath("Desktop")) "SatinalmaPro.apk"
    if (Test-Path $apkMasaustu) { Copy-Item $apkMasaustu $apk -Force }
}
if (-not (Test-Path $apk)) {
    Write-Host "UYARI: SatinalmaPro.apk bulunamadi, release'e APK eklenmeyecek." -ForegroundColor Yellow
}

# --- version.json repoya ---
if (Test-Path $CloneKlasor) {
    try { & $git -C $CloneKlasor pull origin main 2>&1 | Out-Null } catch { }
} else {
    & $git clone $repoUrl $CloneKlasor
}

Copy-Item $manifest (Join-Path $CloneKlasor "version.json") -Force

$commitMsg = "Release $tag manifest"
& $git -C $CloneKlasor add version.json
$status = & $git -C $CloneKlasor status --porcelain
if ($status) {
    & $git -C $CloneKlasor -c user.name="$GitHubKullanici" -c user.email="$GitHubKullanici@users.noreply.github.com" `
        commit -m $commitMsg
    & $git -C $CloneKlasor push origin main
    Write-Host "version.json GitHub'a gonderildi." -ForegroundColor Green
} else {
    Write-Host "version.json zaten guncel." -ForegroundColor Yellow
}

# --- Release API ---
$token = $env:GITHUB_TOKEN
if (-not $token) {
    Write-Host ""
    Write-Host "GITHUB_TOKEN tanimli degil. Release otomatik olusturulamadi." -ForegroundColor Yellow
    Write-Host "Manuel: https://github.com/$GitHubKullanici/$RepoAdi/releases/new" -ForegroundColor Cyan
    Write-Host "  Tag: $tag" -ForegroundColor Cyan
    Write-Host "  Dosyalar: SatinalmaPro_Kurulum.exe, SatinalmaPro.zip, SatinalmaPro.apk" -ForegroundColor Cyan
    exit 0
}

$headers = @{
    Authorization = "Bearer $token"
    Accept        = "application/vnd.github+json"
    "X-GitHub-Api-Version" = "2022-11-28"
}

# --- Eski release'leri sil (yalnizca guncel surum kalsin) ---
Write-Host "Eski GitHub release'leri temizleniyor..."
try {
    $sayfa = 1
    while ($true) {
        $liste = Invoke-RestMethod -Uri "$apiBase/releases?per_page=100&page=$sayfa" -Headers $headers
        if (-not $liste -or $liste.Count -eq 0) { break }
        foreach ($r in $liste) {
            if ($r.tag_name -eq $tag) { continue }
            Write-Host "  Siliniyor: $($r.tag_name)" -ForegroundColor DarkYellow
            Invoke-RestMethod -Uri $r.url -Method Delete -Headers $headers | Out-Null
        }
        if ($liste.Count -lt 100) { break }
        $sayfa++
    }
} catch {
    Write-Host "UYARI: Eski release temizligi atlandi: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Ayni tag varsa once sil (dosyalarin karismamasi icin)
try {
    $mevcutRelease = Invoke-RestMethod -Uri "$apiBase/releases/tags/$tag" -Headers $headers
    Write-Host "Mevcut $tag release yeniden yuklenecek, once siliniyor..." -ForegroundColor Yellow
    Invoke-RestMethod -Uri $mevcutRelease.url -Method Delete -Headers $headers | Out-Null
} catch {
    # release yok — sorun degil
}

$bodyMetni = if ($Notes) { $Notes } else { (Get-Content $manifest -Raw | ConvertFrom-Json).notes }
$releaseBody = @{
    tag_name   = $tag
    name       = "Satinalma Pro $tag"
    body       = $bodyMetni
    draft      = $false
    prerelease = $false
} | ConvertTo-Json

Write-Host "Release olusturuluyor: $tag ..."
$release = Invoke-RestMethod -Uri "$apiBase/releases" -Method Post -Headers $headers -Body $releaseBody -ContentType "application/json; charset=utf-8"

function Upload-Asset($dosya, $etiket) {
    if (-not (Test-Path $dosya)) { return }
    $ad = Split-Path $dosya -Leaf
    $uploadUrl = $release.upload_url -replace '\{.*$', "?name=$ad"
    Write-Host "  Yukleniyor: $ad ($etiket)..."
    Invoke-RestMethod -Uri $uploadUrl -Method Post -Headers @{
        Authorization = "Bearer $token"
        Accept        = "application/vnd.github+json"
        "X-GitHub-Api-Version" = "2022-11-28"
        "Content-Type" = "application/octet-stream"
    } -InFile $dosya
}

Upload-Asset $exe "kurulum"
Upload-Asset $zip "zip"
if (Test-Path $apk) { Upload-Asset $apk "apk" }

Write-Host ""
Write-Host "=== GitHub v$Version YAYINLANDI ===" -ForegroundColor Green
Write-Host "Release: https://github.com/$GitHubKullanici/$RepoAdi/releases/tag/$tag"
Write-Host "Manifest: https://raw.githubusercontent.com/$GitHubKullanici/$RepoAdi/main/version.json"
