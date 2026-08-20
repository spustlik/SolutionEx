using EnvDTE;
using EnvDTE100;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using SolutionExtensions.Launcher;
using SolutionExtensions.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SolutionExtensions
{
    public class ExtensionDebugger
    {
        public static ExtensionDebugger Create(
            ExtensionItem item,
            SolutionExtensionsPackage package,
            ExtensionManager extensionManager,
            bool debug)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            //do not copy!
            var dllPath = extensionManager.GetRealPath(item.DllPath);
            var dte = package.GetService<DTE, DTE>();
            var monikerName = GetMonikerName(dte);
            var packageId = GetPackageId(dte, package);

            string launcherExe = GetLauncherExe(dte, package);
            var launcher = new LauncherProcess(launcherExe);
            launcher.OnOutputLine = (line) =>
            {
                package.AddToOutputPaneThreadSafe(line);
            };

            var result = new ExtensionDebugger(dte, item, launcher)
            {
                monikerName = monikerName,
                packageId = packageId,
                dllPath = dllPath,
                debug = debug
            };
            return result;
        }

        private DTE dte;
        private readonly ExtensionItem item;
        private string monikerName;
        private string packageId;
        private string dllPath;
        private bool debug;

        public LauncherProcess Launcher { get; }

        public ExtensionDebugger(DTE dte, ExtensionItem item, LauncherProcess launcherProcess)
        {
            this.dte = dte;
            this.item = item;
            this.Launcher = launcherProcess;
        }

        public async Task<(string content, string extension)> GenerateAsync(string inputFilePath, string defaultNamespace)
        {
            var process = Launcher.CreateGeneratorProcess(dllPath, item.ClassName, monikerName, packageId, debug, inputFilePath, defaultNamespace);
            Launcher.StartAndWait(process, debug);
            AttachVsDebugger(process);
            await process.WaitForExitAsync();
            string extension = null;
            string outputFile = null;
            foreach (var line in Launcher.GetOutput().Split('\n'))
            {
                var parts = line.Split(new[] { ':' }, 2);
                if (parts[0].Trim() == LauncherProcess.RESULT_EXT)
                    extension = parts[1];
                if (parts[0].Trim() == LauncherProcess.RESULT_FILE)
                    outputFile = parts[1];
            }
            if (outputFile == null || !File.Exists(outputFile))
                throw new Exception($"Output file cannot be found: {outputFile}");
            var content = File.ReadAllText(outputFile);
            File.Delete(outputFile);
            return (content, extension);
        }
        
        private void Process_Exited(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        public void Run(string argument)
        {
            var process = Launcher.CreateRunProcess(dllPath, item.ClassName, monikerName, packageId, debug, argument);
            Launcher.StartAndWait(process, debug);
            AttachVsDebugger(process);
            //caller must verify than there is an breakpoint
            //wait for process to end?
        }

        private void AttachVsDebugger(System.Diagnostics.Process process)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dbg = dte.Debugger as Debugger5;
            var p = FindDTEProcess(dbg, process.Id);
            if (p == null)
                throw new Exception($"Cannot find runner process {process.Id}");
            p.Attach();
        }

        private static string GetLauncherExe(DTE dte, SolutionExtensionsPackage package)
        {
            //added as Asset to extension manifest
            var path = Path.GetDirectoryName(typeof(SolutionExtensionsPackage).Assembly.Location);
            var launcherExe = Path.Combine(path, "SolutionExtensions.Launcher.merged.exe");
            //warn about references, so it is merged to one exe
#if DEBUG
            //for debugging purposes, it can be called directly to just compiled version
            // cannot test existence of some cfg file, because build deletes all
            var alt = @"D:\GitHub\SolutionEx\SolutionExtensions.Launcher\bin\Debug\SolutionExtensions.Launcher.exe";
            if (File.Exists(alt))
                launcherExe = alt;
#endif
            return launcherExe;
        }

        private static EnvDTE.Process FindDTEProcess(Debugger5 dbg, int id)
        {
            EnvDTE.Process findProcess(IEnumerable<EnvDTE.Process> list)
            {
                return list.FirstOrDefault(p => p.ProcessID == id);
            }
            ThreadHelper.ThrowIfNotOnUIThread();
            var process = findProcess(dbg.LocalProcesses.Cast<EnvDTE.Process>());
            if (process != null)
                return process;
            var t = dbg.Transports.Item(1);
            process = findProcess(dbg.GetProcesses(t, null).Cast<EnvDTE.Process>());
            return process;
        }

        private static string GetPackageId(DTE dte, SolutionExtensionsPackage package)
        {
            var id = package.GetType().GUID.ToString("B");
            return id;
        }

        private static string GetMonikerName(DTE dte, System.Diagnostics.Process process = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (process == null) process = System.Diagnostics.Process.GetCurrentProcess();
            return $"VisualStudio.DTE.{dte.Version}:{process.Id}";
        }

        public static bool ValidateBreakpoint(ExtensionItem item, SolutionExtensionsPackage package, ExtensionManager extensionManager)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = package.GetService<DTE, DTE>();
            var methodName = item.IsGenerator ? "Generate" : "Run";
            var fn = $"{item.ClassName}.{methodName}";
            //* FunctionName="ExtensionSamples.Sample1.Run(DTE dte, IServiceProvider package)" 
#pragma warning disable VSTHRD010 
            return dte.Debugger.Breakpoints
                .OfType<Breakpoint>().Any(b => b.FunctionName.StartsWith(fn));
#pragma warning restore VSTHRD010 
            // TODO: somehow instruct debugger to break in method
        }
    }
}