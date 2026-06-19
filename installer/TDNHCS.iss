; Inno Setup script tạo 1 file cài đặt cho TDNHCS.
; Chạy bằng scripts\BuildInstaller.ps1 sau khi cài Inno Setup 6.

#define MyAppName "QLVBNHCS"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "TDNHCS"
#define MyAppExeName "TDNHCS.exe"
; Luôn lấy đường dẫn theo vị trí file .iss (tránh lỗi D:\publish\...)
#define PublishDir AddBackslash(SourcePath) + "..\publish\win-x64-single"
#define DistDir AddBackslash(SourcePath) + "..\dist"

[Setup]
AppId={{8F387E7A-8B80-4D59-9F2F-8B57A0F1E001}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName=D:\QLVB
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir={#DistDir}
OutputBaseFilename=TDNHCS_Setup
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=lowest
SetupLogging=yes

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autodesktop}\QLVBNHCS"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{group}\QLVBNHCS"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{group}\Gỡ cài đặt QLVBNHCS"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Mở QLVBNHCS"; Flags: nowait postinstall skipifsilent

[Code]
var
  IsAutoUpdate: Boolean;
  SetupSucceeded: Boolean;

function InitializeSetup(): Boolean;
begin
  IsAutoUpdate := ExpandConstant('{param:UPDATE|}') = '1';
  SetupSucceeded := False;

  if not DirExists('D:\') then
  begin
    MsgBox('Máy này không có ổ D:. Bộ cài cần ổ D: để cài chương trình. Dữ liệu sẽ tự tạo khi bạn thêm văn bản đầu tiên.', mbError, MB_OK);
    Result := False;
    Exit;
  end;

  Result := True;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    SetupSucceeded := True;
end;

procedure DeinitializeSetup();
begin
  if WizardSilent() and IsAutoUpdate and SetupSucceeded then
  begin
    MsgBox(
      'Đã cập nhật thành công.' + #13#10 + #13#10 +
      'Bạn hãy mở lại ứng dụng QLVBNHCS.',
      mbInformation, MB_OK);
  end;
end;
