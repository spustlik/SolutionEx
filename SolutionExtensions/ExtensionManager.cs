using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using SolutionExtensions.Extensions;
using SolutionExtensions.Model;
using SolutionExtensions.Reflector;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SolutionExtensions
{
    public class ExtensionManager
    {
        public const string SELFKEY = "SELF";
        public const string SELF = "$(" + SELFKEY + ")";

        private SolutionExtensionsPackage package;

        public ExtensionManager(SolutionExtensionsPackage package)
        {
            this.package = package;
        }

        public void SyncToDte(ExtensionsModel model)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = package.GetService<DTE, DTE>();
            if (dte.Solution == null)
                return;
            var cmdSvc = (package as IServiceProvider).GetService(typeof(IMenuCommandService)) as OleMenuCommandService;

            var extCommands = GetExtCommands(dte);
            for (int i = 0; i < extCommands.Count; i++)
            {
                var cmd = extCommands[i];
                var oleCmd = cmd as OleMenuCommand ?? cmdSvc?.FindCommand(cmd.GetCommandID()) as OleMenuCommand;
                SyncItem(model, cmd, oleCmd);
            }
        }

        private void SyncItem(ExtensionsModel model, Command cmd, OleMenuCommand oleCmd)
        {
            var item = FindItemByCmd(model, cmd);
            if (item != null)
            {
                cmd.AddShortCutToCommand(item.ShortCutKey);
                if (oleCmd != null)
                {
                    oleCmd.Text = item.Title;
                    oleCmd.Visible = true;
                }
            }
            else
            {
                if (oleCmd != null)
                {
                    oleCmd.Visible = false;
                }
            }
        }

        private ExtensionItem FindItemByCmd(ExtensionsModel model, Command cmd)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var i = cmd.ID - CommandIds.Command_Extension1;
            if (i < 0 || i >= model.Extensions.Count)
                return null;
            return model.Extensions[i];
        }

        private List<Command> GetExtCommands(DTE dte)
        {
            var guid = CommandIds.CommandSetGuid.ToString("B").ToUpper();
            ThreadHelper.ThrowIfNotOnUIThread();
            var result = new List<Command>();
            foreach (Command cmd in dte.Commands)
            {
                if (cmd.ID < CommandIds.Command_Extension1 || String.Compare(cmd.Guid, guid, ignoreCase: true) != 0)
                    continue;
                result.Add(cmd);
            }
            return result;
        }

        /// <summary>
        /// loads cfg file
        /// </summary>
        /// <returns>true if loaded</returns>
        public bool LoadFile(ExtensionsModel target)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = this.package.GetService<DTE, DTE>();
            if (dte.Solution == null)
                return false;
            var cfgFilePath = GetCfgFilePath();
            if (!File.Exists(cfgFilePath))
                return false;
            ExtensionsSerialization.LoadFromFile(target, cfgFilePath);
            foreach (var item in target.Extensions)
            {
                EnsureTitle(item);
            }

            //return target.Extensions.Count > 0;
            return true;
        }

        public void SaveFile(ExtensionsModel source)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = this.package.GetService<DTE, DTE>();
            if (dte.Solution == null)
                throw new InvalidOperationException("No solution loaded");
            var cfgFilePath = GetCfgFilePath();
            if (!File.Exists(cfgFilePath) && source.Extensions.Count == 0)
                return; //skip saving if not exists and no extension
            ExtensionsSerialization.SaveToFile(source, cfgFilePath);
        }
        public void SetDllPath(ExtensionItem item, string fileName)
        {
            if (!fileName.Contains('%') && !fileName.Contains("$("))
            {
                var solPath = Path.GetDirectoryName(GetSolutionFileName());
                if (fileName.StartsWith(solPath, StringComparison.OrdinalIgnoreCase))
                {
                    var relPath = fileName.Substring(solPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    fileName = Path.Combine("$(SolutionDir)", relPath);
                }
            }
            item.DllPath = fileName;
        }


        public string GetRealPath(string dllPath)
        {
            string getVariable(string name)
            {
                switch (name)
                {
                    case "SOLUTIONDIR": return Path.GetDirectoryName(GetSolutionFileName());
                    case SELFKEY: return SelfAssembly.Location;
                    default: return null;
                }
            }
            return StringTemplates.ExpandString(dllPath, getVariable, envVariables: true);
        }
        public void EnsureTitle(ExtensionItem item)
        {
            if (!string.IsNullOrEmpty(item.Title))
                return;
            if (string.IsNullOrEmpty(item.ClassName))
                return;
            item.Title = item.ClassName.Split('.').Last();
        }
        public bool IsDllPathSelf(ExtensionItem item)
        {
            return item.DllPath == SELF;
        }

        private Lazy<Assembly> _selfAssembly = new Lazy<Assembly>(() => typeof(CreateGUID).Assembly);
        public Assembly SelfAssembly => _selfAssembly.Value;

        public void SetItemTitleFromMethod(ExtensionItem item)
        {
            if (!String.IsNullOrEmpty(item.Title))
                return;
            var (method, type) = FindExtensionMethod(item, throwIfNotFound: false);
            if (method == null)
                return;
            var description = method.GetDescription() ?? type.GetDescription() ?? type.Name;
            item.Title = description;
        }
        public void SetArgumentFromClass(ExtensionItem item)
        {
            if (!string.IsNullOrEmpty(item.Argument))
                return;
            var (_, type) = FindExtensionMethod(item, throwIfNotFound: false);
            if (type == null)
                return;
            var pi = ExtensionObject.FindArgumentProperty(type);
            if (pi.propertyInfo == null) return;
            item.Argument = "?";
        }

        private (MethodInfo method, Type type) FindExtensionMethod(ExtensionItem item, bool throwIfNotFound)
        {
            var assembly = LoadVersionedAssembly(GetRealPath(item.DllPath));
            return ExtensionObject.FindExtensionMethod(assembly, item.ClassName, throwIfNotFound);
        }

        public bool CompileIfNeeded(ExtensionItem item)
        {
            if (!item.CompileBeforeRun)
                return false;
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = package.GetService<DTE, DTE>() as DTE2;
            var path = Path.GetDirectoryName(item.DllPath);
            var name = Path.GetFileNameWithoutExtension(item.DllPath);
            var projects = dte.Solution.Projects.Cast<Project>().ToArray();
            var proj = projects.FirstOrDefault(p => p.Name == name);
            if (proj == null)
                throw new ApplicationException($"Cannot find project '{name}' to compile");
            var cfg = dte.Solution.SolutionBuild.ActiveConfiguration.Name;
            dte.Solution.SolutionBuild.BuildProject(cfg, proj.UniqueName, WaitForBuildToFinish: true);
            if (dte.Solution.SolutionBuild.LastBuildInfo != 0)
                throw new ApplicationException($"Error compiling project '{name}'");
            return true;
        }

        private Assembly LoadVersionedAssembly(string dllPath)
        {
            if (dllPath == SelfAssembly.Location)
                return SelfAssembly;
            return ExtensionObject.LoadVersionedAssembly(dllPath);
        }

        public string[] FindExtensionClassesInDll(string dllPath)
        {
            var assembly = LoadVersionedAssembly(GetRealPath(dllPath));
            return ExtensionObject.GetExtensionClassNames(assembly);
        }
        public bool AskArgumentIfNeeded(ExtensionItem item, out string argument)
        {
            argument = item.Argument;
            if (argument != null && argument.StartsWith("?"))
            {
                argument = argument.TrimStart('?');
                var (_, type) = FindExtensionMethod(item, throwIfNotFound: false);
                var prompt = "Enter argument value";
                if (type != null)
                {
                    var pi = ExtensionObject.FindArgumentProperty(type);
                    prompt = pi.description ?? prompt;
                    if (String.IsNullOrEmpty(argument))
                        argument = pi.defaultValue + "";
                }
                if (!TextInputDialog.Show(item.Title, prompt, argument, out argument))
                    return false;
            }
            return true;
        }
        public void RunExtension(ExtensionItem item, string argument)
        {
            var dte = package.GetService<DTE, DTE>();
            var (method, type) = FindExtensionMethod(item, throwIfNotFound: true);
            ExtensionObject.RunExtension(type, method, dte, package, argument);
        }

        public bool IsDllPathInSolutionScope(ExtensionItem item)
        {
            var solPath = Path.GetDirectoryName(GetSolutionFileName());
            var realDllPath = GetRealPath(item.DllPath);
            var dllPath = Path.GetDirectoryName(realDllPath);
            return dllPath.StartsWith(solPath, StringComparison.OrdinalIgnoreCase);
        }
        public string GetCfgFilePath()
        {
            return Path.ChangeExtension(GetSolutionFileName(), ".extensions.cfg");
        }

        private string GetSolutionFileName()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = this.package.GetService<DTE, DTE>();
            if (dte.Solution == null || String.IsNullOrEmpty(dte.Solution.FullName))
                return null;
            return dte.Solution.FullName;
        }

        public bool IsDllExists(ExtensionItem item)
        {
            var realDllPath = GetRealPath(item.DllPath);
            return File.Exists(realDllPath);
        }

        public enum CheckResult
        {
            Ok,
            ClassNotFound,
            RunMethodNotFound,
            ArgumentPropertyNotFound,
        }
        public CheckResult CheckItemCode(ExtensionItem item)
        {
            var (method, type) = FindExtensionMethod(item, throwIfNotFound: false);
            if (type == null)
                return CheckResult.ClassNotFound;
            if (method == null)
                return CheckResult.RunMethodNotFound;
            if (!string.IsNullOrEmpty(item.Argument))
            {
                var pi = ExtensionObject.FindArgumentProperty(type);
                if (pi.propertyInfo == null)
                    return CheckResult.ArgumentPropertyNotFound;
            }
            return CheckResult.Ok;
        }

    }

}