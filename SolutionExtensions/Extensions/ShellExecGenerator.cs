using EnvDTE;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SolutionExtensions
{
    //arguments:
    //  mygen %1 %2 -debug //%1 will be finename, %2 namespace
    //  if no %1 used, it will be added to the end of the string
    //TODO:
    // Argument can contain $(SOLUTIONDIR) or system variables (or is SDIR there?)

    //generator always return name of generated file(s)
    // if your command cannot do it, just encapsulate it into cmd
    //  mygen.exe >myfile.txt
    //  echo myfile.txt

#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
    public class ShellExecGenerator
    {
        [Description("Command line to execute")]
        //[DefaultValue("myGenerator %1 -debug")]
        public string Argument { get; set; }
        public string Extension { get; set; } = ".default";
        [Description("Shell execute")]
        public async Task<string[]> Generate(DTE dte, string input, string inputFileName, string ns)
        {
            var ps = new ProcessStartInfo
            {
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                FileName = "cmd.exe",
                Arguments = "/c " + GetCommandLine(inputFileName, ns),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using var p = System.Diagnostics.Process.Start(ps);
            p.WaitForExit(15000);
            var output = await p.StandardOutput.ReadToEndAsync();
            var err = await p.StandardError.ReadToEndAsync();
            if (!string.IsNullOrWhiteSpace(err))
                throw new System.Exception($"Process returned error:\n{err}");
            var lines = output.Split('\n').Select(line => line.TrimEnd('\r')).ToArray();
            return lines;
        }

        private string GetCommandLine(string inputFileName, string ns)
        {
            var s = Argument;
            var s2 = s.Replace("%1", inputFileName);
            if (s != s2)
            {
                inputFileName = null; //used, do not add
                s = s2;
            }
            s = s.Replace("%2", ns);
            if (inputFileName != null)
                s += " " + inputFileName;
            return s;
        }
    }
}
