#define AppName "Kill Port"
#define AppVersion "1.0.0"
#define AppExe "KillPort.exe"
#define AppId "{{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}"

[Setup]
AppId={#AppId}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisherURL=https://github.com
DefaultDirName={localappdata}\KillPort
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir=.
OutputBaseFilename=KillPort-Setup
Compression=lzma2/ultra64
SolidCompression=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=
SetupIconFile=KillPort.App\app.ico
WizardStyle=modern
WizardSizePercent=100
ShowLanguageDialog=auto
LanguageDetectionMethod=uilanguage
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}
VersionInfoVersion={#AppVersion}
VersionInfoProductName={#AppName}
VersionInfoDescription=Close processes listening on TCP ports

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[CustomMessages]
english.CreateDesktopIcon=Create a &desktop shortcut
spanish.CreateDesktopIcon=Crear acceso directo en el &Escritorio
english.StartWithWindows=Start {#AppName} with &Windows
spanish.StartWithWindows=Iniciar {#AppName} al &arrancar Windows
english.LaunchNow=Launch {#AppName} now
spanish.LaunchNow=Iniciar {#AppName} ahora
english.UninstallShortcut=Uninstall
spanish.UninstallShortcut=Desinstalar

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{group}\{cm:UninstallShortcut}"; Filename: "{uninstallexe}"
Name: "{userdesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; Flags: unchecked
Name: "startupwin"; Description: "{cm:StartWithWindows}"; Flags: unchecked

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; \
  ValueType: string; ValueName: "{#AppName}"; ValueData: """{app}\{#AppExe}"""; \
  Flags: uninsdeletevalue; Tasks: startupwin

[Run]
Filename: "{app}\{#AppExe}"; \
  Description: "{cm:LaunchNow}"; \
  Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{userappdata}\KillPort"
