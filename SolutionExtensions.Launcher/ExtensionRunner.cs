using System.Diagnostics;

namespace SolutionExtensions.Launcher
{
    /// <summary>
    /// this class is here to simplify code, because it will break and will be visible
    /// </summary>
    public class ExtensionRunner
    {
        private readonly ExtensionRI ri;
        private readonly EnvDTE.DTE dte;
        private readonly object package;
        public bool BreakDebugger { get; set; }
        //Run
        public string RunArgument { get; set; }
        //Generator
        public string GeneratorFilePath { get; set; }
        public string GeneratorDefaultNamespace { get; set; }
        public string GeneratorContent { get; set; }
        public string GeneratorResult { get; set; }
        public string GeneratorExtension { get; set; }

        public ExtensionRunner(ExtensionRI ri, EnvDTE.DTE dte, object package)
        {
            this.ri = ri;
            this.dte = dte;
            this.package = package;
        }

        public void Run()
        {
            RunCall(dte, package);
        }
        private void RunCall(EnvDTE.DTE dte, object package)
        {
            if (BreakDebugger)
                Debugger.Break();
            ExtensionObject.Run(ri, dte, package, RunArgument);
        }
        public void Generate()
        {
            GenerateCall(dte, package);
        }
        private void GenerateCall(EnvDTE.DTE dte, object package)
        {
            if (BreakDebugger)
                Debugger.Break();
            GeneratorResult = ExtensionObject.Generate(ri, dte, GeneratorContent, GeneratorFilePath, GeneratorDefaultNamespace, out var ext);
            GeneratorExtension = ext;
        }
    }
}