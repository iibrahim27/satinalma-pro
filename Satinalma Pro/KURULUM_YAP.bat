@echo off
chcp 65001 >nul
setlocal EnableExtensions

cd /d "%~dp0"

echo.
echo ========================================
echo   Satinalma Pro - Kurulum Paketi Olustur
echo ========================================
echo.

if "%~1"=="" (
    echo Surum belirtilmedi - SatinalmaPro.csproj icindeki surum kullanilacak.
    echo.
    echo Kullanim:  KURULUM_YAP.bat SURUM
    echo Ornek:     KURULUM_YAP.bat 1.2.0
    echo.
    powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\kurulum-yap.ps1"
) else (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\kurulum-yap.ps1" -Version "%~1"
)

set EXIT_CODE=%ERRORLEVEL%
echo.
if %EXIT_CODE% NEQ 0 (
    echo [HATA] Islem basarisiz. Cikis kodu: %EXIT_CODE%
) else (
    echo [OK] Kurulum exe hazir: SatinalmaPro_Kurulum.exe
)
echo.
pause
exit /b %EXIT_CODE%
