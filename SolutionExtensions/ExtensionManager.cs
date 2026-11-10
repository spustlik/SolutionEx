using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using SolutionExtensions.Extensions;
using SolutionExtensions.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

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
                cmd.AddShortCutToCommand(item.ShortCutKey, clear: true);
                if (oleCmd != null)
                {
                    oleCmd.Text = item.Title;
                    oleCmd.Enabled = true;
                    oleCmd.Visible = true;
                }
            }
            else
            {
                cmd.AddShortCutToCommand(null, clear: true);
                if (oleCmd != null)
                {
                    oleCmd.Enabled = false;
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
            fileName = GetSolutionRelativePath(fileName);
            item.DllPath = fileName;
        }

        public string GetSolutionRelativePath(string fileName)
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
            return fileName;
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

        private ExtensionRI FindRI(ExtensionItem item)
        {
            var a = LoadAssembly(item);
            if (a == null)
                return null;
            return new ExtensionRI(a, item.ClassName);
        }

        public void UpdateItemFromDll(ExtensionItem item)
        {
            item.ArgumentTitle = null;
            ExtensionRI ri = FindRI(item);
            if (ri == null || ri.Type == null) return;
            item.IsGenerator = ri.GenerateMethod != null;
            if (String.IsNullOrEmpty(item.Title))
                item.Title = ri.GetDescription();
            var ap = ri.FindArgumentProperty();
            if (ap.propertyInfo == null) return;
            item.ArgumentTitle = ap.description;
        }

        public Assembly LoadAssembly(ExtensionItem item)
        {
            if (String.IsNullOrEmpty(item.DllPath))
                return null;
            var assembly = LoadVersionedAssembly(GetRealPath(item.DllPath));
            return assembly;
        }

        public bool CompileIfNeeded(ExtensionItem item)
        {
            if (!item.CompileBeforeRun)
                return false;
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = package.GetService<DTE, DTE>() as DTE2;
            var path = Path.GetDirectoryName(item.DllPath);
            var name = Path.GetFileNameWithoutExtension(item.DllPath);

            var proj = dte.Solution.FindSolutionProject(p => p.Name == name);
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
            if (assembly == null)
                return new string[0];
            return ExtensionObject.GetExtensionClassNames(assembly);
        }
        public bool AskArgumentIfNeeded(ExtensionItem item, out string argument)
        {
            argument = item.Argument;
            if (argument != null && argument.StartsWith("?"))
            {
                argument = argument.TrimStart('?');
                var ri = FindRI(item);
                if (ri != null)
                {
                    var prompt = "Enter argument value";
                    if (ri.Type != null)
                    {
                        var ai = ri.FindArgumentProperty();
                        prompt = ai.description ?? prompt;
                        if (String.IsNullOrEmpty(argument))
                            argument = ai.defaultValue + "";
                    }
                    if (!TextInputDialog.Show(item.Title, prompt, argument, out argument))
                        return false;
                }
            }
            return true;
        }
        public void RunExtension(ExtensionItem item, string argument)
        {
            var dte = package.GetService<DTE, DTE>();
            var ri = FindRI(item);
            if (ri == null)
                throw new InvalidOperationException($"Invalid Reflection information of item");
            ExtensionObject.Run(ri, dte, package, argument);
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
            MethodNotFound,
            ArgumentPropertyNotFound,
        }
        public CheckResult CheckItemCode(ExtensionItem item)
        {
            var ri = FindRI(item);
            if (ri == null)
                return CheckResult.ClassNotFound;
            if (ri.Type == null)
                return CheckResult.ClassNotFound;
            if (ri.RunMethod == null && ri.GenerateMethod == null)
                return CheckResult.MethodNotFound;
            if (!string.IsNullOrEmpty(item.Argument))
            {
                var ai = ri.FindArgumentProperty();
                if (ai.propertyInfo == null)
                    return CheckResult.ArgumentPropertyNotFound;
            }
            return CheckResult.Ok;
        }

        /// <summary>
        /// add files to item and if it exists on another, it is removed
        /// </summary>
        public (int removed, int added) AddFilesToItem(ExtensionItem item, IEnumerable<ExtensionItem> extensions, string[] addFiles)
        {
            int added = AddFiles(item, addFiles);
            var other = extensions.Where(ex => ex.IsGenerator && ex != item).ToArray();
            int removed = RemoveFiles(other, addFiles);
            return (removed, added);
        }

        public int RemoveFiles(IEnumerable<ExtensionItem> extensions, string[] files)
        {
            int removed = 0;
            var removeFiles = files.Select(fn => GetSolutionRelativePath(fn).ToLowerInvariant());
            foreach (var gen in extensions)
            {
                var exFiles = gen.Files.Select(fn => fn.ToLowerInvariant()).ToArray();
                foreach (var fn in removeFiles)
                {
                    var index = exFiles.IndexOf(fn);
                    if (index < 0) continue;
                    gen.Files.RemoveAt(index);
                    removed++;
                }
            }
            return removed;
        }

        private int AddFiles(ExtensionItem item, string[] files)
        {
            int added = 0;
            foreach (var fn in files)
            {
                if (item.Files.Contains(fn, StringComparer.InvariantCultureIgnoreCase))
                    continue;
                item.Files.Add(fn);
                added++;
            }
            return added;
        }

        public IEnumerable<ExtensionItem> GetGeneratorsAssignedTo(IEnumerable<ExtensionItem> extensions, string fullName)
        {
            fullName = GetSolutionRelativePath(fullName);
            return extensions
                .Where(ex => ex.IsGenerator)
                .Where(ex => ex.Files.Any(fn => String.Compare(fn, fullName, StringComparison.InvariantCultureIgnoreCase) == 0));
        }

        public GeneratorResult RunGeneratorOnFile(string inputPath, string defaultNamespace)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            package.Log($"Find generator for file {inputPath}");
            var fn = GetSolutionRelativePath(inputPath);
            var item = package.Model.Extensions
                .Where(ex => ex.IsGenerator)
                .Where(ex => ex.Files.Contains(fn, StringComparer.OrdinalIgnoreCase))
                .FirstOrDefault();
            package.Log($"Result: {item?.Title} {item?.DllPath}.{item?.ClassName}");
            try
            {
                if (item == null)
                    throw new Exception($"Generator for {fn} cannot be found");
                if (CompileIfNeeded(item))
                    package.Log($"Extension recompiled");
                var ri = FindRI(item);
                if (ri == null)
                    throw new InvalidOperationException($"Invalid Reflection information of item");
                //ExtensionObject.RunExtension(ri, dte, package, argument);
                var dte = package.GetService<DTE, DTE>();
                var content = File.ReadAllText(inputPath);
                var result = package.JoinableTaskFactory.Run(
                    () => ExtensionObject.GenerateAsync(ri, dte, content, inputPath, defaultNamespace)
                );
                return result;
            }
            catch (Exception ex)
            {
                package.Log($"Error: {ex.Message}\n{ex}");
                throw;
            }
        }

        public string[] AssignGeneratorResultFiles(GeneratorResult result, string inputPath, Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var files = GetGeneratorResultFiles(result, inputPath);
            foreach (var f in files)
            {
                project.ProjectItems.AddFromFile(f);
            }
            package.Log($"Generated files: {string.Join(",", files)}");
            return files;
        }
        private string[] GetGeneratorResultFiles(GeneratorResult result, string inputPath) 
        { 
            var rootPath = Path.GetDirectoryName(inputPath);
            if (result.Files.Length > 0)
            {
                string getPath(string fileName)
                {
                    if (!Path.IsPathRooted(fileName))
                        fileName = Path.GetFullPath(Path.Combine(rootPath, fileName));
                    if (Path.GetExtension(fileName) == ".")
                        fileName = Path.ChangeExtension(fileName, result.Extension);
                    return fileName;
                }
                return result.Files.Select(fn => getPath(fn)).ToArray();
            }
            else
            {
                var fileName = inputPath;
                if (!Path.IsPathRooted(fileName))
                    fileName = Path.GetFullPath(Path.Combine(rootPath, fileName));
                fileName = Path.ChangeExtension(fileName, result.Extension);
                File.WriteAllText(fileName, result.Content);
                return new[] { fileName };
            }
        }

        private bool HasCustomTool(ProjectItem projectItem, out string customTool, out Property prop)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            prop = projectItem.Properties.Item("CustomTool");
            customTool = prop.Value as string;
            return customTool == nameof(SolutionFileGenerator);
        }

        /// <summary>
        /// returns true if success, or already exists
        /// </summary>
        private bool TrySetCustomTool(ProjectItem projectItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!HasCustomTool(projectItem, out _, out var prop))
            {
                try
                {
                    prop.Value = nameof(SolutionFileGenerator);
                }
                catch
                {
                    /* if not supported, throws
                    System.InvalidOperationException: The "None" rule is missing the "Generator" property.
                       at Microsoft.Verify.Operation(Boolean condition, String unformattedMessage, Object arg1, Object arg2)
                    ...
                    at Microsoft.VisualStudio.ProjectSystem.VS.Implementation.Package.Automation.OAPropertyInfo.set_Value(Object value)
                    at SolutionExtensions.ToolWindows.ExtensionsListToolWindow.SetCustomTool(IEnumerable`1 items)
                    */
                    return false;
                }
            }
            return true;
        }
    }
}