using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SolutionExtensions.Launcher
{
    public class LauncherProcess
    {
        public static readonly string ERROR = "[ERROR]";

        public static readonly string DONE = "[DONE]";
        public static readonly string WAIT = "[WAIT]";
        public static readonly string PREPARE = "[PREPARE]";
        public static readonly string RUN = "[RUN]";
        public static readonly string LOG = "[LOG]";

        public static Process RunExtension(
            string launchedExecutable,
            string dllPath,
            string className,
            string dteMonikerName,
            string packageId,
            string argument,
            bool waitForDebugger,
            Action<string> onOutputData)
        {
            var args = new List<string>()
            {
                $"\"{dllPath}\"",
                className,
                dteMonikerName,
                packageId,
                $"\"{argument?.Replace("\"","\"\"")}\""
            };
            if (waitForDebugger)
                args.Add("/waitfordebugger");
            //args.Add("/break");
            //args.Add("/waitforenter");
            var p = new Process();
            p.StartInfo = new ProcessStartInfo(launchedExecutable)
            {
                Arguments = string.Join(" ", args),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.UTF8,
            };
            p.EnableRaisingEvents = true;
            p.OutputDataReceived += (_, e) => onOutputData(e.Data);
            p.Start();
            p.BeginOutputReadLine();
            return p;
        }

    }
}

