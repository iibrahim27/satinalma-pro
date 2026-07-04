# METRİK Firebase Tam Deploy
# Kullanım: .\deploy-firebase.ps1
# Not: PowerShell betik engeli varsa firebase yerine firebase.cmd kullanılır.

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $Root

$Firebase = if (Get-Command firebase.cmd -ErrorAction SilentlyContinue) { "firebase.cmd" } else { "firebase" }

$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")

Write-Host "=== METRİK Firebase Deploy ===" -ForegroundColor Cyan
Write-Host "Proje: satinalmapro-8e7da"

if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    Write-Host "Node.js bulunamadi. winget install OpenJS.NodeJS.LTS" -ForegroundColor Red
    exit 1
}

try {
    & $Firebase projects:list --project satinalmapro-8e7da 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "not logged in" }
    Write-Host "Firebase oturumu: OK" -ForegroundColor Green
} catch {
    Write-Host "Firebase girisi gerekli..." -ForegroundColor Yellow
    & $Firebase login
}

Write-Host "`nFunctions derleniyor..." -ForegroundColor Cyan
Push-Location functions
npm install
npm run build
Pop-Location

Write-Host "`nDeploy (rules + indexes + storage + functions)..." -ForegroundColor Cyan
& $Firebase deploy --only firestore:rules,firestore:indexes,storage,functions --project satinalmapro-8e7da --force

Write-Host "`nBildirim sablonlari seed ediliyor..." -ForegroundColor Cyan
$env:GOOGLE_APPLICATION_CREDENTIALS = Join-Path $Root "Satinalma Pro\fcm-service-account.json"
Push-Location functions
npm run seed:templates
Pop-Location

Write-Host "`nLegacy talep migrasyonu..." -ForegroundColor Cyan
Push-Location functions
npm run migrate:legacy
Pop-Location

Write-Host "`n=== Deploy tamamlandi ===" -ForegroundColor Green
