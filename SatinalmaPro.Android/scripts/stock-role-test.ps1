# Depo + Atölye stock role emulator test
$ErrorActionPreference = 'Continue'
$adb = "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe"
$creds = Get-Content "$env:TEMP\satinalma-stock-test-creds.json" -Raw | ConvertFrom-Json
$outDir = "$env:TEMP\satinalma-stock-role-test"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$report = @()

function Dump-Ui([string]$name) {
  & $adb shell uiautomator dump /sdcard/ui.xml 2>&1 | Out-Null
  cmd /c "`"$adb`" pull /sdcard/ui.xml `"$outDir\$name.xml`" >nul 2>&1"
  cmd /c "`"$adb`" exec-out screencap -p > `"$outDir\$name.png`" 2>nul"
  if (Test-Path "$outDir\$name.xml") { return (Get-Content "$outDir\$name.xml" -Raw -ErrorAction SilentlyContinue) }
  return ''
}
function Texts([string]$xml) {
  return (([regex]::Matches($xml, 'text="([^"]{1,80})"')).Groups | Where-Object { $_.Name -eq '1' } | ForEach-Object { $_.Value } | Select-Object -Unique)
}
function Has([string]$xml, [string]$t) { return $xml -match [regex]::Escape($t) }
function Tap([int]$x, [int]$y) { & $adb shell input tap $x $y | Out-Null }
function Type-Text([string]$t) {
  $esc = $t -replace ' ', '%s' -replace '([\\&|;<>()\$`!"' + "'" + '])', '\$1'
  & $adb shell "input text $esc" | Out-Null
}
function Wait-Ms([int]$ms) { Start-Sleep -Milliseconds $ms }
function Get-BottomTabs([string]$xml) {
  $bottom = @()
  foreach ($n in [regex]::Matches($xml, 'text="([^"]+)"[^>]*bounds="\[(\d+),(\d+)\]\[(\d+),(\d+)\]"')) {
    if ([int]$n.Groups[3].Value -gt 2000) {
      $bottom += [pscustomobject]@{
        text = $n.Groups[1].Value
        x = [int](([int]$n.Groups[2].Value + [int]$n.Groups[4].Value) / 2)
        y = [int](([int]$n.Groups[3].Value + [int]$n.Groups[5].Value) / 2)
      }
    }
  }
  return ($bottom | Sort-Object x)
}

