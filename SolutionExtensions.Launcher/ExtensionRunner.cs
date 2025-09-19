using System;
using System.Diagnostics;
using System.Reflection;

namespace SolutionExtensions.Launcher
{
    public class ExtensionRunner
    {
        private Type type;
        private MethodInfo method;
        private readonly string argument;
        private readonly bool breakDebugger;

        public ExtensionRunner(Type type, MethodInfo method, string argument, bool breakDebugger)
        {
            this.type = type;
            this.method = method;
            this.argument = argument;
            this.breakDebugger = breakDebugger;
        }

        public void Run(EnvDTE.DTE dte, object package)
        {
            if (breakDebugger)
                Debugger.Break();
            ExtensionObject.RunExtension(type, method, dte, package, argument);
        }
    }
}