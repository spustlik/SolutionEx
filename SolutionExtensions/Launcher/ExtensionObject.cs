using EnvDTE;
using SolutionExtensions.Reflector;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SolutionExtensions
{
    public static class ExtensionObject
    {
        public static void Run(ExtensionRI ri, DTE dte, object package, string argument)
        {
            if (ri == null)
                throw new ArgumentNullException("ri");
            var method = ri.RunMethod;
            if (method == null)
                throw new ArgumentNullException("method");
            //var (method, type) = FindExtensionMethod(assembly, className, throwIfNotFound: true);
            var parameters = new object[method.GetParameters().Length];
            parameters[0] = dte;
            if (parameters.Length > 1)
                parameters[1] = package;
            if (ri.Type == null)
                throw new ArgumentNullException("Type");
            var instance = method.IsStatic ? null : Activator.CreateInstance(ri.Type);
            if (!string.IsNullOrEmpty(argument))
            {
                var ai = ri.FindArgumentProperty();
                if (ai == default || ai.propertyInfo == null)
                    throw new Exception($"Missing Argument property on '{ri.Type.Name}'");
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

        public static async Task<GeneratorResult> GenerateAsync(ExtensionRI ri, DTE dte, string content, string inputPath, string defaultNamespace, string argument)
        {
            // string Generate(DTE dte, string input, string inputFileName, string ns)
            var method = ri.GenerateMethod;
            //var (method, type) = FindExtensionMethod(assembly, className, throwIfNotFound: true);
            var parameters = new object[method.GetParameters().Length];
            parameters[0] = dte;
            if (parameters.Length > 1)
                parameters[1] = content;
            if (parameters.Length > 2)
                parameters[2] = inputPath;
            if (parameters.Length > 3)
                parameters[3] = defaultNamespace;
            var extension = "";
            var instance = method.IsStatic ? null : Activator.CreateInstance(ri.Type);
            var argProp = ri.FindArgumentProperty();
            if (argProp.propertyInfo != null)
            {
                argProp.propertyInfo.SetValue(instance, argument);
            }
            var extProp = ri.FindGeneratorExtensionProperty();
            if (extProp.propertyInfo != null)
            {
                var value = extProp.propertyInfo.GetValue(instance);
                extension = Convert.ChangeType(value, typeof(string)) as string;
            }
            try
            {
                var resultObj = method.Invoke(instance, parameters);
                return await GetResultAsync(resultObj, method, extension);
            }
            catch (TargetInvocationException tex)
            {
                throw tex.InnerException;
            }
        }

        private static async Task<GeneratorResult> GetResultAsync(object result, MethodInfo method, string extension)
        {
            if (result == null) return new GeneratorResult() { Extension = extension };
            var resultObj = result;
            if (resultObj is Task task && IsTaskWithResult(resultObj.GetType()))
            {
                dynamic dtask = task;
                resultObj = await dtask;
            }
            if (resultObj is string)
            {
                var resultContent = resultObj as string;
                return new GeneratorResult() { Content = resultContent, Extension = extension };
            }
            else if (resultObj is IEnumerable<string> strings)
            {
                return new GeneratorResult() { Files = strings.Where(s=>!String.IsNullOrWhiteSpace(s)).ToArray(), Extension = extension };
            }
            throw new InvalidOperationException($"Unsupported result type {result.GetType()}");
        }

        private static bool IsTaskWithResult(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>);
        }

        public static string[] GetExtensionClassNames(Assembly assembly)
        {
            return assembly.GetTypes()
                .Where(t => ExtensionRI.IsExtensionClass(t))
                .Select(t => t.FullName)
                .OrderBy(s => s)
                .ToArray();
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
        public MethodInfo GenerateMethod => GetMethod(IsGenerateMethod);
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
        public (PropertyInfo propertyInfo, object _) FindGeneratorExtensionProperty()
        {
            var propertyInfo = Type.GetProperty("Extension");
            return (propertyInfo, null);
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

    //Content or Files or nothing, extension when files if for file without extension
    public class GeneratorResult
    {
        public string Extension { get; set; }
        public string Content { get; set; }
        public string[] Files { get; set; }
    }

}