function Login-And-Test($u) {
  $username = $u.username; $password = $u.password; $rol = $u.rol
  Write-Host "=== $rol ($username) ==="
  & $adb shell pm clear com.metrik.satinalmapro | Out-Null
  & $adb logcat -c | Out-Null
  & $adb shell am start -n com.metrik.satinalmapro/com.satinalmapro.android.MainActivity | Out-Null
  Wait-Ms 5000
  $xml = Dump-Ui "login_$rol"
  $edits = [regex]::Matches($xml, 'class="android.widget.EditText"[^>]*bounds="\[(\d+),(\d+)\]\[(\d+),(\d+)\]"|bounds="\[(\d+),(\d+)\]\[(\d+),(\d+)\]"[^>]*class="android.widget.EditText"')
  if ($edits.Count -lt 2) { return [pscustomobject]@{ rol = $rol; ok = $false; error = 'no edits' } }
  $e0 = $edits[0]; $e1 = $edits[1]
  if ($e0.Groups[1].Success) {
    $ux = [int](([int]$e0.Groups[1].Value + [int]$e0.Groups[3].Value) / 2); $uy = [int](([int]$e0.Groups[2].Value + [int]$e0.Groups[4].Value) / 2)
    $px = [int](([int]$e1.Groups[1].Value + [int]$e1.Groups[3].Value) / 2); $py = [int](([int]$e1.Groups[2].Value + [int]$e1.Groups[4].Value) / 2)
  } else {
    $ux = [int](([int]$e0.Groups[5].Value + [int]$e0.Groups[7].Value) / 2); $uy = [int](([int]$e0.Groups[6].Value + [int]$e0.Groups[8].Value) / 2)
    $px = [int](([int]$e1.Groups[5].Value + [int]$e1.Groups[7].Value) / 2); $py = [int](([int]$e1.Groups[6].Value + [int]$e1.Groups[8].Value) / 2)
  }
  Tap $ux $uy; Wait-Ms 400; Type-Text $username; Wait-Ms 600
  Tap $px $py; Wait-Ms 400; Type-Text $password; Wait-Ms 800
  & $adb shell input keyevent 111 | Out-Null; Wait-Ms 400
  $pre = Dump-Ui "prelogin_$rol"
  $btn = $null
  foreach ($n in [regex]::Matches($pre, '<node[^>]+>')) {
    $s = $n.Value
    if ($s -match 'clickable="true"' -and $s -match 'enabled="true"' -and $s -match 'bounds="\[(\d+),(\d+)\]\[(\d+),(\d+)\]"') {
      $y1 = [int]$Matches[2]
      if ($y1 -ge 1400 -and $y1 -le 1700) {
        $btn = @{ x = [int](([int]$Matches[1] + [int]$Matches[3]) / 2); y = [int](([int]$Matches[2] + [int]$Matches[4]) / 2) }
      }
    }
  }
  if ($btn) { Tap $btn.x $btn.y } else { Tap 540 1525 }
  Wait-Ms 12000
  $pid1 = (& $adb shell pidof com.metrik.satinalmapro).Trim()
  Wait-Ms 2500
  $pid2 = (& $adb shell pidof com.metrik.satinalmapro).Trim()
  $homeXml = Dump-Ui "home_$rol"
  $loggedIn = Has $homeXml 'MV'
  $tabs = @(Get-BottomTabs $homeXml | ForEach-Object { $_.text })
  $hasStokTab = ($tabs -join ',') -match 'Stok'
  $hasMalzeme = ($tabs -join ',') -match 'Malzeme'
  $hasProcurementQueue = (Has $homeXml 'Gelen Talepler') -or (Has $homeXml 'Yeni Talep') -or (Has $homeXml 'Teklif')
  $hasStokDurumHome = (Has $homeXml 'Stok') -or (Has $homeXml 'Kritik') -or (Has $homeXml 'Stok durumu')
  $crash = (& $adb logcat -d -t 120 2>$null | Select-String -Pattern 'FATAL EXCEPTION|SatinalmaProCrash|AndroidRuntime: FATAL' | Select-Object -Last 3) -join ' | '

  # Isler hub
  $bottom = Get-BottomTabs $homeXml
  $isler = $bottom | Where-Object { $_.text -match 'ler' -or $_.text -eq 'İşler' } | Select-Object -First 1
  if (-not $isler -and $bottom.Count -ge 2) { $isler = $bottom[1] }
  $queues = @()
  if ($isler) {
    Tap $isler.x $isler.y; Wait-Ms 2000
    $ix = Dump-Ui "isler_$rol"
    & $adb shell input swipe 540 1700 540 700 350 | Out-Null; Wait-Ms 700
    $ix += "`n" + (Dump-Ui "isler2_$rol")
    foreach ($q in @('Stok Durumu','Stok Girişi','Stok Çıkışı','Stok Hareketleri','Gelen Talepler','Yeni Talep','Teklif İstenen','Taleplerim')) {
      if (Has $ix $q) { $queues += $q }
    }
  }

  # Stok tab / durum
  $stokNode = $bottom | Where-Object { $_.text -match 'Stok' } | Select-Object -First 1
  $stokXml = ''
  $stokItems = @()
  if ($stokNode) {
    Tap $stokNode.x $stokNode.y; Wait-Ms 2500
    $stokXml = Dump-Ui "stok_$rol"
  } elseif ($queues -contains 'Stok Durumu') {
    # tap from isler if needed - reopen isler and tap Stok Durumu
    if ($isler) { Tap $isler.x $isler.y; Wait-Ms 1500 }
    $ix2 = Dump-Ui "isler_pre_stok_$rol"
    $node = $null
    foreach ($n in [regex]::Matches($ix2, '<node[^>]+>')) {
      if ($n.Value -match 'Stok Durumu' -and $n.Value -match 'bounds="\[(\d+),(\d+)\]\[(\d+),(\d+)\]"') {
        $node = @{ x = [int](([int]$Matches[1]+[int]$Matches[3])/2); y = [int](([int]$Matches[2]+[int]$Matches[4])/2) }
      }
    }
    if ($node) { Tap $node.x $node.y; Wait-Ms 2500; $stokXml = Dump-Ui "stok_$rol" }
  }
  foreach ($m in @('Çimento','Demir','Boya','Kritik','Tükendi','Stok Durumu','Normal')) {
    if ($stokXml -and (Has $stokXml $m)) { $stokItems += $m }
  }

  # For Depo: open Stok Girişi from isler
  $girisOk = $null; $cikisOk = $null; $hareketOk = $null
  if ($rol -eq 'Depo') {
    if ($isler) { Tap $isler.x $isler.y; Wait-Ms 1500 }
    $ix3 = Dump-Ui "isler_depo_$rol"
    function Tap-Label([string]$xml, [string]$label) {
      foreach ($n in [regex]::Matches($xml, '<node[^>]+>')) {
        if ($n.Value -match [regex]::Escape($label) -and $n.Value -match 'bounds="\[(\d+),(\d+)\]\[(\d+),(\d+)\]"') {
          Tap ([int](([int]$Matches[1]+[int]$Matches[3])/2)) ([int](([int]$Matches[2]+[int]$Matches[4])/2))
          return $true
        }
      }
      return $false
    }
    if (Tap-Label $ix3 'Stok Girişi') {
      Wait-Ms 2000
      $gx = Dump-Ui "giris_$rol"
      $girisOk = (Has $gx 'Stok Girişi') -or (Has $gx 'Giriş kaydet') -or (Has $gx 'Malzeme')
    }
    if ($isler) { Tap $isler.x $isler.y; Wait-Ms 1500 }
    $ix4 = Dump-Ui "isler_depo2_$rol"
    if (Tap-Label $ix4 'Stok Çıkışı') {
      Wait-Ms 2000
      $cx = Dump-Ui "cikis_$rol"
      $cikisOk = (Has $cx 'Stok Çıkışı') -or (Has $cx 'Çıkış kaydet') -or (Has $cx 'Teslim alan')
    }
    if ($isler) { Tap $isler.x $isler.y; Wait-Ms 1500 }
    $ix5 = Dump-Ui "isler_depo3_$rol"
    if (Tap-Label $ix5 'Stok Hareketleri') {
      Wait-Ms 2000
      $hx = Dump-Ui "hareket_$rol"
      $hareketOk = (Has $hx 'Stok Hareketleri') -or (Has $hx 'Giriş') -or (Has $hx 'Çıkış') -or (Has $hx 'Çimento')
    }
  }

  $expectedQueues = if ($rol -eq 'Atölye') { @('Stok Durumu') } else { @('Stok Durumu','Stok Girişi','Stok Çıkışı','Stok Hareketleri') }
  $missing = @($expectedQueues | Where-Object { $queues -notcontains $_ })
  $unexpectedProc = @($queues | Where-Object { $_ -in @('Gelen Talepler','Yeni Talep','Teklif İstenen','Taleplerim') })

  return [pscustomobject]@{
    rol = $rol
    username = $username
    loggedIn = $loggedIn
    alive = ($pid2 -ne '')
    noCrashKick = ($pid1 -ne '' -and $pid1 -eq $pid2)
    tabs = ($tabs -join ',')
    hasStokTab = [bool]$hasStokTab
    hasMalzemeTab = [bool]$hasMalzeme
    hasProcurementOnHome = [bool]$hasProcurementQueue
    queues = ($queues -join ' | ')
    missingQueues = ($missing -join ',')
    unexpectedProcurement = ($unexpectedProc -join ',')
    stokScreenItems = ($stokItems -join ',')
    stokDurumOk = ($stokItems.Count -gt 0)
    girisOk = $girisOk
    cikisOk = $cikisOk
    hareketOk = $hareketOk
    crash = $crash
    pass = [bool]($loggedIn -and ($pid1 -eq $pid2) -and $hasStokTab -and (-not $hasMalzeme) -and ($missing.Count -eq 0) -and ($unexpectedProc.Count -eq 0) -and ($stokItems.Count -gt 0) -and ($rol -ne 'Depo' -or ($girisOk -and $cikisOk -and $hareketOk)))
  }
}

foreach ($u in $creds.users) {
  $row = Login-And-Test $u
  $report += $row
  $row | Format-List | Out-String | Write-Host
}
$report | ConvertTo-Json -Depth 5 | Set-Content "$outDir\report.json" -Encoding UTF8
$report | Format-Table rol,pass,loggedIn,hasStokTab,hasMalzemeTab,stokDurumOk,girisOk,cikisOk,hareketOk,missingQueues,unexpectedProcurement -AutoSize | Out-String | Write-Host
Write-Host "REPORT $outDir\report.json"
