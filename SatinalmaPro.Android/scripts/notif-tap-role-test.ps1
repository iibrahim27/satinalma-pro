# Notification tap deep-link test per role
$ErrorActionPreference = 'Continue'
$adb = "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe"
$creds = Get-Content "$env:TEMP\satinalma-notif-tap-creds.json" -Raw | ConvertFrom-Json
$outDir = "$env:TEMP\satinalma-notif-tap-test"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$report = @()

function Dump-Ui([string]$name) {
  & $adb shell uiautomator dump /sdcard/ui.xml 2>&1 | Out-Null
  cmd /c "`"$adb`" pull /sdcard/ui.xml `"$outDir\$name.xml`" >nul 2>&1"
  cmd /c "`"$adb`" exec-out screencap -p > `"$outDir\$name.png`" 2>nul"
  if (Test-Path "$outDir\$name.xml") { return (Get-Content "$outDir\$name.xml" -Raw -ErrorAction SilentlyContinue) }
  return ''
}
function Has([string]$xml, [string]$t) { return $xml -match [regex]::Escape($t) }
function Tap([int]$x, [int]$y) { & $adb shell input tap $x $y | Out-Null }
function Type-Text([string]$t) {
  $esc = $t -replace ' ', '%s' -replace '([\\&|;<>()\$`!"' + "'" + '])', '\$1'
  & $adb shell "input text $esc" | Out-Null
}
function Wait-Ms([int]$ms) { Start-Sleep -Milliseconds $ms }

function Login([string]$username, [string]$password, [string]$tag) {
  & $adb shell pm clear com.metrik.satinalmapro | Out-Null
  & $adb shell pm grant com.metrik.satinalmapro android.permission.POST_NOTIFICATIONS 2>$null | Out-Null
  & $adb shell cmd appops set com.metrik.satinalmapro POST_NOTIFICATION allow 2>$null | Out-Null
  & $adb shell am start -n com.metrik.satinalmapro/com.satinalmapro.android.MainActivity | Out-Null
  Wait-Ms 5000
  $xml = Dump-Ui "login_$tag"
  $edits = [regex]::Matches($xml, 'class="android.widget.EditText"[^>]*bounds="\[(\d+),(\d+)\]\[(\d+),(\d+)\]"|bounds="\[(\d+),(\d+)\]\[(\d+),(\d+)\]"[^>]*class="android.widget.EditText"')
  if ($edits.Count -lt 2) { return $false }
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
  $pre = Dump-Ui "prelogin_$tag"
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
  # dismiss permission dialog if any
  $homeXml = Dump-Ui "home_$tag"
  if ((Has $homeXml 'İzin ver') -or (Has $homeXml 'Allow') -or (Has $homeXml 'izin')) {
    foreach ($label in @('İzin ver','Allow','İzin Ver','While using the app','Uygulamayı kullanırken')) {
      foreach ($n in [regex]::Matches($homeXml, '<node[^>]+>')) {
        if ($n.Value -match [regex]::Escape($label) -and $n.Value -match 'bounds="\[(\d+),(\d+)\]\[(\d+),(\d+)\]"') {
          Tap ([int](([int]$Matches[1]+[int]$Matches[3])/2)) ([int](([int]$Matches[2]+[int]$Matches[4])/2))
          Wait-Ms 1500
          break
        }
      }
    }
    $homeXml = Dump-Ui "home2_$tag"
  }
  return (Has $homeXml 'MV')
}

function Simulate-NotifTap([string]$route, [string]$tag) {
  # Exact PendingIntent extras used by LocalNotificationHelper / FCM tap
  & $adb shell am start -n com.metrik.satinalmapro/com.satinalmapro.android.MainActivity `
    -a android.intent.action.VIEW `
    --es bildirim_route "$route" `
    --es bildirim_id "tap-$tag" `
    --activity-single-top | Out-Null
  Wait-Ms 3500
  return (Dump-Ui "after_$tag")
}

foreach ($u in $creds.users) {
  $tag = $u.rol
  Write-Host "=== $($u.rol) route=$($u.route) ==="
  $loggedIn = Login $u.username $u.password $tag
  $after = ''
  $matched = @()
  $landedDashboard = $false
  $landedBildirim = $false
  if ($loggedIn) {
    $after = Simulate-NotifTap $u.route $tag
    foreach ($e in @($u.expect)) {
      if (Has $after $e) { $matched += $e }
    }
    # Also accept top bar title / screen content without exact Turkish encoding issues
    $landedDashboard = (Has $after 'Öncelikli') -or (Has $after 'Hızlı işlem') -or ((Has $after 'Bekleyen') -and (Has $after 'Son hareket'))
    $landedBildirim = (Has $after 'Bildirim yok') -or ((Has $after 'Bildirimler') -and (Has $after 'Okundu') -and (Has $after 'Temizle'))
  }
  $pass = [bool]($loggedIn -and ($matched.Count -gt 0) -and (-not $landedDashboard) -and (-not $landedBildirim))
  # For stock screens, dashboard check may false-positive; refine:
  if ($u.route -like 'stok-*') {
    $pass = [bool]($loggedIn -and ($matched.Count -gt 0) -and (-not $landedBildirim))
  }
  $row = [pscustomobject]@{
    rol = $u.rol
    route = $u.route
    loggedIn = $loggedIn
    matched = ($matched -join ',')
    landedDashboard = $landedDashboard
    landedBildirimList = $landedBildirim
    pass = $pass
    preview = ((([regex]::Matches($after, 'text="([^"]{2,40})"')).Groups | Where-Object Name -eq 1 | ForEach-Object Value | Select-Object -Unique | Select-Object -First 12) -join ' | ')
  }
  $report += $row
  $row | Format-List | Out-String | Write-Host
}

$report | ConvertTo-Json -Depth 5 | Set-Content "$outDir\report.json" -Encoding UTF8
$report | Format-Table rol,route,pass,loggedIn,matched,landedDashboard,landedBildirimList -AutoSize | Out-String | Write-Host
$allPass = -not ($report | Where-Object { -not $_.pass })
Write-Host "ALL_PASS=$allPass"
Write-Host "REPORT $outDir\report.json"
