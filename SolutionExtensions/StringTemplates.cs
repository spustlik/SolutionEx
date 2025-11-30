using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SolutionExtensions
{
    public class StringTemplates
    {

        private static Regex _varRegex = new Regex(@"\$\((?<var>[^\)]*)\)", RegexOptions.Compiled);
        /// <summary>
        /// replaces $(var) and %ENV_VAR% in string
        /// $(var) value is calling getUpperCasedVariableValue with uppercased variable name
        /// %ENV_VAR% is using environment variables
        /// </summary>
        public static string ExpandString(string s, Func<string, string> getUpperCasedVariableValue, bool envVariables = false)
        {
            if (String.IsNullOrEmpty(s))
                return s;
            if (envVariables)
                s = Environment.ExpandEnvironmentVariables(s);
            s = _varRegex.Replace(s, (m) =>
            {
                var var = m.Groups["var"].Value;
                if (!string.IsNullOrEmpty(var))
                    return getUpperCasedVariableValue(var.Trim().ToUpperInvariant());
                return m.Value;
            });
            return s;
        }
        public static string ExpandString(string s, Dictionary<string, string> variables, bool envVariables = false)
        {
            var variablesCaseInsensitive = new Dictionary<string, string>(variables, StringComparer.InvariantCultureIgnoreCase);
            return ExpandString(s, (name) => variablesCaseInsensitive.TryGetValue(name, out var value) ? value : null, envVariables);
        }

        public static string GetExtensionCsharp(string _namespace, string _className, string nuget)
        {
            return ExpandString(ExtensionCsharp, new Dictionary<string, string>() {
                { "namespace",  _namespace},
                { "className", _className},
                { "nuget", nuget},
            });
        }

        public static string Nuget_VS = @"Microsoft.VisualStudio.Interop";
        public static string ExtensionCsharp = @"using EnvDTE;
using System;
using System.Windows.Forms;

//TODO: add nuget package to your project: $(nuget)

namespace $(namespace)
{
    public class $(className)
    {
        /// <summary>
        /// Runs your code 
        /// </summary>
        /// <remarks>
        /// change type of package to Microsoft.VisualStudio.Shell.Package, if you want to reference another libraries
        /// </remarks>
        /// <param name=""dte"">Reference to VS DTE, you can change it to EnvDTE80.DTE2</param>
        /// <param name=""package"">Reference to executing package, you can change it to AsyncPackage,IAsyncServiceProvider</param>
        public void Run(DTE dte, IServiceProvider package)
        {
            MessageBox.Show(""Hello from $(className)"");
        }
    }
}";

    }
}
