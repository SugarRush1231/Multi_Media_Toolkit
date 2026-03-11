; Inno Setup Script for Multi Media Toolkit (YoutubeDownloader)
#define MyAppName "Multi Media Toolkit"
#define MyAppVersion "1.0.3"
#define MyAppPublisher "KBS"
#define MyAppExeName "YoutubeDownloader.exe"
#define MyIcoName "mmt.ico"

[Setup]
; AppId: 이 앱만의 고유 ID입니다. (중복 설치 방지)
AppId={{D8C8E1F2-A6B1-4A2D-B5C4-E1F7B9D2C3E4}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}

; 제어판(프로그램 추가/제거) 설정
UninstallDisplayName={#MyAppName} (v{#MyAppVersion})
UninstallFileName=MMT_Uninstaller
UninstallDisplayIcon={app}\{#MyAppExeName}

; 버전 및 정보 설정
VersionInfoVersion={#MyAppVersion}
AllowNoIcons=yes
; 관리자 권한 필수 (Program Files 설치 및 시스템 권한 확보)
PrivilegesRequired=admin
; 설치 경로 변경 허용
DisableDirPage=no

; 설치 파일 외형 및 압축
SetupIconFile=d:\AI\test\YoutubeDownloader\mmt.ico
Compression=lzma2/ultra64
SolidCompression=yes
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
OutputDir=d:\AI\test\YoutubeDownloader\dist
OutputBaseFilename=MMT_Setup_v1.0.3
WizardStyle=modern

[Languages]
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; 1. 'dotnet publish'로 생성된 모든 배포 파일 포함
; 주의: 반드시 'dotnet publish -c Release -r win-x64 --self-contained' 명령을 먼저 실행해야 합니다.
Source: "d:\AI\test\YoutubeDownloader\bin\Release\net10.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; 2. 아이콘 및 외부 도구(ffmpeg, yt-dlp 등) 명시적 포함 (누락 방지)
Source: "d:\AI\test\YoutubeDownloader\mmt.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "d:\AI\test\YoutubeDownloader\ffmpeg.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "d:\AI\test\YoutubeDownloader\ffprobe.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "d:\AI\test\YoutubeDownloader\yt-dlp.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyIcoName}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyIcoName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
