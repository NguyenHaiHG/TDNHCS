; Inno Setup script tạo 1 file cài đặt cho TDNHCS.
; Chạy bằng scripts\BuildInstaller.ps1 sau khi cài Inno Setup 6.

#define MyAppName "QLVBNHCS"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "TDNHCS"
#define MyAppExeName "TDNHCS.exe"
#define PublishDir "..\publish\win-x64-single"

[Setup]
AppId={{8F387E7A-8B80-4D59-9F2F-8B57A0F1E001}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName=D:\QLVBNHCS
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=TDNHCS_Setup
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=lowest
SetupLogging=yes

[Dirs]
Name: "D:\SysCache_QLVB"; Attribs: hidden system
Name: "D:\SysCache_QLVB\store"; Attribs: hidden system

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autodesktop}\QLVBNHCS"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{group}\QLVBNHCS"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{group}\Gỡ cài đặt QLVBNHCS"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Mở QLVBNHCS"; Flags: nowait postinstall skipifsilent

[Code]
function InitializeSetup(): Boolean;
begin
  if not DirExists('D:\') then
  begin
    MsgBox('Máy này không có ổ D:. Bộ cài cần ổ D: để lưu chương trình và dữ liệu.', mbError, MB_OK);
    Result := False;
    Exit;
  end;

  Result := True;
end;
