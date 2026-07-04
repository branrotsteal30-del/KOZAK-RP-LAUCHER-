[Setup]
AppName=KOZAK RP Launcher
AppVersion=3.9
AppPublisher=KOZAK RP
AppPublisherURL=https://kozakrp.qniks.me
AppSupportURL=https://kozakrp.qniks.me
AppUpdatesURL=https://kozakrp.qniks.me
DefaultDirName={commonappdata}\KOZAK_RP_Launcher
DefaultGroupName=KOZAK RP
AllowNoIcons=yes
OutputBaseFilename=KOZAK_RP_Launcher_Setup_v3.9
Compression=lzma2
SolidCompression=yes
OutputDir=installer_output
UninstallDisplayIcon={app}\KOZAK RP.exe
WizardStyle=modern
WizardImageFile=..\background.png
WizardSmallImageFile=..\header_logo.png
SetupIconFile=..\app.ico
AppCopyright=© 2026 KOZAK RP. All rights reserved.
DisableDirPage=no
DisableProgramGroupPage=yes
PrivilegesRequired=admin
UsePreviousAppDir=no
ShowLanguageDialog=yes
LanguageDetectionMethod=none
AppMutex=UKRAINEGTA_LAUNCHER
DirExistsWarning=no

[Languages]
Name: "ukrainian"; MessagesFile: "compiler:Languages\Ukrainian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Створити ярлик на робочому столі"; GroupDescription: "Додаткові ярлики"; Flags: unchecked
Name: "quicklaunchicon"; Description: "Створити ярлик у панелі швидкого запуску"; GroupDescription: "Додаткові ярлики"; Flags: unchecked

[Files]
; Build output files
Source: "..\bin\Release\net48\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Additional resources
Source: "..\background.png"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\background_gta5.png"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\header_logo.png"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\app.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\KOZAK RP Launcher"; Filename: "{app}\KOZAK RP.exe"; IconFilename: "{app}\KOZAK RP.exe"; Comment: "Запустити лаунчер KOZAK RP"
Name: "{group}\Відкрити папку лаунчера"; Filename: "{app}"; IconFilename: "{app}\KOZAK RP.exe"; Comment: "Відкрити папку встановлення"
Name: "{group}\Сайт KOZAK RP"; Filename: "https://kozakrp.qniks.me"; IconFilename: "{app}\KOZAK RP.exe"; Comment: "Перейти на офіційний сайт"
Name: "{group}\Деінсталяція KOZAK RP Launcher"; Filename: "{uninstallexe}"; Comment: "Видалити лаунчер"
Name: "{autodesktop}\KOZAK RP Launcher"; Filename: "{app}\KOZAK RP.exe"; Tasks: desktopicon; IconFilename: "{app}\KOZAK RP.exe"; Comment: "Запустити лаунчер KOZAK RP"
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\KOZAK RP Launcher"; Filename: "{app}\KOZAK RP.exe"; Tasks: quicklaunchicon; IconFilename: "{app}\KOZAK RP.exe"; Comment: "Запустити лаунчер KOZAK RP"

[Run]
; Removed automatic launcher start due to admin rights requirement
; Users can launch launcher manually from desktop icon or start menu

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

[Code]
procedure InitializeWizard;
begin
  // Modern wizard customization
  WizardForm.WizardBitmapImage.Height := WizardForm.ClientHeight;
  
  // Set modern colors
  WizardForm.Color := clWhite;
  WizardForm.Font.Color := clBlack;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  // Update wizard pages with modern styling
  if CurPageID = wpWelcome then
    WizardForm.Caption := 'Встановлення KOZAK RP Launcher v3.9'
  else if CurPageID = wpFinished then
    WizardForm.Caption := 'Встановлення завершено'
  else
    WizardForm.Caption := 'KOZAK RP Launcher v3.9 - Встановлення';
end;
