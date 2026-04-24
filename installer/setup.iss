#define MyAppName "PharmacyERP"
#define MyAppExeName "PharmacyERP.exe"
#define PgPassword "postgres123"
#define MyAppVersion "0.0.0"   ; overridden from CI via /DMyAppVersion=...

[Setup]
AppId={{A1F6C7B2-1234-4D9F-ABCD-1234567890AB}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
DefaultDirName={autopf}\{#MyAppName}
OutputDir=output
OutputBaseFilename=PharmacyERP-Setup-{#MyAppVersion}
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin
UninstallDisplayName={#MyAppName}
SetupLogging=yes

[Files]
Source: "app\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion
Source: "postgresql\postgresql-installer.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall
Source: "scripts\init-db.sql"; DestDir: "{tmp}"; Flags: deleteafterinstall

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

function IsPostgresInstalled(): Boolean;
var
  ResultCode: Integer;
begin
  Exec('cmd.exe', '/C sc query postgresql-x64-16', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := (ResultCode = 0);
end;

function GetPgBinPath(): string;
var
  InstallDir: string;
begin
  if RegQueryStringValue(HKLM, 'SOFTWARE\PostgreSQL\Installations\PostgreSQL 16', 'Base Directory', InstallDir) then
    Result := InstallDir + '\bin'
  else
    Result := '';
end;

procedure InstallPostgres();
begin
  Log('Installing PostgreSQL...');
  ExecOrFail(
    ExpandConstant('{tmp}\postgresql-installer.exe'),
    '--mode unattended --superpassword ' + '{#PgPassword}',
    'PostgreSQL installation failed.'
  );
end;

procedure WaitForPostgres();
var
  I: Integer;
  PgBin: string;
begin
  PgBin := GetPgBinPath();

  for I := 1 to 12 do
  begin
    if PgBin <> '' then
    begin
      if ExecAndLog(
        'cmd.exe',
        '/C "' + PgBin + '\pg_isready.exe" -U postgres'
      ) then
        exit;
    end
    else
    begin
      if ExecAndLog(
        'cmd.exe',
        '/C pg_isready -U postgres'
      ) then
        exit;
    end;

    Sleep(3000);
  end;

  MsgBox('PostgreSQL did not become ready in time.', mbError, MB_OK);
  Abort;
end;

procedure InitDatabase();
var
  PgBin: string;
begin
  Log('Initializing database...');
  PgBin := GetPgBinPath();

  if PgBin <> '' then
  begin
    ExecOrFail(
      'cmd.exe',
      '/C set PGPASSWORD={#PgPassword} && "' + PgBin + '\psql.exe" -U postgres -f "' + ExpandConstant('{tmp}\init-db.sql') + '"',
      'Database initialization failed.'
    );
  end
  else
  begin
    ExecOrFail(
      'cmd.exe',
      '/C set PGPASSWORD={#PgPassword} && psql -U postgres -f "' + ExpandConstant('{tmp}\init-db.sql') + '"',
      'Database initialization failed.'
    );
  end;
end;

procedure SetConnectionString();
var
  ResultCode: Integer;
begin
  Log('Setting PHARMA_DB environment variable...');
  Exec(
    'cmd.exe',
    '/C setx PHARMA_DB "Host=localhost;Database=pharmacydb;Username=pharma_user_1;Password=sh_pharma_user_2026_1"',
    '',
    SW_HIDE,
    ewWaitUntilTerminated,
    ResultCode
  );
end;

procedure RunMigration();
begin
  Log('Running EF migration...');
  ExecOrFail(
    ExpandConstant('{app}\PharmacyERP.exe'),
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
    if not IsUpgrade() then
    begin
      Log('Fresh install detected');

      if not IsPostgresInstalled() then
      begin
        InstallPostgres();
      end;

      WaitForPostgres();
      InitDatabase();
      SetConnectionString();
    end
    else
    begin
      Log('Upgrade detected - skipping DB init');
      WaitForPostgres();
    end;

    RunMigration();
  end;
end;