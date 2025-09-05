using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SolutionExtensions
{
    public class ExtensionManager
    {
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

            //runcommand is using index of item, so it must be synced
            //sync title,shortcut to commandX

            //Shortcut is on command
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
        /// <returns>true if any extension loaded</returns>
        public bool LoadFile(ExtensionsModel target, bool skipIfNoSolution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = this.package.GetService<DTE, DTE>();
            if (dte.Solution == null)
            {
                if (skipIfNoSolution)
                    return false;
                throw new InvalidOperationException("No solution loaded");
            }
            var cfgFilePath = GetCfgFilePath();
            if (!File.Exists(cfgFilePath))
                return false;
            LoadFromFile(target, cfgFilePath);
            return target.Extensions.Count > 0; ;
        }

        public void SaveFile(ExtensionsModel source)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = this.package.GetService<DTE, DTE>();
            if (dte.Solution == null)
                throw new InvalidOperationException("No solution loaded");
            var cfgFilePath = GetCfgFilePath();
            SaveToFile(source, cfgFilePath);
        }
        public void SaveToFile(ExtensionsModel source, string cfgFilePath)
        {
            using (var fs = new FileStream(cfgFilePath, FileMode.Create, FileAccess.Write))
            {
                using (var sw = new StreamWriter(fs))
                {
                    sw.WriteLine("# Solution extensions configuration file");
                    sw.WriteLine("# format: [Title]|[ShortCutKey]|[ClassName]|DllPath");
                    foreach (var ext in source.Extensions)
                    {
                        if (ext.Title == null && ext.DllPath == null && ext.ClassName == null)
                            continue;
                        sw.WriteLine($"{ext.Title}|{ext.ShortCutKey}|{ext.ClassName}|{ext.DllPath}");
                    }
                }
            }
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
                    case "SELF": return SelfAssembly.Location;
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
            return item.DllPath == "$(SELF)";
        }

        //TODO:should return assembly of some SELF extension
        private Lazy<Assembly> _selfAssembly = new Lazy<Assembly>(() => typeof(ExtensionManager).Assembly);
        public Assembly SelfAssembly => _selfAssembly.Value;

        public void SetItemTitleFromMethod(ExtensionItem item)
        {
            if (!String.IsNullOrEmpty(item.Title))
                return;
            var (method, type) = FindExtensionMethod(item, throwIfNotFound: false);
            if (method == null)
                return;
            var description =
                method.GetCustomAttribute<DescriptionAttribute>()?.Description ??
                type.GetCustomAttribute<DescriptionAttribute>()?.Description;
            item.Title = description;
        }

        private (MethodInfo method, Type type) FindExtensionMethod(ExtensionItem item, bool throwIfNotFound)
        {
            var assembly = LoadVersionedAssembly(GetRealPath(item.DllPath));
            return ExtensionObject.FindExtensionMethod(assembly, item.ClassName, throwIfNotFound);
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

        private void LoadFromFile(ExtensionsModel target, string cfgFilePath)
        {
            using (var fs = new FileStream(cfgFilePath, FileMode.Open, FileAccess.Read))
            {
                using (var sr = new StreamReader(fs))
                {
                    target.Extensions.Clear();
                    while (!sr.EndOfStream)
                    {
                        var line = sr.ReadLine().Trim();
                        if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
                            continue;
                        var item = LoadItem(line);
                        if (item != null)
                            target.Extensions.Add(item);
                    }
                }
            }
        }

        private ExtensionItem LoadItem(string line)
        {
            var parts = line.Split('|');
            if (parts.Length != 4)
                return null;
            var item = new ExtensionItem()
            {
                Title = parts[0],
                ShortCutKey = parts[1],
                ClassName = parts[2],
                DllPath = parts[3]
            };
            EnsureTitle(item);
            return item;
        }

        public void RunExtension(ExtensionItem extension)
        {
            var dte = package.GetService<DTE, DTE>();
            var (method, type) = FindExtensionMethod(extension, true);
            ExtensionObject.RunExtension(type, method, dte, package);
        }

        public bool IsClassValid(ExtensionItem item)
        {
            var cls = FindExtensionClassesInDll(item.DllPath);
            return cls.FirstOrDefault(c => c == item.ClassName) != null;
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

    }

}