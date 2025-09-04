using EnvDTE;
using System;

namespace ExtensionSamples
{
    public static class Sample2
    {
        public static void Run(DTE dte, IServiceProvider package)
        {
            //method can be also static
            if (dte.Solution == null)
                throw new Exception($"No solution");
            var csharp1 = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";
            var csharp2 = "{FAE04EC1-301F-11D3-BF4B-00C04F79EFBC}";

            //dte.Solution.TemplatePath[EnvDTE.Constants.type(csharp);
            //var p1 = dte.Solution.ProjectItemsTemplatePath(csharp1);
            //var p2 = dte.Solution.ProjectItemsTemplatePath(csharp2);
            var p3 = dte.Solution.TemplatePath[csharp1];
            var p4 = dte.Solution.TemplatePath[csharp2];
            
        }
    }
}
