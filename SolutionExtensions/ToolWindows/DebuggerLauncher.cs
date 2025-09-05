using EnvDTE;
using EnvDTE100;
using Microsoft.VisualStudio.Shell;
using SolutionExtensions.Launcher;
using System;

namespace SolutionExtensions.ToolWindows
{
    internal class DebuggerLauncher
    {
        internal static void RunExtension(ExtensionItem item, SolutionExtensionsPackage package, ExtensionManager extensionManager)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            //do not copy!
            var dllPath = extensionManager.GetRealPath(item.DllPath);
            var dte = package.GetService<DTE, DTE>();
            var monikerName = GetMonikerName(dte);
            var packageId = GetPackageId(dte, package);
            var launcher = LauncherProcess.RunExtension(dllPath, item.ClassName, monikerName, packageId);
            launcher.WaitForExit(100);
            if (launcher.HasExited)
            {
                var output = launcher.StandardOutput.ReadToEnd();
                throw new Exception($"Launcher exited with {launcher.ExitCode}\n{output}");
            }
            
            var dbg = dte.Debugger as Debugger5;
            var p = FindDTEProcess(dbg, launcher);
            if (p == null || dbg.CurrentProcess == null)
                throw new Exception($"Cannot find runner process");
            p.Attach();
            //dbg.SetNextStatement();
            //dbg.CurrentStackFrame
            //dbg.StepOver();
            //dbg.Breakpoints.Add();
            //string Function = "", 
            //string File = "", 
            //    int Line = 1, 
            //    int Column = 1, 
            //    string Language = "", 
            //    string Data = "", 
            //    int DataCount = 1, 
            //    string Address = "", 
            //    );
            //dbg.Break();
        }

        private static EnvDTE.Process FindDTEProcess(Debugger5 dbg, System.Diagnostics.Process launcher)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (EnvDTE.Process p in dbg.LocalProcesses)
            {
                if (p.ProcessID == launcher.Id)
                    return p;
            }
            return null;
        }

        private static string GetPackageId(DTE dte, SolutionExtensionsPackage package)
        {
            return package.GetType().GUID.ToString("B");
        }

        private static string GetMonikerName(DTE dte, System.Diagnostics.Process process = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (process == null) process = System.Diagnostics.Process.GetCurrentProcess();
            //VisualStudio.DTE.17.0:25176
            //var comType = dte.GetType();//System.__ComObject
            //string progId1 = comType.InvokeMember("ProgID", System.Reflection.BindingFlags.GetProperty, null, dte, null) as string;
            //string clsid = comType.GUID.ToString();//0000
            //string progId2 = dte.GetType().ToString(); // returns "System.__ComObject"
            //string progId3 = System.Runtime.InteropServices.Marshal.GenerateProgIdForType(dte.GetType()); // throws error
            return $"VisualStudio.DTE.{dte.Version}:{process.Id}";
        }
    }
}