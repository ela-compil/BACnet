; Inno Setup Script: http://www.jrsoftware.org/
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "Yabe"
#define MyAppVersion "1.0.4"
#define MyAppPublisher "Yabe Authors"
#define MyAppURL "http://sourceforge.net/projects/yetanotherbacnetexplorer"
#define MyAppExeName "Yabe.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{F8639277-80EB-4EC9-AE36-D4BF2115ABFA}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={pf}\{#MyAppName}
DefaultGroupName={#MyAppName}
LicenseFile=C:\Users\Fred\Dev\Yabe\trunk\Docs\MIT_license.txt
OutputBaseFilename=SetupYabe_v{#MyAppVersion}
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64 

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "czech"; MessagesFile: "compiler:Languages\Czech.isl"
Name: "danish"; MessagesFile: "compiler:Languages\Danish.isl"
Name: "dutch"; MessagesFile: "compiler:Languages\Dutch.isl"
Name: "finnish"; MessagesFile: "compiler:Languages\Finnish.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "greek"; MessagesFile: "compiler:Languages\Greek.isl"
Name: "hebrew"; MessagesFile: "compiler:Languages\Hebrew.isl"
Name: "hungarian"; MessagesFile: "compiler:Languages\Hungarian.isl"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "norwegian"; MessagesFile: "compiler:Languages\Norwegian.isl"
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"
Name: "portuguese"; MessagesFile: "compiler:Languages\Portuguese.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "serbian"; MessagesFile: "compiler:Languages\SerbianLatin.isl"
Name: "slovenian"; MessagesFile: "compiler:Languages\Slovenian.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "ukrainian"; MessagesFile: "compiler:Languages\Ukrainian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\..\Yabe\bin\Debug\Yabe.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\Yabe\bin\Debug\CalendarView.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\Yabe\bin\Debug\PacketDotNet.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\Yabe\bin\Debug\SharpPcap.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\Yabe\bin\Debug\ZedGraph.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\Yabe\bin\Debug\README.Txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\Docs\history.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\Docs\MIT_license.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\Docs\ZedGraph Calendar SharpPcap License-LGPL.txt"; DestDir: "{app}"; Flags: ignoreversion

Source: "..\..\Yabe\bin\Debug\ReadSinglePropDescr.Xml"; DestDir: "{app}"; Flags: ignoreversion

Source: "..\..\DemoServer\bin\Debug\DemoServer.exe"; DestDir: "{app}\AddOn"; Flags: ignoreversion
Source: "..\..\DemoServer\bin\Debug\DeviceStorage.Xml"; DestDir: "{app}\AddOn"; Flags: ignoreversion
                           
Source: "..\..\CodeExamples\Bacnet.Room.Simulator\bin\Debug\Bacnet.Room.Simulator.exe"; DestDir: "{app}\AddOn"; Flags: ignoreversion
Source: "..\..\CodeExamples\Bacnet.Room.Simulator\Readme.txt"; DestDir: "{app}\AddOn"; Flags: ignoreversion

Source: "..\..\Mstp.BacnetCapture\bin\Debug\Mstp.BacnetCapture.exe"; DestDir: "{app}\AddOn"; Flags: ignoreversion

[Icons]
Name: "{group}\Yabe"; Filename: "{app}\Yabe.Exe"

Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"

Name: "{group}\Doc\Yabe Directory"; Filename : "{app}"
Name: "{group}\Doc\Yabe Website"; Filename : "{#MyAppURL}"
Name: "{group}\Doc\Readme"; Filename: "{app}\Readme.txt"
Name: "{group}\Doc\Full source code"; Filename: "http://sourceforge.net/p/yetanotherbacnetexplorer/code/HEAD/tarball?path=/trunk"
   
Name: "{group}\AddOn\DemoServer"; Filename: "{app}\AddOn\DemoServer.Exe"
Name: "{group}\AddOn\Mstp.BacnetCapture"; Filename: "{app}\AddOn\Mstp.BacnetCapture.exe"
Name: "{group}\AddOn\Bacnet.Room.Simulator"; Filename: "{app}\AddOn\Bacnet.Room.Simulator.exe"
Name: "{group}\AddOn\RoomSimulatorReadme"; Filename: "{app}\AddOn\Readme.txt"

Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, "&", "&&")}}"; Flags: nowait postinstall skipifsilent

[Code]

function InitializeSetup(): Boolean;
begin
  Result := true;
  if not RegKeyExists(HKEY_LOCAL_MACHINE, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4') then
  begin
    MsgBox('Microsoft .NET 4.0 required', mbInformation, MB_OK);
  end
  else
  begin
    if FileExists('C:\Program Files (x86)\YabeAuthors\Yabe\Yabe.exe') then
    begin
      MsgBox('Previous Yabe version detected : it should be manually uninstalled from the control panel', mbInformation, MB_OK);
      Result := false;
    end
  end
end;

