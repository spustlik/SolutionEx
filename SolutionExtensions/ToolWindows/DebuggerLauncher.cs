using EnvDTE;
using EnvDTE100;
using Microsoft.VisualStudio.Shell;
using SolutionExtensions.Launcher;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms.Design.Behavior;

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
            var dbg = dte.Debugger as Debugger5;
            var p = AttachDebuggerToProcess(dbg, launcher);
            if (p == null || dbg.CurrentProcess == null)
                throw new Exception($"Cannot attach to runner process");
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

        private static EnvDTE.Process AttachDebuggerToProcess(Debugger5 dbg, System.Diagnostics.Process launcher)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (EnvDTE.Process p in dbg.LocalProcesses)
            {
                if (p.ProcessID == launcher.Id)
                {
                    p.Attach();
                    return p;
                }
            }
            return null;
        }

        private static string GetPackageId(DTE dte, SolutionExtensionsPackage package)
        {
            throw new NotImplementedException();
        }

        private static string GetMonikerName(DTE dte)
        {
            throw new NotImplementedException();
        }
    }
}