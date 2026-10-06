#ifndef MyAppName
#define MyAppName "PharmacyERP"
#endif

#ifndef MyAppExeName
#define MyAppExeName "PharmacyERP.exe"
#endif

#ifndef MyAppVersion
#define MyAppVersion "0.0.0"   ; overridden from CI or MSBuild via /DMyAppVersion=...
#endif

#ifndef AppSourceDir
#define AppSourceDir "app"
#endif

#ifndef OutputDir
#define OutputDir "output"
#endif

[Setup]
AppId={{A1F6C7B2-1234-4D9F-ABCD-1234567890AB}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
DefaultDirName={autopf}\{#MyAppName}
OutputDir={#OutputDir}
OutputBaseFilename=PharmacyERP-Setup-{#MyAppVersion}
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin
UninstallDisplayName={#MyAppName}
SetupLogging=yes

[Files]
Source: "{#AppSourceDir}\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"

[Code]

function ExecAndLog(FileName, Params: string): Boolean;
var
  ResultCode: Integer;
begin
  Log('Executing: ' + FileName + ' ' + Params);
  Result := Exec(FileName, Params, '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Log('Exit code: ' + IntToStr(ResultCode));
end;

procedure ExecOrFail(FileName, Params, ErrorMessage: string);
begin
  if not ExecAndLog(FileName, Params) then
  begin
    Log('ERROR: ' + ErrorMessage);
    MsgBox(ErrorMessage, mbError, MB_OK);
    Abort;
  end;
end;

procedure RunMigration();
begin
  Log('Running EF migration...');
  ExecOrFail(
    ExpandConstant('{app}\{#MyAppExeName}'),
    '--migrate',
    'Database migration failed. Application cannot continue.'
  );
end;

function IsUpgrade(): Boolean;
begin
  Result := RegKeyExists(HKLM,
    'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#SetupSetting("AppId")}_is1');
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then
  begin
    if IsUpgrade() then
      Log('Upgrade detected')
    else
      Log('Fresh install detected');

    RunMigration();
  end;
end;
