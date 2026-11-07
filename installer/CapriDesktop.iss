#define MyAppName "Capri Desktop (Unofficial)"
#ifndef MyAppVersion
  #define MyAppVersion "0.6.0"
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

[Icons]
Name: "{autoprograms}\Capri Desktop"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\Capri Desktop"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch Capri Desktop"; Flags: nowait postinstall skipifsilent
