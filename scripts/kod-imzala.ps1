# Authenticode kod imzalama (Satinalma Pro / Yonetici)
#
# Ortam degiskenleri (oncelik sirasi):
#   SATINALMA_SIGN_PFX          = .pfx dosya yolu
#   SATINALMA_SIGN_PASSWORD     = .pfx sifresi
#   SATINALMA_SIGN_THUMBPRINT   = Windows sertifika deposundaki thumbprint (PFX yoksa)
#   SATINALMA_SIGN_TIMESTAMP    = zaman damgasi URL (varsayilan DigiCert)
#
# Kullanim:
#   .\scripts\kod-imzala.ps1 -Dosyalar @("...\SatinalmaPro.exe", "...\SatinalmaPro_Kurulum.exe")
#   .\scripts\kod-imzala.ps1 -Dosyalar @("...\a.exe") -Zorunlu
#
# Not: Akilli Uygulama Denetimi icin GUVENILIR CA kod imza sertifikasi gerekir
# (DigiCert / Sectigo / SSL.com OV veya EV). Self-signed SAC'yi acmaz.

param(
    [Parameter(Mandatory = $true)]
    [string[]]$Dosyalar,

    [switch]$Zorunlu
)

$ErrorActionPreference = "Stop"

function Find-SignTool {
    $adaylar = @()
    foreach ($kok in @(
            "${env:ProgramFiles(x86)}\Windows Kits\10\bin",
            "$env:ProgramFiles\Windows Kits\10\bin"
        )) {
        if (Test-Path $kok) {
            $adaylar += Get-ChildItem $kok -Recurse -Filter "signtool.exe" -ErrorAction SilentlyContinue |
                Sort-Object FullName -Descending
        }
    }
    $ilk = $adaylar | Select-Object -First 1
    if ($ilk) { return $ilk.FullName }
    return $null
}

function Get-ImzaSertifikasi {
    $pfx = $env:SATINALMA_SIGN_PFX
    $parola = $env:SATINALMA_SIGN_PASSWORD
    $thumb = $env:SATINALMA_SIGN_THUMBPRINT

    if (-not [string]::IsNullOrWhiteSpace($pfx)) {
        if (-not (Test-Path -LiteralPath $pfx)) {
            throw "SATINALMA_SIGN_PFX bulunamadi: $pfx"
        }
        if ([string]::IsNullOrWhiteSpace($parola)) {
            throw "SATINALMA_SIGN_PASSWORD tanimli degil."
        }
        return [System.Security.Cryptography.X509Certificates.X509Certificate2]::new(
            $pfx,
            $parola,
            [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::Exportable)
    }

    if (-not [string]::IsNullOrWhiteSpace($thumb)) {
        $temiz = ($thumb -replace '\s', '').ToUpperInvariant()
        foreach ($storePath in @("Cert:\CurrentUser\My", "Cert:\LocalMachine\My")) {
            $bulunan = Get-ChildItem $storePath -ErrorAction SilentlyContinue |
                Where-Object { $_.Thumbprint -eq $temiz } |
                Select-Object -First 1
            if ($bulunan) { return $bulunan }
        }
        throw "Thumbprint sertifika deposunda yok: $thumb"
    }

    return $null
}

function Sign-OneFile {
    param(
        [string]$Dosya,
        [System.Security.Cryptography.X509Certificates.X509Certificate2]$Sertifika,
        [string]$SignTool,
        [string]$Timestamp
    )

    if (-not (Test-Path -LiteralPath $Dosya)) {
        throw "Imzalanacak dosya yok: $Dosya"
    }

    Write-Host ("  Imzalaniyor: " + $Dosya) -ForegroundColor Cyan

    if ($SignTool -and -not [string]::IsNullOrWhiteSpace($env:SATINALMA_SIGN_PFX)) {
        & $SignTool sign `
            /fd SHA256 `
            /td SHA256 `
            /tr $Timestamp `
            /f $env:SATINALMA_SIGN_PFX `
            /p $env:SATINALMA_SIGN_PASSWORD `
            /d "Satinalma Pro" `
            $Dosya
        if ($LASTEXITCODE -ne 0) {
            throw ("signtool basarisiz: " + $Dosya + " (cikis " + $LASTEXITCODE + ")")
        }
    }
    else {
        $sonuc = Set-AuthenticodeSignature -FilePath $Dosya -Certificate $Sertifika `
            -TimestampServer $Timestamp -HashAlgorithm SHA256
        if ($sonuc.Status -ne "Valid") {
            throw ("Set-AuthenticodeSignature basarisiz: " + $Dosya + " - " + $sonuc.Status + " " + $sonuc.StatusMessage)
        }
    }

    $dogrula = Get-AuthenticodeSignature -FilePath $Dosya
    $yayinci = if ($dogrula.SignerCertificate) { $dogrula.SignerCertificate.Subject } else { "?" }
    Write-Host ("    Durum: " + $dogrula.Status + " | Yayinci: " + $yayinci) -ForegroundColor Green
}

$timestamp = if ($env:SATINALMA_SIGN_TIMESTAMP) {
    $env:SATINALMA_SIGN_TIMESTAMP
}
else {
    "http://timestamp.digicert.com"
}

$sertifika = $null
try {
    $sertifika = Get-ImzaSertifikasi
}
catch {
    if ($Zorunlu) { throw }
    Write-Host ("UYARI: Sertifika okunamadi: " + $_.Exception.Message) -ForegroundColor Yellow
}

if (-not $sertifika) {
    $mesaj = @"
Kod imzasi ATLANDI - sertifika tanimli degil.

Akilli Uygulama Denetimi icin guvenilir CA kod imza sertifikasi alin (OV/EV),
sonra PowerShell oturumunda:

  `$env:SATINALMA_SIGN_PFX = 'C:\certs\satinalma-codesign.pfx'
  `$env:SATINALMA_SIGN_PASSWORD = '***'

veya:

  `$env:SATINALMA_SIGN_THUMBPRINT = 'ABC123...'

Self-signed (yalnizca yerel test; SAC'yi acmaz):
  .\scripts\imza-sertifika-olustur.ps1
"@
    if ($Zorunlu) { throw $mesaj }
    Write-Host $mesaj -ForegroundColor Yellow
    exit 0
}

$signTool = Find-SignTool
if ($signTool) {
    Write-Host ("signtool: " + $signTool) -ForegroundColor DarkGray
}
else {
    Write-Host "signtool yok - Set-AuthenticodeSignature kullanilacak" -ForegroundColor DarkGray
}

foreach ($dosya in $Dosyalar) {
    Sign-OneFile -Dosya $dosya -Sertifika $sertifika -SignTool $signTool -Timestamp $timestamp
}

Write-Host "Kod imzalama tamam." -ForegroundColor Green
