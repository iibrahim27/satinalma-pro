# Yerel test icin self-signed kod imza sertifikasi olusturur.
# UYARI: Self-signed Windows Akilli Uygulama Denetimi'ni (SAC) ACMAZ.
# SAC icin DigiCert / Sectigo / SSL.com OV veya EV Code Signing sertifikasi gerekir.
#
# Kullanim:
#   .\scripts\imza-sertifika-olustur.ps1
#   .\scripts\imza-sertifika-olustur.ps1 -Parola "GucluSifre!" -Yayinci "CN=MV INSAAT"

param(
    [string]$Parola = "SatinalmaDevSign!",
    [string]$Yayinci = "CN=MV INSAAT, O=MV INSAAT, C=TR",
    [string]$CiktiKlasor = ""
)

$ErrorActionPreference = "Stop"

$repoKok = Split-Path $PSScriptRoot -Parent
if (-not $CiktiKlasor) {
    $CiktiKlasor = Join-Path $repoKok "certs"
}
New-Item -ItemType Directory -Force -Path $CiktiKlasor | Out-Null

$pfxYol = Join-Path $CiktiKlasor "satinalma-codesign-dev.pfx"
$cerYol = Join-Path $CiktiKlasor "satinalma-codesign-dev.cer"

Write-Host "Self-signed kod imza sertifikasi olusturuluyor..." -ForegroundColor Cyan
Write-Host "  Yayinci: $Yayinci"

$cert = New-SelfSignedCertificate `
    -Type CodeSigningCert `
    -Subject $Yayinci `
    -KeyExportPolicy Exportable `
    -KeySpec Signature `
    -KeyLength 2048 `
    -HashAlgorithm SHA256 `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -NotAfter (Get-Date).AddYears(3)

$secure = ConvertTo-SecureString -String $Parola -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath $pfxYol -Password $secure | Out-Null
Export-Certificate -Cert $cert -FilePath $cerYol | Out-Null

Write-Host ""
Write-Host "Olusturuldu:" -ForegroundColor Green
Write-Host "  PFX: $pfxYol"
Write-Host "  CER: $cerYol"
Write-Host "  Thumbprint: $($cert.Thumbprint)"
Write-Host "  Parola: $Parola"
Write-Host ""
Write-Host "Bu oturumda kullanmak icin:" -ForegroundColor Yellow
Write-Host "  `$env:SATINALMA_SIGN_PFX = '$pfxYol'"
Write-Host "  `$env:SATINALMA_SIGN_PASSWORD = '$Parola'"
Write-Host ""
Write-Host "SAC uyarisi devam ederse guvenilir CA kod imza sertifikasi satin alin." -ForegroundColor DarkYellow
