using SolutionExtensions.Runner;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml.Linq;

namespace SolutionExtensions.Launcher
{
    public class Program
    {
        static void Main(string[] args)
        {
            Log($"Extension launcher");
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
                Console.WriteLine("ERROR: " + aex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.Message);
                Console.WriteLine(ex);
            }
        }

        private static void DumpMonikers(Arguments cmd)
        {
            var rot = RunningComObjects.GetROT();
            var names = RunningComObjects.EnumerateRunning(rot).Select(m => m.GetMonikerDisplayName()).OrderBy(x => x).ToArray();
            foreach (var m in names)
            {
                Log(m);
            }
        }

        private static void Run(Arguments cmd)
        {
            Log($"Arguments: dllPath={cmd.DllPath},className={cmd.ClassName},moniker={cmd.MonikerName},package={cmd.PackageId}");
            if (!File.Exists(cmd.DllPath))
                throw new ApplicationException($"File doesnt exists: {cmd.DllPath}");
            var assembly = Assembly.LoadFrom(cmd.DllPath);
            var (method, type) = ExtensionObject.FindExtensionMethod(assembly, cmd.ClassName, true);
            var par = method.GetParameters().Select(p => p.Name + ": " + GetTypeStr(p.ParameterType)).ToArray();
            Log($"{type.FullName}.{method.Name}({String.Join(", ", par)}) found");

            //wait for debugger attach
            if (cmd.WaitForDebugger)
            {
                var timeOut = DateTime.Now.AddMinutes(1);
                Log($"Waiting for debugger to attach");
                while (!Debugger.IsAttached)
                {
                    Thread.Sleep(100);
                    if (DateTime.Now > timeOut)
                        throw new ApplicationException($"Waiting for debugger timeout");
                }
            }
            //instantiate dte from moniker
            Log($"Getting running dte from ${cmd.MonikerName}");
            var dteCom = RunningComObjects.GetRunningComObject(cmd.MonikerName);
            if (dteCom == null)
                throw new ApplicationException($"DTE COM is not running");
            var dte = dteCom as EnvDTE.DTE;
            if (dte == null)
                throw new ApplicationException($"Moniker COM is not DTE");
            // TODO: try to find for package
            var package = dte as IServiceProvider;
            //run extension
            Log("Running extension");
            // TODO: somehow instruct debugger to break in method
            if (cmd.BreakDebugger)
                Debugger.Break();
            ExtensionObject.RunExtension(type, method, dte, package);
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
            Console.WriteLine("LOG:" + s);
        }
    }
}
