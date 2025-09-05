using System;
using System.Diagnostics;
using System.Reflection;

namespace SolutionExtensions.Launcher
{
    public class ExtensionRunner
    {
        private Type type;
        private MethodInfo method;
        private readonly bool breakDebugger;

        public ExtensionRunner(Type type, MethodInfo method, bool breakDebugger)
        {
            this.type = type;
            this.method = method;
            this.breakDebugger = breakDebugger;
        }

        public void Run(EnvDTE.DTE dte, IServiceProvider package)
        {
            if (breakDebugger)
                Debugger.Break();
            ExtensionObject.RunExtension(type, method, dte, package);
        }
    }
}