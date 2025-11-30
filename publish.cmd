@ECHO OFF
call %~dp0/_env.cmd
if not "%VSInstallDir%"=="" goto :envOk
	echo ERROR: missing _dev.cmd with VSInstallDir,PAT,ILMERGE variables
	goto :eof
:envOk

SET MSBUILD="%VSInstallDir%\MSBuild\Current\Bin\msbuild.exe"
SET VSIX="%VSInstallDir%\VSSDK\VisualStudioIntegration\Tools\Bin\VsixPublisher.exe"
SET B=%MSBUILD% /t:Rebuild /p:Configuration=Release -noConsoleLogger /logger:FileLogger,Microsoft.Build.Engine;logfile=build.log

rem SET all variables to allow skipping
SET SRC=%~dp0
SET LAUNCHER=%SRC%\SolutionExtensions.Launcher\bin\release
SET X=%SRC%\SolutionExtensions\SolutionExtensions.csproj
SET RES=%SRC%\SolutionExtensions\bin\release

SET LOG=%~dp0\publish.log
DEL %LOG% >nul

ECHO --- Started at %DATE% %TIME% --->%LOG% 
if NOT %1.==. goto %1

:stepL
ECHO Building Launcher in Release mode
%B% SolutionExtensions.Launcher/SolutionExtensions.Launcher.csproj
type build.log>>%LOG%

:stepML
ECHO Merging Launcher
cmd /c SolutionExtensions.Launcher\ilmerge.cmd %LAUNCHER% >>%LOG%

:stepCL
ECHO Copying Launcher
copy /b %SRC%\libs\SolutionExtensions.Launcher.merged.exe %SRC%\SolutionExtensions>nul

:stepBX
ECHO Building VSIX in Release mode
%B% %X%
type build.log>>%LOG%

del build.log>nul

:stepP
echo -- Ignore error, if already logged in
%VSIX% login -publisherName "JanStuchlik" -personalAccessToken "%PAT%"
echo --
%VSIX% publish -payload %RES%\SolutionExtensions.vsix  -publishManifest %RES%\packageManifest.json -personalAccessToken "%PAT%"

