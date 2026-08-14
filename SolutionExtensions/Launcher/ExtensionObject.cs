using EnvDTE;
using SolutionExtensions.Reflector;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SolutionExtensions
{

    public static class ExtensionObject
    {

        public static void RunExtension(ExtensionRI ri, DTE dte, object package, string argument)
        {
            var method = ri.RunMethod;
            //var (method, type) = FindExtensionMethod(assembly, className, throwIfNotFound: true);
            var parameters = new object[method.GetParameters().Length];
            parameters[0] = dte;
            if (parameters.Length > 1)
                parameters[1] = package;
            var instance = method.IsStatic ? null : Activator.CreateInstance(ri.Type);
            if (!string.IsNullOrEmpty(argument))
            {
                var ai = ri.FindArgumentProperty();
                if (ai.propertyInfo == null) throw new Exception($"Missing Argument property on '{ri.Type.Name}'");
                var argValue = Convert.ChangeType(argument, ai.propertyInfo.PropertyType);
                ai.propertyInfo.SetValue(instance, argValue);
            }
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
            return assembly.GetTypes().Where(t => ExtensionRI.IsExtensionClass(t)).Select(t => t.FullName).ToArray();
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
            if (!File.Exists(verFullName))
                return null;
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

    /// <summary>
    /// reflection info of extension
    /// </summary>
    public class ExtensionRI
    {
        // void Run(DTE dte, object package?)
        public const string RUN_METHOD = "Run";
        // string Generate(DTE dte, string input, string inputFileName, string ns)
        public const string GENERATE_METHOD = "Generate";
        private Assembly assembly;
        private string className;
        private Type type;

        public bool ThrowIfNotFound { get; set; } = false;

        public ExtensionRI(Assembly assembly, string className)
        {
            this.assembly = assembly;
            this.className = className;
        }
        public Type Type
        {
            get
            {
                var type = String.IsNullOrEmpty(className) ? null : assembly.GetType(className);
                if (type == null)
                {
                    if (ThrowIfNotFound)
                        throw new InvalidOperationException($"Class {className} not found in assembly {assembly.FullName}");
                }
                return type;
            }
        }

        public string GetDescription()
        {
            var mi = GetMethod(IsKnownMethod);
            if (mi == null)
                return null;
            return mi.GetDescription() ??
                Type.GetDescription() ??
                Type.Name;
        }

        public MethodInfo RunMethod => GetMethod(IsRunMethod);
        private MethodInfo GetMethod(Func<MethodInfo, bool> find)
        {
            var methods = Type.GetMethods();
            var method = methods.FirstOrDefault(m => find(m));
            if (method == null && ThrowIfNotFound)
                throw new InvalidOperationException($"Class {className} does not have a valid extension method.\n{DumpType(type)}");
            return method;
        }

        public (PropertyInfo propertyInfo, string description, object defaultValue) FindArgumentProperty()
        {
            var propertyInfo = Type.GetProperty("Argument");
            var defaultValue = propertyInfo?.GetCustomAttribute<DefaultValueAttribute>()?.Value;
            var description = propertyInfo?.GetDescription();
            return (propertyInfo, description, defaultValue);
        }

        private static string DumpType(Type type)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Type {type.FullName} in {type.Assembly.Location}");
            sb.AppendLine($"Methods:");
            foreach (var mi in type.GetMethods()
                .OrderBy(mi => IsKnownMethod(mi))
                .ThenBy(mi => mi.Name))
            {
                sb.Append($"{mi.Name}(");
                var parameters = mi.GetParameters();
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i != 0) sb.Append(", ");
                    var pi = parameters[i];
                    sb.Append($"{pi.ParameterType.FullName} {pi.Name}");
                    sb.Append($"/* isDTE:{IsDTEParameter(pi)} */");
                }
                sb.AppendLine($") (check:{IsKnownMethod(mi)})");
            }
            return sb.ToString();
        }

        public static bool IsExtensionClass(Type type)
        {
            return type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Any(m => IsKnownMethod(m));
        }
        public static bool IsKnownMethod(MethodInfo m)
        {
            return IsRunMethod(m) || IsGenerateMethod(m);
        }

        public static bool IsRunMethod(MethodInfo m)
        {
            return m.Name == RUN_METHOD &&
                m.GetParameters().Length >= 1 &&
                IsDTEParameter(m.GetParameters()[0]);
        }
        private static bool IsGenerateMethod(MethodInfo m)
        {
            return m.Name == GENERATE_METHOD &&
                m.GetParameters().Length >= 1 &&
                IsDTEParameter(m.GetParameters()[0]);
        }

        private static bool IsDTEParameter(ParameterInfo pi)
        {
            if (typeof(DTE).IsAssignableFrom(pi.ParameterType))
                return true;//not working when used in launcher, possible another envdte.dll (merged)
            if (pi.ParameterType.GUID == typeof(DTE).GUID)
                return true;
            if (pi.ParameterType.GetInterfaces().Any(i => i.GUID == typeof(DTE).GUID))
                return true;
            return false;
        }


    }
}
