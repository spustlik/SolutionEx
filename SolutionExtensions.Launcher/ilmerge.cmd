@ECHO OFF
call %~dp0\..\_env.cmd
if not .%ILMERGE%==. goto :envOk
	echo ERROR: missing _env.cmd
	goto :eof
:envOk

SET D=%CD%
SET DST=%~dp0%..\libs\SolutionExtensions.Launcher.Merged.exe
SET SRC=%1
if %SRC%.==. SET SRC=%~dp0%bin\debug
cd %SRC%
%ILMERGE% /out:%DST% /t:exe SolutionExtensions.Launcher.exe envdte.dll Microsoft.VisualStudio.Interop.dll Microsoft.VisualStudio.Imaging.Interop.14.0.DesignTime.dll Mono.Cecil.dll
cd %D%
