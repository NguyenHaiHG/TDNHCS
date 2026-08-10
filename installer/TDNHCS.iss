; Inno Setup script tạo 1 file cài đặt cho TDNHCS.
; Chạy bằng scripts\BuildInstaller.ps1 sau khi cài Inno Setup 6.

#define MyAppName "QLVBNHCS"
; Version được truyền từ BuildInstaller.ps1 qua /DMyAppVersion=x.x.x
; Nếu build thủ công mà không truyền, dùng fallback bên dưới
#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif
#define MyAppPublisher "TDNHCS"
#define MyAppExeName "TDNHCS.exe"
; Luôn lấy đường dẫn theo vị trí file .iss (tránh lỗi D:\publish\...)
#define PublishDir AddBackslash(SourcePath) + "..\publish\win-x64-single"
#define DistDir AddBackslash(SourcePath) + "..\dist"
#define PublishedExe AddBackslash(PublishDir) + MyAppExeName

#ifnexist PublishedExe
  #error "Chưa tìm thấy file publish\win-x64-single\TDNHCS.exe. Hãy chạy scripts\BuildInstaller.ps1 để publish app trước khi đóng gói Inno."
#endif

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
ArchitecturesAllowed=x64os
ArchitecturesInstallIn64BitMode=x64os
PrivilegesRequired=admin
SetupLogging=yes

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[InstallDelete]
Type: filesandordirs; Name: "{app}\*"
; Xóa shortcut cũ có thể tồn tại từ các bản test với tên khác
Type: files; Name: "{autodesktop}\TDNHCS.lnk"
Type: files; Name: "{autodesktop}\QLVB.lnk"
Type: files; Name: "{autodesktop}\Quản lý văn bản.lnk"

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

function StartsWith(const Value: string; const Prefix: string): Boolean;
begin
  Result := Copy(Value, 1, Length(Prefix)) = Prefix;
end;

function IsOldQlvbInstall(const KeyName: string; const DisplayName: string): Boolean;
var
  NormalizedName: string;
begin
  NormalizedName := Uppercase(DisplayName);
  Result :=
    (KeyName = '{#SetupSetting("AppId")}_is1') or
    StartsWith(NormalizedName, 'QLVBNHCS') or
    StartsWith(NormalizedName, 'TDNHCS') or
    StartsWith(NormalizedName, 'QLVB') or
    (Pos('QUAN LY VAN BAN', NormalizedName) > 0);
end;

procedure RunUninstaller(const UninstallString: string);
var
  ResultCode: Integer;
begin
  if UninstallString = '' then
  begin
    Exit;
  end;

  Exec(
    ExpandConstant('{cmd}'),
    '/C "' + UninstallString + ' /VERYSILENT /SUPPRESSMSGBOXES /NORESTART"',
    '',
    SW_HIDE,
    ewWaitUntilTerminated,
    ResultCode);
end;

procedure UninstallOldQlvbInstallsFromRoot(RootKey: Integer);
var
  Names: TArrayOfString;
  I: Integer;
  KeyName: string;
  DisplayName: string;
  QuietUninstallString: string;
  UninstallString: string;
begin
  if not RegGetSubkeyNames(RootKey, 'Software\Microsoft\Windows\CurrentVersion\Uninstall', Names) then
  begin
    Exit;
  end;

  for I := 0 to GetArrayLength(Names) - 1 do
  begin
    KeyName := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\' + Names[I];
    RegQueryStringValue(RootKey, KeyName, 'DisplayName', DisplayName);

    if IsOldQlvbInstall(Names[I], DisplayName) then
    begin
      if RegQueryStringValue(RootKey, KeyName, 'QuietUninstallString', QuietUninstallString) then
      begin
        RunUninstaller(QuietUninstallString);
      end
      else if RegQueryStringValue(RootKey, KeyName, 'UninstallString', UninstallString) then
      begin
        RunUninstaller(UninstallString);
      end;
    end;
  end;
end;

procedure UninstallOldQlvbInstalls();
begin
  UninstallOldQlvbInstallsFromRoot(HKCU);
  UninstallOldQlvbInstallsFromRoot(HKLM);
end;

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

  UninstallOldQlvbInstalls();

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
