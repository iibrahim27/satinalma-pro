# Role matrix emulator test for Satinalma Pro
# Usage: powershell -File role-matrix-test.ps1
$ErrorActionPreference = 'Continue'
$adb = "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe"
$creds = Get-Content "$env:TEMP\satinalma-role-test-creds.json" -Raw | ConvertFrom-Json
$outDir = "$env:TEMP\satinalma-role-matrix"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$report = @()

function Dump-Ui([string]$name) {
  & $adb shell uiautomator dump /sdcard/ui.xml 2>&1 | Out-Null
  cmd /c "`"$adb`" pull /sdcard/ui.xml `"$outDir\$name.xml`" >nul 2>&1"
  cmd /c "`"$adb`" exec-out screencap -p > `"$outDir\$name.png`" 2>nul"
  if (Test-Path "$outDir\$name.xml") {
    return (Get-Content "$outDir\$name.xml" -Raw -ErrorAction SilentlyContinue)
  }
  return ''
}

function Get-Bounds([string]$xml, [string]$contains) {
  return Get-ClickableBounds $xml $contains
}

function Get-ClickableBounds([string]$xml, [string]$contains) {
  $nodes = [regex]::Matches($xml, '<node[^>]+>')
  $fallback = $null
  foreach ($n in $nodes) {
    $s = $n.Value
    if ($s -notmatch [regex]::Escape($contains)) { continue }
    if ($s -notmatch 'bounds="\[(\d+),(\d+)\]\[(\d+),(\d+)\]"') { continue }
    $box = @{ x = [int](([int]$Matches[1] + [int]$Matches[3]) / 2); y = [int](([int]$Matches[2] + [int]$Matches[4]) / 2); x1 = [int]$Matches[1]; y1 = [int]$Matches[2]; x2 = [int]$Matches[3]; y2 = [int]$Matches[4] }
    $fallback = $box
    if ($s -match 'clickable="true"') { return $box }
  }
  return $fallback
}

function Get-BottomTabs([string]$xml) {
  $tabs = @()
  $nodes = [regex]::Matches($xml, 'text="([^"]+)"[^>]*bounds="\[(\d+),(\d+)\]\[(\d+),(\d+)\]"')
  $bottom = @()
  foreach ($n in $nodes) {
    $y1 = [int]$n.Groups[3].Value
    if ($y1 -gt 2000) {
      $bottom += [pscustomobject]@{
        text = $n.Groups[1].Value
        x = [int](([int]$n.Groups[2].Value + [int]$n.Groups[4].Value) / 2)
        y = [int](([int]$n.Groups[3].Value + [int]$n.Groups[5].Value) / 2)
      }
    }
  }
  $bottom = $bottom | Sort-Object x
  foreach ($b in $bottom) {
    if ($tabs -notcontains $b.text) { $tabs += $b.text }
  }
  return @{ labels = $tabs; nodes = $bottom }
}

function Tap([int]$x, [int]$y) { & $adb shell input tap $x $y | Out-Null }
function Clear-FocusedField {
  # Batch deletes in one shell to avoid slow per-keyevent roundtrips.
  & $adb shell "input keyevent 123; for i in 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17 18 19 20 21 22 23 24 25 26 27 28 29 30 31 32 33 34 35 36 37 38 39 40; do input keyevent 67; done" | Out-Null
}
function Type-Text([string]$t) {
  $esc = $t -replace ' ', '%s' -replace '([\\&|;<>()\$`!"' + "'" + '])', '\$1'
  & $adb shell "input text $esc" | Out-Null
}
function Wait-Ms([int]$ms) { Start-Sleep -Milliseconds $ms }

