@ECHO OFF
SET ILM="C:\Users\jstuc\Dropbox\Tools\ILMerge\ILMerge.exe" 
SET D=%CD%
SET DST=%~dp0%..\libs\SolutionExtensions.Launcher.Merged.exe
SET SRC=%1
if %SRC%.==. SET SRC=%~dp0%bin\debug
cd %SRC%
%ILM% /out:%DST% /t:exe SolutionExtensions.Launcher.exe envdte.dll Microsoft.VisualStudio.Interop.dll Microsoft.VisualStudio.Imaging.Interop.14.0.DesignTime.dll Mono.Cecil.dll
cd %D%
