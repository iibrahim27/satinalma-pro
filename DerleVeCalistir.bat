@echo off
setlocal EnableExtensions

set "PROJE=%~dp0Satinalma Pro\SatinalmaPro.csproj"
set "EXE=%~dp0Satinalma Pro\bin\Release\net9.0-windows10.0.17763.0\SatinalmaPro.exe"

title Satinalma Pro

taskkill /IM SatinalmaPro.exe /F >nul 2>&1

echo.
echo Satinalma Pro derleniyor...
dotnet build "%PROJE%" -c Release --verbosity minimal
if errorlevel 1 (
    echo Derleme basarisiz.
    pause
    exit /b 1
)

if not exist "%EXE%" (
    set "EXE=%~dp0Satinalma Pro\bin\Debug\net9.0-windows10.0.17763.0\SatinalmaPro.exe"
)

if not exist "%EXE%" (
    echo Hata: SatinalmaPro.exe bulunamadi.
    echo Beklenen: %~dp0Satinalma Pro\bin\Release\net9.0-windows10.0.17763.0\
    pause
    exit /b 1
)

start "" "%EXE%"
echo Uygulama acildi.
