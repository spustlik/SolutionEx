using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

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
        private readonly int retryCount;

        public ExtensionRunner(Type type, MethodInfo method, EnvDTE.DTE dte, object package, string argument, bool breakDebugger, int retryCount)
        {
            this.type = type;
            this.method = method;
            this.dte = dte;
            this.package = package;
            this.argument = argument;
            this.breakDebugger = breakDebugger;
            this.retryCount = retryCount;
        }

        public void Run()
        {
            int count = retryCount;
            while (count >= 0)
            {
                count--;
                if (TryRunMethod())
                    break;
            }
        }

        private bool TryRunMethod()
        {
            try
            {
                RunMethod(dte, package);
                return true;
            }
            catch (ExternalException ex)
            {
                const uint RPC_E_SERVERCALL_RETRYLATER = 0x8001010A;
                if ((uint)ex.HResult == RPC_E_SERVERCALL_RETRYLATER)
                    return false;
                throw;
            }
        }

        private void RunMethod(EnvDTE.DTE dte, object package)
        {
            if (breakDebugger)
                Debugger.Break();
            ExtensionObject.RunExtension(type, method, dte, package, argument);
        }
    }
}