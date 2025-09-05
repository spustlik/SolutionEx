using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SolutionExtensions.Launcher
{
    public class LauncherProcess
    {
        public static Process RunExtension(
            string dllPath, string className, string dteMonikerName, string packageId,
            StringBuilder output)
        {
            //TODO: how to add exe to vsix?
            var path = Path.GetDirectoryName(typeof(SolutionExtensionsPackage).Assembly.Location);
            var fn = Path.Combine(path, "SolutionExtensions.Launcher.merged.exe");
            //for debugging purposes, it can be called directly to \bin\debug\SolutionExtensions.Launcher.exe
            fn = @"D:\GitHub\SolutionEx\SolutionExtensions.Launcher\bin\Debug\SolutionExtensions.Launcher.exe";
            var args = new List<string>()
            {
                $"\"{dllPath}\"",
                className,
                dteMonikerName,
                packageId
            };
            args.Add("/waitfordebugger");
            //args.Add("/break");
            //args.Add("/waitforenter");
            var p = new Process();
            p.StartInfo = new ProcessStartInfo(fn)
            {
                Arguments = string.Join(" ", args),
                UseShellExecute = false,
                //CreateNoWindow = true,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.UTF8,
            };
            p.EnableRaisingEvents = true;
            p.OutputDataReceived += (_, e) => output.AppendLine(e.Data);
            p.Start();
            p.BeginOutputReadLine();
            return p;
        }
    }
}
