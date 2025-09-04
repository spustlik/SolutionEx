using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
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

            //runcommand is using index of item, so it must be synced
            //same shortcut, probably title - but it needs MenuItem
            //var dumpRoot = n
            //var doc = dte.Documents.Add(EnvDTE.Constants.vsDocumentKindText);
            //(doc.Selection as EnvDTE.TextSelection).Insert(s);
            //sync title,shortcut to commandX

            //Shortcut is on command
            var guid = CommandIds.CommandSetGuid.ToString();
            for (int i = 0; i < model.Extensions.Count; i++)
            {
                var item = model.Extensions[i];
                var cmd = dte.Commands.Cast<Command>()
                    .FirstOrDefault(c => c.Guid == guid && c.ID == CommandIds.Command_Extension1 + i);
                if (cmd == null)
                    continue;
                //TODO:sync
                //cmd.Name = "";
                //cmd.Bindings
            }
        }

        private void Log(DTE dte, string msg)
        {
            dte.AddToOutputPane(msg, typeof(SolutionExtensionsPackage).Name);
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
            var cfgFilePath = GetCfgFilePath(dte);
            if (!File.Exists(cfgFilePath))
                return false;
            LoadFromFile(target, cfgFilePath);
            return target.Extensions.Count > 0; ;
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

        public void SaveFile(ExtensionsModel source)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = this.package.GetService<DTE, DTE>();
            if (dte.Solution == null)
                throw new InvalidOperationException("No solution loaded");
            var cfgFilePath = GetCfgFilePath(dte);
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
                    case "SELF": return SelfAssembly.Location;
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

        //TODO:should return assembly of some extension
        private Lazy<Assembly> _selfAssembly = new Lazy<Assembly>(() => typeof(ExtensionManager).Assembly);
        public Assembly SelfAssembly => _selfAssembly.Value;

        public void RunExtension(ExtensionItem extension)
        {
            //TODO:try to find dll if only project name ->/bin/debug or /bin/release
            //TODO:try to rebuild if not found
            //warning: method/extension can be in this assembly
            var (method, type) = FindExtensionMethod(extension, throwIfNotFound: true);
            var dte = this.package.GetService<DTE, DTE>();
            var parameters = new object[method.GetParameters().Length];
            parameters[0] = dte;
            if (parameters.Length > 1)
                parameters[1] = this.package; //can be used as IServiceProvider, etc.
            var instance = method.IsStatic ? null : Activator.CreateInstance(type);
            method.Invoke(instance, parameters);
        }

        public (MethodInfo method, Type type) FindExtensionMethod(ExtensionItem extension, bool throwIfNotFound = false)
        {
            var assembly = LoadVersionedAssembly(GetRealPath(extension.DllPath));
            var type = String.IsNullOrEmpty(extension.ClassName) ? null : assembly.GetType(extension.ClassName);
            if (type == null)
            {
                if (throwIfNotFound)
                    throw new InvalidOperationException($"Class {extension.ClassName} not found in assembly {assembly.FullName}");
                return (method: null, type: null);
            }
            var method = type.GetMethods().FirstOrDefault(m => IsRunMethod(m));
            if (method == null)
            {
                if (throwIfNotFound)
                    throw new InvalidOperationException($"Class {extension.ClassName} does not have a valid Run method");
                return (method: null, type);
            }
            return (method, type);
        }

        public string[] FindExtensionClassesInDll(string dllPath)
        {
            var assembly = LoadVersionedAssembly(GetRealPath(dllPath));
            return assembly.GetTypes().Where(t => IsExtensionClass(t)).Select(t => t.FullName).ToArray();
        }

        public string GetCfgFilePath(DTE dte)
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
            //can be static
            return t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly).Any(m => IsRunMethod(m));
        }

        private bool IsRunMethod(MethodInfo m)
        {
            return m.Name == "Run" && m.GetParameters().Length >= 1 && typeof(DTE).IsAssignableFrom(m.GetParameters()[0].ParameterType);
        }

        private Assembly LoadVersionedAssembly(string dllPath)
        {
            if (dllPath == SelfAssembly.Location)
                return SelfAssembly;
            var ver = File.GetLastWriteTime(dllPath).ToString("yyyyMMdd-HH-mm-ss-fff");
            var dllFn = Path.GetFileNameWithoutExtension(dllPath);
            var dllExt = Path.GetExtension(dllPath);
            var verFn = $"{dllFn}-{ver}{dllExt}";

            var versionsFolder = Path.GetDirectoryName(dllPath);// do not use another dir, to allow load referenced assemblies //Path.Combine(Path.GetTempPath(), this.GetType().Namespace);
            var verFullName = Path.Combine(versionsFolder, verFn);
            if (!File.Exists(verFullName))
            {
                if (!Directory.Exists(versionsFolder))
                    Directory.CreateDirectory(versionsFolder);
                DeleteUnlockedFiles(versionsFolder, $"{dllFn}-*{dllExt}");
                using (var ai = Mono.Cecil.AssemblyDefinition.ReadAssembly(dllPath))
                {
                    ai.Name.Name = $"{ai.Name.Name}_{ver}";
                    ai.Write(verFullName);
                }
            }
            var a = Assembly.LoadFrom(verFullName);
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