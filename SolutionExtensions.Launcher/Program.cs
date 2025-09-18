using Microsoft.VisualStudio.Shell.Interop;
using SolutionExtensions.Reflector;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace SolutionExtensions.Launcher
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            var ver = typeof(Program).Assembly.GetName().Version;
            var cfg = typeof(Program).Assembly.IsAssemblyDebugBuild() ? "debug" : "release";
            Log($"Extension launcher v{ver}, ({cfg})");
            if (args.Length < 4)
            {
                Console.WriteLine($"Needs 4 arguments");
                return;
            }
            try
            {
                var cmdLine = ParseArgs(args);
                switch (cmdLine.Action)
                {
                    case ActionEnum.Run: Run(cmdLine); break;
                    case ActionEnum.DumpMonikers: DumpMonikers(cmdLine); break;
                    default:
                        Console.WriteLine($"Unknown arguments");
                        break;
                }
                if (cmdLine.WaitForEnter)
                {
                    Console.WriteLine($"Press <ENTER>");
                    Console.ReadLine();
                }
            }
            catch (ApplicationException aex)
            {
                Console.WriteLine($"{LauncherProcess.ERROR}: {aex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{LauncherProcess.ERROR}: {ex.Message}");
                Console.WriteLine(ex);
            }
        }

        private static void DumpMonikers(Arguments cmd)
        {
            var rot = new RunningComObjects();
            var names = rot.GetRunningMonikers()
                .Select(m => RunningComObjects.GetMonikerDisplayName(m))
                .OrderBy(x => x)
                .ToArray();
            foreach (var m in names)
            {
                Log(m);
            }
        }

        private static void Run(Arguments cmd)
        {
            Log($"Arguments: dllPath={cmd.DllPath},className={cmd.ClassName},moniker={cmd.MonikerName},package={cmd.PackageId}");
            if (!File.Exists(cmd.DllPath))
                throw new ApplicationException($"File doesn't exists: {cmd.DllPath}");
            var assembly = Assembly.LoadFrom(cmd.DllPath);
            var (method, type) = ExtensionObject.FindExtensionMethod(assembly, cmd.ClassName, true);
            var par = method.GetParameters().Select(p => p.Name + ": " + GetTypeStr(p.ParameterType)).ToArray();
            Log($"{type.FullName}.{method.Name}({String.Join(", ", par)}) found");

            //wait for debugger attach
            if (cmd.WaitForDebugger)
            {
                var timeOut = DateTime.Now.AddMinutes(1);
                Console.WriteLine($"{LauncherProcess.WAIT}: Waiting for debugger to attach");
                while (!Debugger.IsAttached)
                {
                    Thread.Sleep(100);
                    if (DateTime.Now > timeOut)
                        throw new ApplicationException($"Waiting for debugger timeout");
                }
            }
            //instantiate dte from moniker
            Console.WriteLine($"{LauncherProcess.PREPARE}: Getting running DTE from ${cmd.MonikerName}");
            var dte = GetDTE(cmd);
            IServiceProvider serviceProvider = null;
            try
            {
                serviceProvider = GetServiceProvider(cmd.PackageId, dte);
            }
            catch (Exception ex)
            {
                Console.WriteLine("WARNING: " + ex.Message);
            }
            //run extension
            Console.WriteLine($"{LauncherProcess.RUN}: Running extension");
            //to simplify code, which will break
            var runner = new ExtensionRunner(type, method, cmd.BreakDebugger);
            runner.Run(dte, serviceProvider);
            Console.WriteLine($"{LauncherProcess.DONE}");
        }

        private static EnvDTE.DTE GetDTE(Arguments cmd)
        {
            //Marshal.GetActiveObject() //needs progid, not moniker
            var rot = new RunningComObjects();
            var dteCom = rot.GetRunningComObject(cmd.MonikerName);
            if (dteCom == null)
                throw new ApplicationException($"DTE COM is not running");
            var dte = dteCom as EnvDTE.DTE;
            if (dte == null)
                throw new ApplicationException($"Moniker COM is not DTE");
            return dte;
        }

        private static IServiceProvider GetServiceProvider(string id, EnvDTE.DTE dte)
        {
            var svc = dte.GetOLEServiceProvider(throwIfNotFound: true);
            var sp = new ServiceProviderDelegate((type =>
            {
                return svc.QueryService(type.GUID);
            }));
            return sp;
            /*
            var shell = svc.QueryService<SVsShell>() as IVsShell;
            if (shell == null) throw new Exception($"Cannot get VSShell from DTE");
            var packages = shell.GetPackages().ToArray();
            var guid = new Guid(id);

            //not working, all objects are COM
            //var found = packages.FirstOrDefault(p => p.GetType().GUID == guid);
            //not working either
            var found = packages.Select(x => ReflectionCOM.QueryInterface(x, guid)).FirstOrDefault(x => x != null);               
            if (found == null) throw new Exception($"Cannot find package {guid}");
            var sp = found as IServiceProvider;
            if (sp == null) throw new Exception($"Package is not IServiceProvider");
            return sp;
            */
        }

        enum ActionEnum
        {
            Help,
            Run,
            DumpMonikers
        }
        class Arguments
        {
            public ActionEnum Action = ActionEnum.Help;
            public string DllPath;
            public string ClassName;
            public string MonikerName;
            public string PackageId;
            public bool WaitForDebugger;
            public bool BreakDebugger;
            public bool WaitForEnter;
        }
        private static Arguments ParseArgs(string[] args)
        {
            var switches = args
                .Where(x => x.StartsWith("/"))
                .Select(x => x.TrimStart('/').Trim())
                .Select(x => x.Split(new[] { ':' }, 2))
                .ToDictionary(parts => parts[0].Trim().ToUpperInvariant(), parts => parts.Skip(1));
            var strings = args.Where(x => !x.StartsWith("/")).ToArray();

            var r = new Arguments();
            r.WaitForDebugger = switches.ContainsKey("WAITFORDEBUGGER");
            r.BreakDebugger = switches.ContainsKey("BREAK");
            r.WaitForEnter = switches.ContainsKey("WAITFORENTER");
            if (switches.ContainsKey("DUMPMONIKERS"))
            {
                r.Action = ActionEnum.DumpMonikers;
            }
            if (strings.Length == 4)
            {
                r.DllPath = strings[0];
                r.ClassName = strings[1];
                r.MonikerName = strings[2];
                r.PackageId = strings[3];
                r.Action = ActionEnum.Run;
            }
            return r;
        }

        private static string GetTypeStr(Type t)
        {
            return t.IsValueType ? t.Name : t.FullName;
        }

        private static void Log(string s)
        {
            Console.WriteLine($"{LauncherProcess.LOG}:{s}");
        }
    }
}
