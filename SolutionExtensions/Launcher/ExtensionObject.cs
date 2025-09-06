using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SolutionExtensions
{
    public static class ExtensionObject
    {
        public static (MethodInfo method, Type type) FindExtensionMethod(
            Assembly assembly, string className, bool throwIfNotFound = false)
        {
            var type = String.IsNullOrEmpty(className) ? null : assembly.GetType(className);
            if (type == null)
            {
                if (throwIfNotFound)
                    throw new InvalidOperationException($"Class {className} not found in assembly {assembly.FullName}");
                return (method: null, type: null);
            }
            var method = type.GetMethods().FirstOrDefault(m => IsRunMethod(m));
            if (method == null)
            {
                if (throwIfNotFound)
                    throw new InvalidOperationException($"Class {className} does not have a valid Run method");
                return (method: null, type);
            }
            return (method, type);
        }
        public static bool IsExtensionClass(Type t)
        {
            return t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly).Any(m => IsRunMethod(m));
        }

        public static bool IsRunMethod(MethodInfo m)
        {
            return m.Name == "Run" && m.GetParameters().Length >= 1 && typeof(DTE).IsAssignableFrom(m.GetParameters()[0].ParameterType);
        }


        public static void RunExtension(Type type, MethodInfo method, DTE dte, IServiceProvider package)
        {
            //var (method, type) = FindExtensionMethod(assembly, className, throwIfNotFound: true);
            var parameters = new object[method.GetParameters().Length];
            parameters[0] = dte;
            if (parameters.Length > 1)
                parameters[1] = package; //can be used as IServiceProvider, etc.
            var instance = method.IsStatic ? null : Activator.CreateInstance(type);
            try
            {
                method.Invoke(instance, parameters);
            }
            catch (TargetInvocationException tex)
            {
                throw tex.InnerException;
            }
        }

        public static string[] GetExtensionClassNames(Assembly assembly)
        {
            return assembly.GetTypes().Where(t => IsExtensionClass(t)).Select(t => t.FullName).ToArray();
        }
        public static Assembly LoadVersionedAssembly(string dllPath)
        {
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

        private static void DeleteUnlockedFiles(string path, string pattern)
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
    }
}