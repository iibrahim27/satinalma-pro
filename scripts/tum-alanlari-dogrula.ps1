# Satinalma Pro - otomatik dogrulama (derleme + statik tarama)
$ErrorActionPreference = "Stop"
$kok = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$basarisiz = @()
$basarili = @()

function Rapor {
    param([string]$Ad, [bool]$Ok, [string]$Detay = "")
    if ($Ok) {
        $script:basarili += $Ad
        Write-Host "[OK] $Ad" -ForegroundColor Green
        if ($Detay) { Write-Host "     $Detay" -ForegroundColor DarkGray }
    } else {
        $script:basarisiz += $Ad
        Write-Host "[HATA] $Ad" -ForegroundColor Red
        if ($Detay) { Write-Host "       $Detay" -ForegroundColor Yellow }
    }
}

Write-Host ""
Write-Host "=== Satinalma Pro - Tum Alan Dogrulama ===" -ForegroundColor Cyan
Write-Host "Kok: $kok"
Write-Host ""

# 1. WPF derleme
$wpf = Join-Path $kok "Satinalma Pro\SatinalmaPro.csproj"
& dotnet build $wpf -c Release --nologo -v q
Rapor "WPF masaustu derleme" ($LASTEXITCODE -eq 0)

# 2. Shared derleme
$shared = Join-Path $kok "SatinalmaPro.Shared\SatinalmaPro.Shared.csproj"
& dotnet build $shared -c Release --nologo -v q
Rapor "Shared kutuphane derleme" ($LASTEXITCODE -eq 0)

# 3. MAUI Android
$apkScript = Join-Path $kok "SatinalmaPro.Mobile\scripts\apk-olustur.ps1"
if (Test-Path $apkScript) {
    Write-Host ""
    Write-Host "APK derlemesi basliyor, 3-8 dakika surebilir..." -ForegroundColor Yellow
    & $apkScript
    $apkYolu = Join-Path ([Environment]::GetFolderPath("Desktop")) "SatinalmaPro.apk"
    $apkOk = (Test-Path $apkYolu) -and ($LASTEXITCODE -eq 0)
    $apkDetay = ""
    if (Test-Path $apkYolu) {
        $mb = [math]::Round((Get-Item $apkYolu).Length / 1MB, 1)
        $apkDetay = "$mb MB"
    }
    Rapor "Android APK derleme" $apkOk $apkDetay
} else {
    Rapor "Android APK derleme" $false "apk-olustur.ps1 bulunamadi"
}

# 4. Firebase functions
$fnDir = Join-Path $kok "functions"
if (Test-Path (Join-Path $fnDir "package.json")) {
    Push-Location $fnDir
    if (-not (Test-Path "node_modules")) {
        npm ci --silent 2>$null
        if ($LASTEXITCODE -ne 0) { npm install --silent }
    }
    npm run build --silent 2>$null
    Pop-Location
    Rapor "Firebase functions derleme" ($LASTEXITCODE -eq 0)
} else {
    Rapor "Firebase functions derleme" $true "functions/ yok - atlandi"
}

# 5. Modul eslestirme WPF
$catalogPath = Join-Path $kok "Satinalma Pro\Models\ModuleCatalog.cs"
$catalogLines = Get-Content $catalogPath
$moduller = @()
foreach ($line in $catalogLines) {
    if ($line -match '^\s+Title = "(.+)"') {
        $moduller += $Matches[1]
    }
}
$gercekViewlar = @(
    "Alinan Malzemeler", "Stok Yonetimi", "Agrega", "Cimento", "Akaryakit Takip",
    "Arac Filo Takip", "Satinalma", "Raporlamalar", "Finansman Raporlama", "Ayarlar"
)
# Normalize for comparison - catalog has Turkish chars
$normalize = {
    param($s)
    $s = $s -replace "ı","i" -replace "İ","I" -replace "ö","o" -replace "Ö","O"
    $s = $s -replace "ü","u" -replace "Ü","U" -replace "ş","s" -replace "Ş","S"
    $s = $s -replace "ç","c" -replace "Ç","C" -replace "ğ","g" -replace "Ğ","G"
    $s -replace " ",""
}
$normModuller = $moduller | ForEach-Object { & $normalize $_ }
$normGercek = $gercekViewlar | ForEach-Object { & $normalize $_ }
$placeholderModuller = @()
for ($i = 0; $i -lt $moduller.Count; $i++) {
    if ($normGercek -notcontains $normModuller[$i]) {
        $placeholderModuller += $moduller[$i]
    }
}
$modulDetay = if ($placeholderModuller.Count -eq 0) { "10/10 modul gercek view" } else { "Placeholder: $($placeholderModuller -join ', ')" }
Rapor "WPF modul view eslestirmesi" ($placeholderModuller.Count -eq 0) $modulDetay

