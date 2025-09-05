using System.Collections.Generic;
using System.Diagnostics;

namespace SolutionExtensions.Launcher
{
    public class LauncherProcess
    {
        public static Process RunExtension(string dllPath, string className, string dteMonikerName, string packageId)
        {
            var psi = new ProcessStartInfo(typeof(Program).Assembly.Location);
            var args = new List<string>()
            {
                dllPath,
                className, 
                dteMonikerName, 
                packageId
            };
            args.Add("/waitfordebugger");
            args.Add("/break");
            psi.Arguments = string.Join(" ", args);
            //psi.CreateNoWindow = true;
            //psi.RedirectStandardOutput = true;            
            var p = Process.Start(psi);
            //p.OutputDataReceived
            return p;
        }
    }
}
