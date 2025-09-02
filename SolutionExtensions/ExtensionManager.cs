using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
            var dte = this.package.GetService<DTE, DTE>();
            if (dte.Solution == null)
                return;

            //run command is using index of item, so it must be synced
            //same shortcut, probably title - but it needs MenuItem
            Log(dte, $"Commands count={dte.Commands.Count}");
            foreach (Command cmd in dte.Commands)
            {
                //cmd.LocalizedName
                //cmd.Collection //parent collection
                var b = cmd.Bindings as object[]; //array of strings in format like "VC Dialog Editor::F9"
                Log(dte, $"ID={cmd.ID},GUID={cmd.Guid},{cmd.Name},BIND={b.Length}:{string.Join("|", b)}");
            }
            //System.Diagnostics.Debugger.Break();
        }

        private void Log(DTE dte, string msg)
        {
            dte.AddToOutputPane(msg, typeof(SolutionExtensionsPackage).Name);
        }

        public void LoadFile(ExtensionsModel target, bool skipIfNoSolution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = this.package.GetService<DTE, DTE>();
            if (dte.Solution == null)
            {
                if (skipIfNoSolution)
                    return;
                throw new InvalidOperationException("No solution loaded");
            }
            var si = dte.Solution.FindSolutionItemsProject();
            var cfgFilePath = GetCfgFilePath(dte);
            if (si != null)
            {
                var fi = si.FindProjectItem(cfgFilePath);
                if (fi != null)
                    cfgFilePath = fi.FileNames[1];
            }
            if (!File.Exists(cfgFilePath))
                return;
            LoadFromFile(target, cfgFilePath);
        }

        public void EnsureTitle(ExtensionItem item)
        {
            if (string.IsNullOrEmpty(item.Title))
            {
                if (string.IsNullOrEmpty(item.ClassName))
                    item.Title = "(no title)";
                else
                    item.Title = item.ClassName.Split('.').Last();
            }
        }

        public void SaveFile(ExtensionsModel source, bool modifySolution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = this.package.GetService<DTE, DTE>();
            if (dte.Solution == null)
                throw new InvalidOperationException("No solution loaded");
            var cfgFilePath = GetCfgFilePath(dte);
            SaveToFile(source, cfgFilePath);
            if (modifySolution)
            {
                var si = dte.Solution.FindSolutionItemsProject();
                if (si == null)
                    si = dte.Solution.AddSolutionFolder();
                var fi = si.FindProjectItem(cfgFilePath);
                if (fi == null)
                    fi = si.ProjectItems.AddFromFile(cfgFilePath);
            }
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
                switch (name.Trim().ToUpperInvariant())
                {
                    case "SOLUTIONDIR": return Path.GetDirectoryName(GetSolutionFileName());
                    default: return null;
                }
            }
            var r = Environment.ExpandEnvironmentVariables(dllPath);
            var rex = new Regex(@"\$\((?<var>[^\)]*)\)", RegexOptions.Compiled);
            r = rex.Replace(r, (m) =>
            {
                var var = m.Groups["var"].Value;
                if (!string.IsNullOrEmpty(var))
                    return getVariable(var);
                return m.Value;
            });
            return r;
        }
        public void RunExtension(ExtensionItem extension)
        {
            //try to find dll if only project name ->/bin/debug or /bin/release
            //try to rebuild if not found
            var assembly = LoadVersionedAssembly(GetRealPath(extension.DllPath));
            var type = assembly.GetType(extension.ClassName);
            if (type == null)
                throw new InvalidOperationException($"Class {extension.ClassName} not found in assembly {assembly.FullName}");
            var runMethod = type.GetMethods().FirstOrDefault(m => IsRunMethod(m));
            if (runMethod == null)
                throw new InvalidOperationException($"Class {extension.ClassName} does not have a valid Run method");
            var obj = Activator.CreateInstance(type);
            var dte = this.package.GetService<DTE, DTE>();
            var parameters = new object[runMethod.GetParameters().Length];
            parameters[0] = dte;
            if (parameters.Length > 1)
                parameters[1] = this.package; //can be used as IServiceProvider, etc.
            runMethod.Invoke(obj, parameters);
        }

        public string[] FindExtensionClassesInDll(string dllPath)
        {
            var assembly = LoadVersionedAssembly(GetRealPath(dllPath));
            return assembly.GetTypes().Where(t => IsExtensionClass(t)).Select(t => t.FullName).ToArray();
        }

        private string GetCfgFilePath(DTE dte)
        {
            return Path.ChangeExtension(GetSolutionFileName(), ".extensions.cfg");
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

        public bool IsClassValid(ExtensionItem item)
        {
            var cls = FindExtensionClassesInDll(item.DllPath);
            return cls.FirstOrDefault(c => c == item.ClassName) != null;
        }

        private bool IsExtensionClass(Type t)
        {
            return t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Any(m => IsRunMethod(m));
        }

        private bool IsRunMethod(MethodInfo m)
        {
            return m.Name == "Run" && m.GetParameters().Length >= 1 && typeof(DTE).IsAssignableFrom(m.GetParameters()[0].ParameterType);
        }

        private Assembly LoadVersionedAssembly(string dllPath)
        {
            var ver = File.GetLastWriteTime(dllPath).ToString("yyyyMMdd-HH-mm-ss-fff");
            var dllFn = Path.GetFileNameWithoutExtension(dllPath);
            var dllExt = Path.GetExtension(dllPath);
            var verFn = $"{dllFn}-{ver}{dllExt}";
            var versionsFolder = Path.Combine(Path.GetTempPath(), this.GetType().Namespace);
            var verPath = Path.Combine(versionsFolder, verFn);
            if (!File.Exists(verPath))
            {
                if (!Directory.Exists(versionsFolder))
                    Directory.CreateDirectory(versionsFolder);
                DeleteUnlockedFiles(versionsFolder, $"{dllFn}-*{dllExt}");
                using (var ai = Mono.Cecil.AssemblyDefinition.ReadAssembly(dllPath))
                {
                    ai.Name.Name = $"{ai.Name.Name}_{ver}";
                    ai.Write(verPath);
                }
            }
            var a = Assembly.LoadFrom(verPath);
            //Console.WriteLine($"loaded assembly {a.GetName().Name}, version {a.GetName().Version} from {Path.GetFileName(unique)}");
            return a;
            //File.Copy(dllPath, verPath);
        }

        private void DeleteUnlockedFiles(string path, string pattern)
        {
            foreach (var fn in Directory.GetFiles(path, pattern))
            {
                if (IsFileLocked(fn))
                    continue;
                try
                {
                    File.Delete(fn);
                }
                catch
                {
                }
            }
        }
        public static bool IsFileLocked(string filePath)
        {
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                }
                return false;
            }
            catch (IOException)
            {
                return true;
            }
        }


        public bool IsDllPathInSolutionScope(ExtensionItem item)
        {
            var solPath = Path.GetDirectoryName(GetSolutionFileName());
            var realDllPath = GetRealPath(item.DllPath);
            var dllPath = Path.GetDirectoryName(realDllPath);
            return dllPath.StartsWith(solPath, StringComparison.OrdinalIgnoreCase);
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