[Setup]
AppId=Kopi.Desktop
AppName=Kopi
AppVersion=0.1.0
DefaultDirName={localappdata}\Programs\Kopi
PrivilegesRequired=lowest
DefaultGroupName=Kopi
OutputDir=..\artifacts
OutputBaseFilename=Kopi-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\Kopi.exe
[Files]
Source: "..\artifacts\app\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{group}\Kopi"; Filename: "{app}\Kopi.exe"
Name: "{autodesktop}\Kopi"; Filename: "{app}\Kopi.exe"
[Run]
Filename: "{app}\Kopi.exe"; Description: "Open Kopi"; Flags: nowait postinstall skipifsilent
