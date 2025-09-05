using System.Collections.Generic;
using System.Diagnostics;

namespace SolutionExtensions.Launcher
{
    public class LauncherProcess
    {
        public static Process RunExtension(string dllPath, string className, string dteMonikerName, string packageId)
        {
            var si = new ProcessStartInfo(typeof(Program).Assembly.Location);
            var args = new List<string>()
            {
                dllPath,
                className,
                dteMonikerName,
                packageId
            };
            args.Add("/waitfordebugger");
            args.Add("/break");
            args.Add("/waitforenter");
            si.Arguments = string.Join(" ", args);
            si.UseShellExecute = false;
            si.ErrorDialog = true;
            //si.CreateNoWindow = true;
            //si.RedirectStandardOutput = true;
            si.RedirectStandardOutput = true;
            var p = Process.Start(si);
            return p;
        }
    }
}
