# Android JVM unit testleri — METRİK gibi Unicode yollarda Gradle test worker bozulmasın diye
# ASCII kopyada çalıştırır: PurchaseModuleAutomationTest (3 senaryo)
$ErrorActionPreference = "Stop"

$kok = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$androidKaynak = Join-Path $kok "SatinalmaPro.Android"
$androidHedef = "C:\temp\metrik-android-test"
$raporHtml = Join-Path $androidHedef "app\build\reports\tests\testDebugUnitTest\index.html"
$sonucKlasoru = Join-Path $androidHedef "app\build\test-results\testDebugUnitTest"

if (-not (Test-Path $androidKaynak)) {
    Write-Host "Hata: SatinalmaPro.Android bulunamadi: $androidKaynak" -ForegroundColor Red
    exit 1
}

# JAVA_HOME — Android Studio JBR
if (-not $env:JAVA_HOME) {
    $jbr = "${env:ProgramFiles}\Android\Android Studio\jbr"
    if (Test-Path $jbr) {
        $env:JAVA_HOME = $jbr
    } else {
        Write-Host "Hata: JAVA_HOME yok ve Android Studio JBR bulunamadi." -ForegroundColor Red
        Write-Host "Android Studio kuruluysa: `$env:JAVA_HOME = 'C:\Program Files\Android\Android Studio\jbr'" -ForegroundColor Yellow
        exit 1
    }
}

Write-Host ""
Write-Host "Android test ortami hazirlaniyor..." -ForegroundColor Cyan
Write-Host "  Kaynak: $androidKaynak"
Write-Host "  Hedef:  $androidHedef (ASCII yol)"
Write-Host ""

New-Item -ItemType Directory -Path (Split-Path $androidHedef) -Force | Out-Null
robocopy $androidKaynak $androidHedef /MIR /XD build .gradle .idea /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
if ($LASTEXITCODE -ge 8) {
    Write-Host "Hata: Proje kopyalanamadi (robocopy $LASTEXITCODE)." -ForegroundColor Red
    exit 1
}

Push-Location $androidHedef
try {
    Write-Host "Gradle unit testleri calistiriliyor..." -ForegroundColor Cyan
    & .\gradlew.bat :app:testDebugUnitTest --no-daemon
    $gradleExit = $LASTEXITCODE
} finally {
    Pop-Location
}

Write-Host ""
if (Test-Path $sonucKlasoru) {
    $xmlDosyalari = Get-ChildItem $sonucKlasoru -Filter "TEST-*.xml" -ErrorAction SilentlyContinue
    $toplamTest = 0
    $toplamHata = 0
    foreach ($xml in $xmlDosyalari) {
        [xml]$doc = Get-Content $xml.FullName -Encoding UTF8
        $suite = $doc.testsuite
        if ($suite) {
            $toplamTest += [int]$suite.tests
            $toplamHata += [int]$suite.failures + [int]$suite.errors
            $ad = $suite.name -replace 'com\.satinalmapro\.test\.automation\.', ''
            $durum = if ([int]$suite.failures + [int]$suite.errors -eq 0) { "GECTI" } else { "HATALI" }
            Write-Host "  $ad : $($suite.tests) test, $durum" -ForegroundColor $(if ($durum -eq "GECTI") { "Green" } else { "Red" })
        }
    }
    Write-Host ""
    Write-Host "Ozet: $toplamTest test, $toplamHata hata" -ForegroundColor $(if ($toplamHata -eq 0 -and $gradleExit -eq 0) { "Green" } else { "Red" })
}

if (Test-Path $raporHtml) {
    Write-Host "HTML rapor: $raporHtml" -ForegroundColor DarkGray
}

if ($gradleExit -ne 0) {
    Write-Host "Android testleri BASARISIZ (exit $gradleExit)." -ForegroundColor Red
    exit $gradleExit
}

Write-Host "Android testleri BASARILI." -ForegroundColor Green
exit 0
