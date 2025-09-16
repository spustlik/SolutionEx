using EnvDTE;
using EnvDTE100;
using Microsoft.VisualStudio.Shell;
using SolutionExtensions.Launcher;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SolutionExtensions
{
    public class ExtensionDebugger
    {
        public static void RunExtension(ExtensionItem item, SolutionExtensionsPackage package, ExtensionManager extensionManager)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            //do not copy!
            var dllPath = extensionManager.GetRealPath(item.DllPath);
            var dte = package.GetService<DTE, DTE>();
            var monikerName = GetMonikerName(dte);
            var packageId = GetPackageId(dte, package);

            string launcherExe = GetLauncherExe(dte, package);

            var output = new System.Text.StringBuilder();
            bool waiting = false;
            void launcherOutputData(string line)
            {
                if (string.IsNullOrEmpty(line))
                    return;
                if (!line.Contains("\n"))
                    line += "\n";
                output.Append(line);
                if (line.StartsWith(LauncherProcess.WAIT))
                    waiting = true;
                package.AddToOutputPaneThreadSafe(line);
            }
            var launcher = LauncherProcess.RunExtension(launcherExe, dllPath, item.ClassName, monikerName, packageId, launcherOutputData);

            while (!launcher.HasExited)
            {
                if (waiting) break;
                launcher.WaitForExit(100);
            }

            if (launcher.HasExited)
                throw new Exception($"Launcher exited with {launcher.ExitCode}\n{output}");

            var dbg = dte.Debugger as Debugger5;
            var p = FindDTEProcess(dbg, launcher.Id);
            if (p == null)
                throw new Exception($"Cannot find runner process {launcher.Id}");
            p.Attach();
            //caller must verify than there is an breakpoint
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
            Process findProcess(IEnumerable<Process> list)
            {
                return list.FirstOrDefault(p => p.ProcessID == id);
            }
            ThreadHelper.ThrowIfNotOnUIThread();
            var process = findProcess(dbg.LocalProcesses.Cast<Process>());
            if (process != null)
                return process;
            var t = dbg.Transports.Item(1);
            process = findProcess(dbg.GetProcesses(t, null).Cast<Process>());
            return process;
        }

        private static string GetPackageId(DTE dte, SolutionExtensionsPackage package)
        {
            var id = package.GetType().GUID.ToString("B");

            //not working anything access to share object to another process
            // so launcher is creating serviceprovider accessing DTE services

            //var prop = dte.Solution.Properties.Item(packageId);
            //prop.Value = package;
            //var sp = package as IServiceProvider;
            //var svc = new ServiceProiderObj(svcType => sp.GetService(svcType));
            //var rot = new RunningComObjects();
            //rot.RegisterComObject(svc, id + "_Moniker");
            /*
            dte.Globals[id] = package;
            dte.Globals[id + "_ServiceProvider"] = svc;
            */
            //dte.Globals.VariablePersists[id] = true;
            //not working properly - result is COM on client side, but not assignable to IServiceProvider
            //see also Program.cs GetPackage
            return id;
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

        public static bool ValidateBreakpoint(ExtensionItem item, SolutionExtensionsPackage package, ExtensionManager extensionManager)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = package.GetService<DTE, DTE>();
            var fn = item.ClassName + ".Run";
            //* FunctionName="ExtensionSamples.Sample1.Run(DTE dte, IServiceProvider package)" 
            return dte.Debugger.Breakpoints.OfType<Breakpoint>().Any(b => b.FunctionName.StartsWith(fn));
            // TODO: somehow instruct debugger to break in method
            //this is not enough - resolving debugger breakpoint
            //var function = type.FullName + "." + method.Name;
            //dte.Debugger.Breakpoints.Add(Function: function);
            /*<Breakpoint 
             * Name="Sample1.cs, line 17 character 13" 
             * Language="C#" 
             * File="D:\GitHub\SolutionEx\SampleSol\ClassLibrary1\ExtensionSamples\Sample1.cs" 
             * FileLine="17" 
             * FunctionName="ExtensionSamples.Sample1.Run(DTE dte, IServiceProvider package)" 
             * FunctionLineOffset="3" 
             * FunctionColumnOffset="1" 
             * LocationType="dbgBreakpointLocationTypeFile" 
             * Type="dbgBreakpointTypePending" />
             * */
        }
    }
}