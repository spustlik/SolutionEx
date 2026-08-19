using Microsoft.VisualStudio.Shell.Interop;
using SolutionExtensions.Reflector;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace SolutionExtensions.Launcher
{
    public partial class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            var ver = typeof(Program).Assembly.GetName().Version;
            var cfg = typeof(Program).Assembly.IsAssemblyDebugBuild() ? "debug" : "release";
            Log($"Extension launcher v{ver}, ({cfg})");
            if (args.Length < 4)
            {
                Console.WriteLine($"Needs at least 4 arguments");
                return;
            }
            try
            {
                var cmdLine = Arguments.Parse(args);
                switch (cmdLine.Action)
                {
                    case ActionEnum.Run: Run(cmdLine); break;
                    case ActionEnum.Generate: Generate(cmdLine); break;
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

        private static void Generate(Arguments cmd)
        {
            ExtensionRI ri = PrepareRI(cmd, x => x.GenerateMethod);
            WaitForDebugger(cmd);
            var dte = GetDTE(cmd);
            object package = GetPackage(dte, cmd);
            Console.WriteLine($"Loading generator file");
            var runner = new ExtensionRunner(ri, dte, package).Assign(cmd);
            var fn = cmd.GeneratorFilePath;
            if (!File.Exists(fn))
                throw new Exception($"Generated file doesn't exist: {fn}");
            var c = File.ReadAllText(fn);
            runner.GeneratorContent = c;
            Console.WriteLine($"{LauncherProcess.RUN}: Running generator");
            runner.Generate();
            Console.WriteLine($"{LauncherProcess.DONE}");
            Console.WriteLine($"{LauncherProcess.RESULT_EXT}:{runner.GeneratorExtension}");
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()+".bin");
            File.WriteAllText(tempFile, runner.GeneratorResult);
            Console.WriteLine($"{LauncherProcess.RESULT_FILE}:{tempFile}");
        }


        private static void Run(Arguments cmd)
        {
            ExtensionRI ri = PrepareRI(cmd, x => x.RunMethod);
            WaitForDebugger(cmd);
            var dte = GetDTE(cmd);
            object package = GetPackage(dte, cmd);
            //run extension
            Console.WriteLine($"{LauncherProcess.RUN}: Running extension");
            //to simplify code, which will break
            var runner = new ExtensionRunner(ri, dte, package).Assign(cmd);
            runner.Run();
            Console.WriteLine($"{LauncherProcess.DONE}");
        }

        private static object GetPackage(EnvDTE.DTE dte, Arguments cmd)
        {
            object package = null;
            try
            {
                //package = FindPackage(cmd.PackageId, dte);
                //if (package == null)
                {
                    //Console.WriteLine("WARNING: Package not get from DTE, trying serviceProvider");
                    package = GetServiceProvider(cmd.PackageId, dte);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WARNING: {ex.Message}");
            }

            return package;
        }

        private static void WaitForDebugger(Arguments cmd)
        {
            if (!cmd.WaitForDebugger)
                return;
            //wait for debugger attach
            var timeOut = DateTime.Now.AddMinutes(1);
            Console.WriteLine($"{LauncherProcess.WAIT}: Waiting for debugger to attach");
            while (!Debugger.IsAttached)
            {
                Thread.Sleep(100);
                if (DateTime.Now > timeOut)
                    throw new ApplicationException($"Waiting for debugger timeout");
            }
        }

        private static ExtensionRI PrepareRI(Arguments cmd, Func<ExtensionRI, MethodInfo> getMethod)
        {
            Log($"Arguments: dllPath={cmd.DllPath},className={cmd.ClassName},moniker={cmd.MonikerName},package={cmd.PackageId}");
            if (!File.Exists(cmd.DllPath))
                throw new ApplicationException($"File doesn't exists: {cmd.DllPath}");
            var assembly = Assembly.LoadFrom(cmd.DllPath);
            var ri = new ExtensionRI(assembly, cmd.ClassName) { ThrowIfNotFound = true };
            var method = getMethod(ri);
            var par = method.GetParameters().Select(p => p.Name + ": " + GetTypeStr(p.ParameterType)).ToArray();
            Log($"{ri.Type.FullName}.{method.Name}({String.Join(", ", par)}) found");
            return ri;
        }

        private static EnvDTE.DTE GetDTE(Arguments cmd)
        {
            //instantiate dte from moniker
            Console.WriteLine($"{LauncherProcess.PREPARE}: Getting running DTE from ${cmd.MonikerName}");
            var rot = new RunningComObjects();
            var dteCom = rot.GetRunningComObject(cmd.MonikerName);
            if (dteCom == null)
                throw new ApplicationException($"DTE COM is not running");
            var dte = dteCom as EnvDTE.DTE;
            if (dte == null)
                throw new ApplicationException($"Moniker COM is not DTE");
            //wait for ready
            var timeOut = DateTime.Now.AddMinutes(1);
            Console.WriteLine($"{LauncherProcess.WAIT}: Waiting for DTE to be ready");
            while (!TryGetSolution(dte))
            {
                Thread.Sleep(100);
                if (DateTime.Now > timeOut)
                    throw new ApplicationException($"Waiting for DTE timeout");
            }
            return dte;
        }

        private static bool TryGetSolution(EnvDTE.DTE dte)
        {
            try
            {
                var s = dte.Solution;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static IServiceProvider GetServiceProvider(string id, EnvDTE.DTE dte)
        {

            var svc = dte.GetOLEServiceProvider(throwIfNotFound: true);
            var sp = new ServiceProviderOle(svc);
            return sp;
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
