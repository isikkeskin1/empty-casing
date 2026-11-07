#define MyAppName "Capri Desktop (Unofficial)"
#ifndef MyAppVersion
#define MyAppVersion "0.7.0"
#endif
#define MyAppExeName "Capri.Desktop.exe"

[Setup]
AppId={{B6CF8AF6-F5D7-4E0B-8C7A-6A615A89D158}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=Capri Desktop Contributors
AppPublisherURL=https://github.com/isikkeskin1/empty-casing
AppSupportURL=https://github.com/isikkeskin1/empty-casing/issues
AppUpdatesURL=https://github.com/isikkeskin1/empty-casing/releases
DefaultDirName={localappdata}\Programs\CapriDesktop
DefaultGroupName=Capri Desktop
UninstallDisplayIcon={app}\{#MyAppExeName}
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
OutputDir=..\installer-output
OutputBaseFilename=Capri-Setup
SetupLogging=yes
CloseApplications=yes
RestartApplications=no
DisableProgramGroupPage=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Optional PRIVATE asset pack is used only in licensed local/CI builds that supply the ZIP.
; Public builds do not include third-party character art by default.
Source: "..\assets\private\Capri-Animation-Pack-v0.2.zip"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

[Icons]
Name: "{autoprograms}\Capri Desktop"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\Capri Desktop"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch Capri Desktop"; Flags: nowait postinstall skipifsilent
; Silent verified auto-updates relaunch the upgraded Capri once setup finishes.
Filename: "{app}\{#MyAppExeName}"; Flags: nowait skipifnotsilent
