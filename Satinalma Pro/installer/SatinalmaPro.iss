; Satınalma Pro — Inno Setup kurulum betiği
; Derleme: Inno Setup 6 kurulu olmalı, sonra ISCC.exe ile derlenir

#define MyAppName "Satınalma Pro"
#define MyAppVersion "2.1.10"
#define MyAppPublisher "MV İNŞAAT"
#define MyAppExeName "SatinalmaPro.exe"
#define MyPublishDir "..\bin\Release\net9.0-windows10.0.17763.0\win-x64\publish"

[Setup]
AppId={{A8F3C2E1-9B4D-4F6A-8C1E-2D5E7A9B0C3D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\Satinalma Pro
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..
OutputBaseFilename=SatinalmaPro_Kurulum
SetupIconFile=..\Assets\app.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Tasks]
Name: "desktopicon"; Description: "Masaüstü kısayolu oluştur"; GroupDescription: "Ek seçenekler:"; Flags: unchecked

[Files]
Source: "{#MyPublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
