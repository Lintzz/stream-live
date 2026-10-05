; version.iss é gerado pelo build a partir de <Version> no .csproj — não editar à mão.
#include "version.iss"

; O ISCC resolve os caminhos do script contra o diretório de onde ele foi chamado, e não
; contra a pasta do script. Sem ancorar, compilar da raiz ("ISCC build\setup.iss" — que é o
; comando documentado) falhava com "não foi possível encontrar o caminho".
; SourcePath é a pasta deste arquivo; subir um nível dá a raiz do repositório, e é a ela que
; todos os caminhos abaixo se referem, venha a chamada de onde vier.
#define RepoRoot AddBackslash(SourcePath) + ".."

[Setup]
SourceDir={#RepoRoot}
; O AppId é o que o Windows usa para reconhecer uma instalação anterior e atualizar por cima.
; Sem ele o Inno usa o AppName, e quem renomeasse o app passaria a ter duas cópias instaladas
; lado a lado (o auto-update instalaria a segunda). O valor é o mesmo que já valia por padrão:
; não mude, nem para acompanhar um rename.
AppId=Stream Live
AppName=Stream Live
AppVersion={#AppVersion}
AppPublisher=Lintzz
AppPublisherURL=https://github.com/Lintzz/stream-live
AppSupportURL=https://github.com/Lintzz/stream-live/issues
AppUpdatesURL=https://github.com/Lintzz/stream-live/releases
DefaultDirName={pf}\Stream Live
DefaultGroupName=Stream Live
UninstallDisplayIcon={app}\StreamLiveApp.exe
Compression=lzma2
SolidCompression=yes
OutputDir=.
OutputBaseFilename=StreamLive_Setup
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
SetupIconFile=src\StreamLiveApp\Assets\app_icon.ico

[Files]
Source: "publish_zip\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Stream Live"; Filename: "{app}\StreamLiveApp.exe"; WorkingDir: "{app}"
Name: "{commondesktop}\Stream Live"; Filename: "{app}\StreamLiveApp.exe"; WorkingDir: "{app}"

[Run]
Filename: "{app}\StreamLiveApp.exe"; Description: "Launch Stream Live"; Flags: nowait postinstall skipifsilent
