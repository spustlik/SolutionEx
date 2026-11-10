using EnvDTE;
using EnvDTE100;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Process = System.Diagnostics.Process;

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
        public static readonly string RESULT_EXT = "EXTENSION";
        public static readonly string RESULT_FILE = "OUTPUTFILE";
        public static readonly string RESULT_CONTENTFILE = "OUTPUTCONTENTFILE";


        private StringBuilder output = new StringBuilder();
        public string GetOutput() { return output.ToString(); }
        private bool waitingForDebugger = false;
        private string launcherExe;

        public LauncherProcess(string launcherExe)
        {
            this.launcherExe = launcherExe;
        }
        public Action<string> OnOutputLine { get; set; }
        private void LauncherProcess_OnOutputData(string line)
        {
            if (string.IsNullOrEmpty(line))
                return;
            if (!line.Contains("\n"))
                line += "\n";
            output.Append(line);
            if (line.StartsWith(LauncherProcess.WAIT))
                waitingForDebugger = true;
            this.OnOutputLine?.Invoke(line);
        }

        public Process CreateRunProcess(
            string dllPath,
            string className,
            string dteMonikerName,
            string packageId,
            bool waitForDebugger,
            string argument)
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
            return CreateProcess(launcherExe, LauncherProcess_OnOutputData, args);
        }
        public Process CreateGeneratorProcess(
            string dllPath,
            string className,
            string dteMonikerName,
            string packageId,
            bool waitForDebugger,
            string inputFilePath,
            string defaultNamespace)
        {
            var args = new List<string>()
            {
                $"\"{dllPath}\"",
                className,
                dteMonikerName,
                packageId,
                $"\"{inputFilePath?.Replace("\"","\"\"")}\"",
                defaultNamespace,
                "/generate"
            };
            if (waitForDebugger)
                args.Add("/waitfordebugger");
            return CreateProcess(launcherExe, LauncherProcess_OnOutputData, args);
        }

        private static Process CreateProcess(string launchedExecutable, Action<string> onOutputData, List<string> args)
        {
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
            return p;
        }

        public void StartAndWait(Process process, bool waitForDebug)
        {
            process.Start();
            process.BeginOutputReadLine();
            //wait for launcher wait
            while (!process.HasExited)
            {
                if (waitingForDebugger) break;
                process.WaitForExit(100);
            }
            if (!waitForDebug)
                return;
            if (process.HasExited)
                throw new Exception($"Launcher exited with {process.ExitCode}\n{output}");
        }

        public static IEnumerable<string> GeneratorResultToStrings(GeneratorResult result)
        {
            if (!String.IsNullOrWhiteSpace(result.Extension))
                yield return $"{RESULT_EXT}:{result.Extension}";
            if (result.Content != null)
            {
                var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".bin");
                File.WriteAllText(tempFile, result.Content);
                yield return $"{RESULT_CONTENTFILE}:{tempFile}";
            }
            else
            {
                foreach (var file in result.Files)
                {
                    yield return $"{RESULT_FILE}:{file}";
                }
            }


        }
        public static GeneratorResult GeneratorResultFromStrings(string[] lines)
        {
            var result = new GeneratorResult();
            var files = new List<string>();
            foreach (var line in lines)
            {
                var parts = line.Split(new[] { ':' }, 2);
                if (parts[0] == RESULT_EXT)
                {
                    result.Extension = parts[1];
                }
                if (parts[0] == RESULT_FILE)
                {
                    files.Add(parts[1]);
                }
                if (parts[0] == RESULT_CONTENTFILE)
                {
                    var fn = parts[1];
                    result.Content = File.ReadAllText(fn);
                    File.Delete(fn);
                }
            }
            result.Files = files.ToArray();
            return result;
        }
    }
}