function Login-Role([string]$username, [string]$password, [string]$rol) {
  Write-Host "=== LOGIN $rol ($username) ==="
  & $adb shell pm clear com.metrik.satinalmapro | Out-Null
  & $adb logcat -c | Out-Null
  & $adb shell am start -n com.metrik.satinalmapro/com.satinalmapro.android.MainActivity | Out-Null
  Wait-Ms 5000
  $xml = Dump-Ui "login_$($rol)"
  $loginBtn = Get-ClickableBounds $xml 'Giriş'
  if (-not $loginBtn) { $loginBtn = Get-ClickableBounds $xml 'Giri' }
  # Prefer lower on-screen button (header also says Giriş Yap)
  if ($loginBtn -and $loginBtn.y1 -lt 900) {
    $all = [regex]::Matches($xml, '<node[^>]+>')
    foreach ($n in $all) {
      $s = $n.Value
      if ($s -match 'Giri' -and $s -match 'clickable="true"' -and $s -match 'bounds="\[(\d+),(\d+)\]\[(\d+),(\d+)\]"') {
        $y1 = [int]$Matches[2]
        if ($y1 -gt 900) {
          $loginBtn = @{ x = [int](([int]$Matches[1] + [int]$Matches[3]) / 2); y = [int](([int]$Matches[2] + [int]$Matches[4]) / 2); y1 = $y1 }
        }
      }
    }
  }
  $edits = [regex]::Matches($xml, 'class="android.widget.EditText"[^>]*bounds="\[(\d+),(\d+)\]\[(\d+),(\d+)\]"|bounds="\[(\d+),(\d+)\]\[(\d+),(\d+)\]"[^>]*class="android.widget.EditText"')
  $userBox = $null; $passBox = $null
  if ($edits.Count -ge 2) {
    $e0 = $edits[0]; $e1 = $edits[1]
    if ($e0.Groups[1].Success) {
      $userBox = @{ x = [int](([int]$e0.Groups[1].Value + [int]$e0.Groups[3].Value) / 2); y = [int](([int]$e0.Groups[2].Value + [int]$e0.Groups[4].Value) / 2) }
      $passBox = @{ x = [int](([int]$e1.Groups[1].Value + [int]$e1.Groups[3].Value) / 2); y = [int](([int]$e1.Groups[2].Value + [int]$e1.Groups[4].Value) / 2) }
    } else {
      $userBox = @{ x = [int](([int]$e0.Groups[5].Value + [int]$e0.Groups[7].Value) / 2); y = [int](([int]$e0.Groups[6].Value + [int]$e0.Groups[8].Value) / 2) }
      $passBox = @{ x = [int](([int]$e1.Groups[5].Value + [int]$e1.Groups[7].Value) / 2); y = [int](([int]$e1.Groups[6].Value + [int]$e1.Groups[8].Value) / 2) }
    }
  }
  if (-not $userBox -or -not $passBox -or -not $loginBtn) {
    return @{ ok = $false; error = 'UI fields not found'; alive = $false; tabs = @(); queues = @(); notifOk = $false }
  }
  # pm clear => empty fields; Compose button stays disabled until both fields non-blank
  Tap $userBox.x $userBox.y
  Wait-Ms 400
  Type-Text $username
  Wait-Ms 600
  Tap $passBox.x $passBox.y
  Wait-Ms 400
  Type-Text $password
  Wait-Ms 800
  & $adb shell input keyevent 111 | Out-Null # hide IME
  Wait-Ms 400
  $afterType = Dump-Ui "prelogin_$($rol)"
  $enabledBtn = $null
  foreach ($n in [regex]::Matches($afterType, '<node[^>]+>')) {
    $s = $n.Value
    if ($s -match 'clickable="true"' -and $s -match 'enabled="true"' -and $s -match 'bounds="\[(\d+),(\d+)\]\[(\d+),(\d+)\]"') {
      $y1 = [int]$Matches[2]
      if ($y1 -ge 1400 -and $y1 -le 1700) {
        $enabledBtn = @{ x = [int](([int]$Matches[1] + [int]$Matches[3]) / 2); y = [int](([int]$Matches[2] + [int]$Matches[4]) / 2) }
      }
    }
  }
  if ($enabledBtn) { Tap $enabledBtn.x $enabledBtn.y }
  elseif ($loginBtn) { Tap $loginBtn.x $loginBtn.y }
  else { Tap 540 1525 }
  Wait-Ms 12000
  $pid1 = (& $adb shell pidof com.metrik.satinalmapro).Trim()
  Wait-Ms 2500
  $pid2 = (& $adb shell pidof com.metrik.satinalmapro).Trim()
  $homeXml = Dump-Ui "home_$($rol)"
  $crash = (& $adb logcat -d -t 200 2>$null | Select-String -Pattern 'FATAL EXCEPTION|SatinalmaProCrash|AndroidRuntime: FATAL' | Select-Object -Last 5) -join ' | '
  $loggedIn = $homeXml -match 'MV'
  $bottom = Get-BottomTabs $homeXml
  $tabs = @($bottom.labels)
  $queueTitles = @()
  if ($loggedIn -and $bottom.nodes.Count -ge 2) {
    $islerNode = $bottom.nodes[1]
    Tap $islerNode.x $islerNode.y
    Wait-Ms 2000
    $islerXml = Dump-Ui "isler_$($rol)"
    & $adb shell input swipe 540 1700 540 700 400 | Out-Null
    Wait-Ms 800
    $islerXml = $islerXml + "`n" + (Dump-Ui "isler2_$($rol)")
    foreach ($q in @(
      'Gelen Talepler', 'Teklif Bekleyen', 'Teklif Girilen', 'Direk Onaylanan', 'Onay Geçmişi', 'Onaylanan Teklifler', 'Geçmiş Talepler', 'Red Talepler',
      'Yeni Talep', 'Taleplerim', 'Onay Bekleyen', 'Onaylanan Talepler',
      'Teklif İstenen', 'Yönetime Gönderilen', 'Düzeltme Bekleyen', 'Karşılaştırma', 'Teklifsiz Firma/Fiyat', 'Sipariş Bekleyen', 'Sipariş Verilen', 'Mal Kabul', 'Sipariş & Mal Kabul',
      'Bu rol için kuyruk yok'
    )) {
      if ($islerXml -match [regex]::Escape($q)) { $queueTitles += $q }
    }
  }
  $notifOk = $false
  $notifTap = ''
  if ($loggedIn) {
    $bildNode = $bottom.nodes | Where-Object { $_.text -match 'Bildirim' } | Select-Object -First 1
    if (-not $bildNode -and $bottom.nodes.Count -ge 3) {
      # Ana, İşler, [Malzeme?], Bildirim, Profil — Bildirim is second-to-last when Malzeme absent, or 4th with Malzeme
      $bildNode = if ($bottom.nodes.Count -ge 5) { $bottom.nodes[3] } else { $bottom.nodes[$bottom.nodes.Count - 2] }
    }
    if ($bildNode) {
      Tap $bildNode.x $bildNode.y
      Wait-Ms 2500
      $nx = Dump-Ui "bildirim_$rol"
      $notifOk = ($nx -match 'Bildirim') -and ($pid2 -ne '')
      $row = Get-Bounds $nx 'Yönetime'
      if (-not $row) { $row = Get-Bounds $nx 'Teklif' }
      if (-not $row) { $row = Get-Bounds $nx 'Onay' }
      if (-not $row) { $row = Get-Bounds $nx 'TLP-' }
      if ($row) {
        Tap $row.x $row.y
        Wait-Ms 2500
        $after = Dump-Ui "notif_tap_$rol"
        $notifTap = (([regex]::Matches($after, 'text="([^"]{2,40})"')).Groups | Where-Object { $_.Name -eq '1' } | ForEach-Object { $_.Value } | Select-Object -Unique | Select-Object -First 10) -join '|'
      }
    }
  }
  return @{
    ok = ($pid1 -ne '' -and $pid1 -eq $pid2)
    pid = $pid2
    crash = $crash
    tabs = $tabs
    queues = $queueTitles
    notifOk = $notifOk
    notifTap = $notifTap
    alive = ($pid2 -ne '')
    loggedIn = [bool]$loggedIn
  }
}

foreach ($u in $creds.users) {
  $res = Login-Role $u.username $u.password $u.rol
  $row = [pscustomobject]@{
    rol = $u.rol
    username = $u.username
    alive = $res.alive
    loggedIn = $res.loggedIn
    noCrashKick = $res.ok
    tabs = ($res.tabs -join ',')
    queues = ($res.queues -join ' | ')
    queueCount = @($res.queues).Count
    notifOk = $res.notifOk
    notifTap = $res.notifTap
    crash = $res.crash
    error = $res.error
  }
  $report += $row
  $row | Format-List | Out-String | Write-Host
}

$report | ConvertTo-Json -Depth 5 | Set-Content "$outDir\report.json" -Encoding UTF8
$report | Format-Table rol, loggedIn, alive, noCrashKick, tabs, queueCount, notifOk -AutoSize | Out-String | Write-Host
Write-Host "REPORT $outDir\report.json"