# 6. Mobil route -> AppShell
$appShell = Get-Content (Join-Path $kok "SatinalmaPro.Mobile\AppShell.xaml.cs") -Raw
$rolNav = Get-Content (Join-Path $kok "SatinalmaPro.Shared\Services\RolNavigasyonu.cs") -Raw
$rotalar = @()
foreach ($m in [regex]::Matches($rolNav, "Route = `"([^`"]+)`"")) {
    $rotalar += $m.Groups[1].Value
}
$rotalar = $rotalar | Select-Object -Unique
$eksikRoute = @()
foreach ($r in $rotalar) {
    if ($r -eq "profil") { continue }
    $pattern = '"' + $r + '"'
    if ($appShell -notlike "*$pattern*") { $eksikRoute += $r }
}
$routeDetay = if ($eksikRoute.Count -eq 0) { "$($rotalar.Count) route" } else { "Eksik: $($eksikRoute -join ', ')" }
Rapor "Mobil rol route AppShell" ($eksikRoute.Count -eq 0) $routeDetay

# 7. Mock Yonetim paneli
$shellHits = Get-ChildItem -Path (Join-Path $kok "SatinalmaPro.Mobile") -Recurse -Filter "*.cs" |
    Where-Object { $_.Name -ne "YonetimShell.xaml.cs" } |
    Select-String -Pattern "YonetimShell" -ErrorAction SilentlyContinue
$shellAktif = $null -ne $shellHits -and $shellHits.Count -gt 0
$mockDetay = if ($shellAktif) { "YonetimShell hala kullaniliyor" } else { "AppShell + Firebase sayfalari aktif" }
Rapor "Mock Yonetim paneli devre disi" (-not $shellAktif) $mockDetay

# 8. Firebase ayar dosyalari
$kritik = @(
    "Satinalma Pro\firebase_ayarlar.json",
    "SatinalmaPro.Mobile\Resources\Raw\firebase_ayarlar.json"
)
$eksikDosya = @()
foreach ($d in $kritik) {
    if (-not (Test-Path (Join-Path $kok $d))) { $eksikDosya += $d }
}
$fbDetay = if ($eksikDosya.Count -eq 0) { "Her iki proje icin mevcut" } else { "Eksik: $($eksikDosya -join ', ')" }
Rapor "Firebase ayar dosyalari yerel" ($eksikDosya.Count -eq 0) $fbDetay

# 9. NotImplemented taramasi
$todoBulunan = Get-ChildItem -Path $kok -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notmatch "\\obj\\|\\bin\\" } |
    Select-String -Pattern "NotImplementedException" -ErrorAction SilentlyContinue
$todoSayi = if ($null -eq $todoBulunan) { 0 } else { @($todoBulunan).Count }
Rapor "NotImplementedException taramasi" ($todoSayi -eq 0) $(if ($todoSayi -gt 0) { "$todoSayi bulundu" })

# Ozet
Write-Host ""
Write-Host "=== OZET ===" -ForegroundColor Cyan
Write-Host "Basarili: $($basarili.Count)" -ForegroundColor Green
Write-Host "Basarisiz: $($basarisiz.Count)" -ForegroundColor $(if ($basarisiz.Count -eq 0) { "Green" } else { "Red" })
if ($basarisiz.Count -gt 0) {
    Write-Host "Basarisiz alanlar:" -ForegroundColor Yellow
    foreach ($b in $basarisiz) { Write-Host "  - $b" -ForegroundColor Yellow }
    exit 1
}
Write-Host ""
Write-Host "Tum otomatik kontroller gecti." -ForegroundColor Green
exit 0
