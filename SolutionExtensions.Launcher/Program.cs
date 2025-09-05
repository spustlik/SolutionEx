using SolutionExtensions.Runner;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

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
                Run(args);
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

        private static void Run(string[] args)
        {
            //args:
            //dll path
            //class name
            //moniker name
            //package id to find it in dte
            var dllPath = args[0];
            var className = args[1];
            var monikerName = args[2];
            var packageId = args[3];
            args = args.Skip(4).ToArray();
            var switches = args
                .Where(x => x.StartsWith("/"))
                .Select(x => x.TrimStart('/').Trim())
                .Select(x => x.Split(new[] { ':' }, 2))
                .ToDictionary(parts => parts[0].Trim().ToUpperInvariant(), parts => parts.Skip(1));
            var waitForDebugger = switches.ContainsKey("WAITFORDEBUGGER");
            //more params like debug:yes/no, copydll:y/n
            var breakDebugger = switches.ContainsKey("BREAK");

            Log($"Arguments: dllPath={dllPath},className={className},moniker={monikerName},package={packageId}");
            if (!File.Exists(dllPath))
                throw new ApplicationException($"File doesnt exists: {dllPath}");
            var assembly = Assembly.Load(dllPath);
            var (method, type) = ExtensionObject.FindExtensionMethod(assembly, className, true);
            var par = method.GetParameters().Select(p => p.Name + ": " + GetTypeStr(p.ParameterType)).ToArray();
            Log($"{type.FullName}.{method.Name}({String.Join(", ", par)}) found");

            //wait for debugger attach
            if (waitForDebugger)
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
            Log($"Getting running dte from ${monikerName}");
            var dteCom = RunningComObjects.GetRunningComObject(monikerName);
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
            if (breakDebugger)
                Debugger.Break();
            ExtensionObject.RunExtension(type, method, dte, package);
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
