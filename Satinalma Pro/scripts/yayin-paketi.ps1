# Satinalma Pro — yayin paketi (kurulum-yap.ps1 sarmalayicisi)
# Kullanim:  .\scripts\yayin-paketi.ps1 -Version "1.2.0" -GitHubKullanici "iibrahim27"

param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [Parameter(Mandatory = $true)]
    [string]$GitHubKullanici,

    [string]$RepoAdi = "satinalma-pro",
    [string]$Notes = ""
)

& (Join-Path $PSScriptRoot "kurulum-yap.ps1") `
    -Version $Version `
    -GitHubKullanici $GitHubKullanici `
    -RepoAdi $RepoAdi `
    -Notes $Notes
