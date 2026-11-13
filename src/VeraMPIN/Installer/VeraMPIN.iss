#define AppName "Vera mPIN"
#define AppVersion "1.0.3"
#define AppExeName "VeraMPIN.exe"
#define PayloadDir "..\artifacts\VeraMPIN-InstallerPayload"

[Setup]
AppId={{612D93E8-9D53-4E91-AB51-95E3A7F6740E}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=Vera mPIN
DefaultDirName={localappdata}\Programs\Vera mPIN
DefaultGroupName=Vera mPIN
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\artifacts\VeraMPIN-Installer
OutputBaseFilename=VeraMPIN-Setup-{#AppVersion}-x64
SetupIconFile=..\Assets\VeraMPIN.ico
UninstallDisplayIcon={app}\VeraMPIN.exe
InfoBeforeFile=ThirdPartyNotices.txt
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
Uninstallable=yes

[Types]
Name: "full"; Description: "Typical installation"
Name: "compact"; Description: "Vera mPIN and VeraCrypt only"
Name: "custom"; Description: "Custom installation"; Flags: iscustom

[Components]
Name: "main"; Description: "Vera mPIN and VeraCrypt"; Types: full compact custom; Flags: fixed
Name: "vcsource"; Description: "VeraCrypt 1.26.24 source archive and license (~40 MiB)"; Types: full custom

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#PayloadDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: main
Source: "..\ThirdParty\VeraCrypt_1.26.24_Source.zip"; DestDir: "{app}\ThirdParty"; Flags: ignoreversion; Components: vcsource
Source: "ThirdPartyNotices.txt"; DestDir: "{app}"; Flags: ignoreversion; Components: main
Source: "..\..\..\README.md"; DestDir: "{app}"; DestName: "README.md"; Flags: ignoreversion; Components: main

[Icons]
Name: "{autoprograms}\Vera mPIN"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"
Name: "{autoprograms}\Vera mPIN\Usage and license notices"; Filename: "{app}\README.md"
Name: "{autoprograms}\Vera mPIN\Uninstall Vera mPIN"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Vera mPIN"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch Vera mPIN"; Flags: postinstall nowait skipifsilent

[Code]
var
  RemoveUserDataOnUninstall: Boolean;

function InitializeUninstall(): Boolean;
var
  Response: Integer;
begin
  if UninstallSilent then
  begin
    RemoveUserDataOnUninstall := False;
    Result := True;
    exit;
  end;

  Response := MsgBox(
    'Also remove saved mount entries and Windows-user-protected mPIN credentials? This cannot be undone.',
    mbConfirmation, MB_YESNOCANCEL);
  Result := Response <> IDCANCEL;
  RemoveUserDataOnUninstall := Response = IDYES;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  UserDataDir: String;
begin
  if (CurUninstallStep = usPostUninstall) and RemoveUserDataOnUninstall then
  begin
    UserDataDir := ExpandConstant('{localappdata}\Vera-mPIN');
    if DirExists(UserDataDir) and not DelTree(UserDataDir, True, True, True) then
      MsgBox('Vera mPIN was uninstalled, but its saved settings could not be removed from:' + #13#10 + UserDataDir,
        mbError, MB_OK);

    UserDataDir := ExpandConstant('{localappdata}\ExternalSsdUnlock');
    if DirExists(UserDataDir) and not DelTree(UserDataDir, True, True, True) then
      MsgBox('Vera mPIN was uninstalled, but legacy saved settings could not be removed from:' + #13#10 + UserDataDir,
        mbError, MB_OK);
  end;
end;
