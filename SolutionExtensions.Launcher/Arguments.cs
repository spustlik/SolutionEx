using System.Linq;

namespace SolutionExtensions.Launcher
{
    public enum ActionEnum
    {
        Help,
        Run,
        Generate,
        DumpMonikers
    }

    public class Arguments
    {
        public ActionEnum Action = ActionEnum.Help;
        public string DllPath;
        public string ClassName;
        public string MonikerName;
        public string PackageId;
        public string Argument;
        public bool WaitForDebugger;
        public bool BreakDebugger;
        public bool WaitForEnter;
        public string GeneratorFilePath;
        public string GeneratorDefaultNamespace;

        public static Arguments Parse(string[] args)
        {
            var switches = args
                .Where(x => x.StartsWith("/"))
                .Select(x => x.TrimStart('/').Trim())
                .Select(x => x.Split(new[] { ':' }, 2))
                .ToDictionary(parts => parts[0].Trim().ToUpperInvariant(), parts => parts.Skip(1));
            var strings = args.Where(x => !x.StartsWith("/")).ToArray();

            var r = new Arguments();
            r.WaitForDebugger = switches.ContainsKey("WAITFORDEBUGGER");
            r.BreakDebugger = switches.ContainsKey("BREAK");
            r.WaitForEnter = switches.ContainsKey("WAITFORENTER");
            if (switches.ContainsKey("DUMPMONIKERS"))
            {
                r.Action = ActionEnum.DumpMonikers;
            }
            if (strings.Length >= 4)
            {
                r.DllPath = strings[0];
                r.ClassName = strings[1];
                r.MonikerName = strings[2];
                r.PackageId = strings[3];
                r.Action = ActionEnum.Run;
            }
            if (switches.ContainsKey("GENERATE"))
            {
                r.Action = ActionEnum.Generate;
                if (strings.Length > 4)
                    r.GeneratorFilePath = strings[4];
                if (strings.Length > 5)
                    r.GeneratorDefaultNamespace = strings[5];
            }
            if (strings.Length > 4 && r.Action == ActionEnum.Run)
            {
                r.Argument = strings[4];
            }
            return r;
        }

    }

    public static class ExtensionRunnerExtensions
    {
        public static ExtensionRunner Assign(this ExtensionRunner runner, Arguments cmd)
        {
            runner.BreakDebugger = cmd.BreakDebugger;

            if (cmd.Action == ActionEnum.Run)
            {
                runner.RunArgument = cmd.Argument;
            }
            if (cmd.Action == ActionEnum.Generate)
            {
                runner.GeneratorFilePath = cmd.GeneratorFilePath;
                runner.GeneratorDefaultNamespace = cmd.GeneratorDefaultNamespace;
            }
            return runner;
        }
    }
}

