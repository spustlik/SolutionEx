using System;
using System.Diagnostics;
using System.Reflection;

namespace SolutionExtensions.Launcher
{
    public class ExtensionRunner
    {
        private readonly Type type;
        private readonly MethodInfo method;
        private readonly EnvDTE.DTE dte;
        private readonly object package;
        private readonly string argument;
        private readonly bool breakDebugger;

        public ExtensionRunner(Type type, MethodInfo method, EnvDTE.DTE dte, object package, string argument, bool breakDebugger)
        {
            this.type = type;
            this.method = method;
            this.dte = dte;
            this.package = package;
            this.argument = argument;
            this.breakDebugger = breakDebugger;
        }

        public void Run()
        {
            RunMethod(dte, package);
        }
        private void RunMethod(EnvDTE.DTE dte, object package)
        {
            if (breakDebugger)
                Debugger.Break();
            ExtensionObject.RunExtension(type, method, dte, package, argument);
        }
    }
}