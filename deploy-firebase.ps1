# METRİK Firebase Tam Deploy
# Kullanım: .\deploy-firebase.ps1
# Kimlik: fcm-service-account.json (etkileşimsiz) veya firebase login (etkileşimli)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $Root

if ($PSVersionTable.PSVersion.Major -lt 6) {
    try {
        chcp 65001 | Out-Null
        [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
        [Console]::InputEncoding = [System.Text.Encoding]::UTF8
        $OutputEncoding = [System.Text.Encoding]::UTF8
    } catch { }
}

$Firebase = if (Get-Command firebase.cmd -ErrorAction SilentlyContinue) { "firebase.cmd" } else { "firebase" }
$ProjectId = "satinalmapro-8e7da"
$ServiceAccountPath = Join-Path $Root "Satinalma Pro\fcm-service-account.json"

$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")

Write-Host "=== METRİK Firebase Deploy ===" -ForegroundColor Cyan
Write-Host "Proje: $ProjectId"

if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    Write-Host "Node.js bulunamadi. winget install OpenJS.NodeJS.LTS" -ForegroundColor Red
    exit 1
}

$nodeVersion = (node -p "process.versions.node")
if ($nodeVersion -notmatch "^24\.") {
    Write-Host "Node.js $nodeVersion bulundu; functions Node 24 gerektirir." -ForegroundColor Yellow
    Write-Host "Onerilen: winget install OpenJS.NodeJS.LTS veya nvm ile Node 24 kullanin." -ForegroundColor Yellow
}

function Test-IsInteractive {
    if ($env:CI -eq "true") { return $false }
    if ([Console]::IsInputRedirected -or [Console]::IsOutputRedirected) { return $false }
    return $true
}

function Invoke-Firebase {
    param(
        [Parameter(Mandatory)][string[]]$Arguments,
        [switch]$Quiet
    )
    $prev = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        if ($Quiet) {
            & $Firebase @Arguments *>&1 | Out-Null
        } else {
            & $Firebase @Arguments *>&1 | ForEach-Object {
                if ($_ -is [System.Management.Automation.ErrorRecord]) {
                    Write-Host $_.ToString()
                } else {
                    Write-Host $_
                }
            }
        }
        return [int]$LASTEXITCODE
    } finally {
        $ErrorActionPreference = $prev
    }
}

function Invoke-Npm {
    param([Parameter(Mandatory)][string[]]$Arguments)
    $prev = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        & npm @Arguments *>&1 | ForEach-Object {
            if ($_ -is [System.Management.Automation.ErrorRecord]) {
                Write-Host $_.ToString()
            } else {
                Write-Host $_
            }
        }
        if ($LASTEXITCODE -ne 0) {
            throw "npm $($Arguments -join ' ') basarisiz (exit $LASTEXITCODE)"
        }
    } finally {
        $ErrorActionPreference = $prev
    }
}

function Test-FirebaseAccess {
    return (Invoke-Firebase -Arguments @("projects:list", "--project", $ProjectId) -Quiet) -eq 0
}

function Ensure-FirebaseAuth {
    if (Test-FirebaseAccess) {
        Write-Host "Firebase oturumu: OK" -ForegroundColor Green
        return
    }

    if (Test-Path $ServiceAccountPath) {
        $env:GOOGLE_APPLICATION_CREDENTIALS = $ServiceAccountPath
        if (Test-FirebaseAccess) {
            Write-Host "Firebase kimlik dogrulama: service account" -ForegroundColor Green
            return
        }
        Write-Host "Service account dosyasi var ama Firebase erisimi reddedildi." -ForegroundColor Red
        Write-Host "IAM: service account'a Firebase Admin veya Editor rolu gerekli." -ForegroundColor Yellow
        exit 1
    }

    if ($env:FIREBASE_TOKEN) {
        if (Test-FirebaseAccess) {
            Write-Host "Firebase kimlik dogrulama: CI token" -ForegroundColor Green
            return
        }
        Write-Host "FIREBASE_TOKEN gecersiz." -ForegroundColor Red
        exit 1
    }

    if (Test-IsInteractive) {
        Write-Host "Firebase girisi gerekli (tarayici acilacak)..." -ForegroundColor Yellow
        if ((Invoke-Firebase -Arguments @("login")) -ne 0) {
            Write-Host "firebase login basarisiz." -ForegroundColor Red
            exit 1
        }
        if (Test-FirebaseAccess) {
            Write-Host "Firebase oturumu: OK" -ForegroundColor Green
            return
        }
    }

    Write-Host "Firebase kimlik dogrulama basarisiz." -ForegroundColor Red
    Write-Host "Etkilesimsiz ortam icin: Satinalma Pro\fcm-service-account.json dosyasini yerlestirin." -ForegroundColor Yellow
    Write-Host "Alternatif: firebase login:ci ile token alip FIREBASE_TOKEN ortam degiskenini ayarlayin." -ForegroundColor Yellow
    exit 1
}

Ensure-FirebaseAuth

if (Test-Path $ServiceAccountPath) {
    $env:GOOGLE_APPLICATION_CREDENTIALS = $ServiceAccountPath
}

Write-Host "`nFunctions derleniyor..." -ForegroundColor Cyan
Push-Location functions
Invoke-Npm @("install")
Invoke-Npm @("run", "build")
Pop-Location

Write-Host "`nDeploy (rules + indexes + storage + functions)..." -ForegroundColor Cyan
$deployExit = Invoke-Firebase -Arguments @("deploy", "--only", "firestore:rules,firestore:indexes,storage,functions", "--project", $ProjectId, "--force")
if ($deployExit -ne 0) {
    Write-Host "Firebase deploy basarisiz (exit $deployExit)." -ForegroundColor Red
    exit 1
}

Write-Host "`nBildirim sablonlari seed ediliyor..." -ForegroundColor Cyan
Push-Location functions
Invoke-Npm @("run", "seed:templates")
Pop-Location

Write-Host "`nLegacy talep migrasyonu..." -ForegroundColor Cyan
Push-Location functions
Invoke-Npm @("run", "migrate:legacy")
Pop-Location

Write-Host "`n=== Deploy tamamlandi ===" -ForegroundColor Green
