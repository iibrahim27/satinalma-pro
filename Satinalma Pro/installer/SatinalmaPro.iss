; Satınalma Pro — Inno Setup kurulum betiği
; Derleme: Inno Setup 6 kurulu olmalı, sonra ISCC.exe ile derlenir

#define MyAppName "Satınalma Pro"
#define MyAppVersion "2.1.111"
#define MyAppPublisher "MV İNŞAAT"
#define MyAppExeName "SatinalmaPro.exe"
#define MyTalepProName "Talep Pro"
#define MyTalepProExeName "TalepPro.exe"
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
Name: "taleppro"; Description: "Talep Pro'yu da bu bilgisayara kur"; GroupDescription: "Ek uygulamalar:"; Flags: checkedonce

[Files]
; Satınalma Pro — Talep Pro dosyaları ayrı görevle kurulur
Source: "{#MyPublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "TalepPro.exe,TalepPro.dll,TalepPro.runtimeconfig.json,TalepPro.deps.json,TalepPro.ico,TalepPro.pdb"
; Talep Pro (yalnızca «Talep Pro'yu da kur» işaretliyse)
Source: "{#MyPublishDir}\TalepPro.exe"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist; Tasks: taleppro
Source: "{#MyPublishDir}\TalepPro.dll"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist; Tasks: taleppro
Source: "{#MyPublishDir}\TalepPro.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist; Tasks: taleppro
Source: "{#MyPublishDir}\TalepPro.deps.json"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist; Tasks: taleppro
Source: "{#MyPublishDir}\TalepPro.ico"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist; Tasks: taleppro
; Publish içinde yoksa kaynak ikon (kısayol için)
Source: "..\..\TalepPro\Assets\app.ico"; DestDir: "{app}"; DestName: "TalepPro.ico"; Flags: ignoreversion skipifsourcedoesntexist; Tasks: taleppro

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{#MyTalepProName}"; Filename: "{app}\{#MyTalepProExeName}"; IconFilename: "{app}\TalepPro.ico"; Tasks: taleppro
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{autodesktop}\{#MyTalepProName}"; Filename: "{app}\{#MyTalepProExeName}"; IconFilename: "{app}\TalepPro.ico"; Tasks: desktopicon and taleppro

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
Filename: "{app}\{#MyTalepProExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyTalepProName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent unchecked; Tasks: taleppro

[UninstallDelete]
; Kurulum kaldırıldığında giriş hatırlatma dosyalarını sil (AppData — Program Files dışında)
Type: files; Name: "{userappdata}\SatinalmaPro\giris_tercihleri.json"
Type: files; Name: "{userappdata}\SatinalmaPro\giris_sifre.dat"
Type: files; Name: "{userappdata}\SatinalmaPro\oturum.json"
Type: files; Name: "{userappdata}\SatinalmaPro\kurulum_izleri.json"

[Code]
function InitializeSetup(): Boolean;
begin
  Result := True;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  InstallId: String;
begin
  if CurStep <> ssPostInstall then
    Exit;

  InstallId := GetDateTimeString('yyyymmddhhnnsszzz', #0, #0);
  SaveStringToFile(ExpandConstant('{app}\.install_id'), InstallId, False);
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  VeriKlasoru: String;
begin
  if CurUninstallStep <> usPostUninstall then
    Exit;

  VeriKlasoru := ExpandConstant('{userappdata}\SatinalmaPro');
  if not DirExists(VeriKlasoru) then
    Exit;

  if MsgBox(
    'Kayıtlı giriş bilgileri silindi.' + #13#10 + #13#10 +
    'Tüm uygulama verilerini de silmek ister misiniz?' + #13#10 +
    '(Talepler, ayarlar ve yerel yedekler — bulutta senkron varsa geri yüklenebilir.)',
    mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
  begin
    DelTree(VeriKlasoru, True, True, True);
  end;
end;
