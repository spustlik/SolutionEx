using System;
using System.Diagnostics;
using System.Reflection;

namespace SolutionExtensions.Launcher
{
    public class ExtensionRunner
    {
        private readonly ExtensionRI ri;
        private readonly EnvDTE.DTE dte;
        private readonly object package;
        private readonly string argument;
        private readonly bool breakDebugger;

        public ExtensionRunner(ExtensionRI ri, EnvDTE.DTE dte, object package, string argument, bool breakDebugger)
        {
            this.ri = ri;
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
            ExtensionObject.RunExtension(ri, dte, package, argument);
        }
    }
}