; Windows installer for Math Runner. See RELEASING.md.
; Build the game first (Builds/Windows/MathRunner.exe), then from the repo root:
;   ISCC.exe /DAppVersion=0.1.0 Tools/installer/MathRunner.iss
; The setup file is written to Builds/Installer/MathRunner-Setup.exe. Never rename it:
; the website downloads releases/latest/download/MathRunner-Setup.exe.

#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif
#define AppName "Math Runner"
#define AppPublisher "IT Kamianets"
#define AppUrl "https://math-runner.itkamianets.com"
#define AppExe "MathRunner.exe"

[Setup]
AppId={{E0056D21-7BB3-4F7E-9012-05773BF432B8}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\..\Builds\Installer
OutputBaseFilename=MathRunner-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#AppExe}
LicenseFile=..\..\LICENSE
; In-game updater: it runs this file with /SILENT. Same AppId replaces the old install in place.
CloseApplications=yes
RestartApplications=no
UsePreviousTasks=yes
UsePreviousLanguage=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "ukrainian"; MessagesFile: "compiler:Languages\Ukrainian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\..\Builds\Windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; \
    Excludes: "*_BurstDebugInformation_DoNotShip*,*_BackUpThisFolder_ButDontShipItWithYourGame*"

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent
; A silent update (started by the game) has no finish page, so start the game again here.
Filename: "{app}\{#AppExe}"; Flags: nowait; Check: WizardSilent
