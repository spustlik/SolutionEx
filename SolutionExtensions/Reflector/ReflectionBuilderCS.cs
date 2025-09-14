using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

/*
 * TODO: use some class structure of generated result (CODEDOM?)
 * than:
 *   - group namespaces
 *   - smart usings
 * TODO: add options:
 * - skip COM/interop attributes
 * - 
 */
namespace SolutionExtensions.Reflector
{
    public class ReflectionBuilderCS
    {
        public HashSet<Type> UsedTypes { get; } = new HashSet<Type>();
        public void Clear()
        {
            UsedTypes.Clear();
        }

        public virtual string GetTypeName(Type type, bool useShort = false)
        {
            int depth = 0;
            string getGenTypeName(Type t)
            {
                if (t.IsGenericTypeDefinition)
                    return t.Name.Split(new[] { '`' }, 2)[0];
                if (!t.IsGenericType || depth > 3)
                    return t.Name;
                var args = String.Join(",", t.GetGenericArguments().Select(ga => getTypeName(ga, true)));
                var gt = t.GetGenericTypeDefinition();
                if (gt == typeof(Nullable<>))
                    return args + "?";
                var n = getTypeName(gt, true);
                return $"{n}<{args}>";
            }
            string getTypeName(Type t, bool _useShort)
            {
                depth++;
                if (_useShort || IsKnownTypeName(t))
                {
                    return GetPrimitiveTypeName(t) ?? getGenTypeName(t);
                }
                var s = t.Namespace; // can be null in spec. 
                if (s != null) s += ".";
                s += getGenTypeName(t);
                return s;
            }
            UsedTypes.Add(type);
            return getTypeName(type, useShort);
        }

        private static bool IsKnownTypeName(Type t)
        {
            return
                t.Namespace == null || 
                t.Namespace == nameof(System) || 
                t.Namespace == nameof(System.Runtime.InteropServices);
        }

        public static string GetPrimitiveTypeName(Type t)
        {
            if (t == typeof(string))
                return "string";
            if (t == typeof(object))
                return "object";
            if (!t.IsPrimitive)
                return null;
            if (t == typeof(int))
                return "int";
            if (t == typeof(uint))
                return "uint";
            if (t == typeof(bool))
                return "bool";
            if (t == typeof(double))
                return "double";
            return null;
        }

        
        class IndentableWriter
        {
            private int indent;
            private StringBuilder sb = new StringBuilder();
            public string Text => sb.ToString();
            public void PushIndent() => indent++;
            public void PopIndent() => indent--;
            public void WriteLine(string s)
            {
                if (s == null || !s.EndsWith("\n"))
                    s += "\n";
                for (var i = 0; i < indent; i++)
                    sb.Append("\t");
                sb.Append(s);
            }
        }
        public string BuildDeclaration(Type type)
        {
            UsedTypes.Add(type);
            var w = new IndentableWriter();
            w.WriteLine($"namespace {type.Namespace}");
            w.WriteLine("{");
            w.PushIndent();
            GenerateAttributes(w, type);
            if (type.IsEnum)
                GenerateEnum(w, type);
            else
                GenerateClass(w, type);
            w.PopIndent();
            w.WriteLine("}"); //ns
            return w.Text;
        }

        private void GenerateClass(IndentableWriter w, Type type)
        {
            var parts = new List<string>();
            if (type.IsValueType)
                parts.Add($"struct {GetTypeName(type, true)}");
            else if (type.IsInterface)
                parts.Add($"interface {GetTypeName(type, true)}");
            else
                parts.Add($"public abstract class {GetTypeName(type, useShort: true)}");

            var baseTypes = new List<Type>();
            if (type.BaseType != null && type.BaseType != typeof(object) && type.BaseType != typeof(ValueType))
                baseTypes.Add(type.BaseType);
            baseTypes.AddRange(type.GetInterfaces());
            if (baseTypes.Count > 0)
                parts.Add(": " + String.Join(", ", baseTypes.Select(t => GetTypeName(t))));
            w.WriteLine(string.Join(" ", parts));
            w.WriteLine("{");
            w.PushIndent();
            GenerateFields(w, type);
            GenerateProperties(w, type);
            GenerateMethods(w, type);
            w.PopIndent();
            w.WriteLine("}");
        }

        private void GenerateEnum(IndentableWriter w, Type type)
        {
            var s = $"public enum {GetTypeName(type, useShort: true)}";
            var ut = Enum.GetUnderlyingType(type);
            w.WriteLine(s);
            w.WriteLine("{");
            w.PushIndent();
            foreach (var item in Enum.GetValues(type))
            {
                var name = Enum.GetName(type, item);
                var value = "";
                if (ut == typeof(int))
                    value = $" = {(int)item}";
                w.WriteLine($"{name}{value},");
            }
            w.PopIndent();
            w.WriteLine("}");
        }

        private string BuildAttributes(object[] attrs)
        {
            var w = new IndentableWriter();
            GenerateAttributes(w, attrs);
            return w.Text.Trim();
        }

        private void GenerateAttributes(IndentableWriter w, MemberInfo info)
        {
            var attrs = info.GetCustomAttributes(false);
            GenerateAttributes(w, attrs);
        }

        private void GenerateAttributes(IndentableWriter w, object[] attrs)
        {
            foreach (var attr in attrs)
            {
                var name = GetTypeName(attr.GetType(), useShort:true);
                if (name.EndsWith(nameof(Attribute)))
                    name = name.Substring(0, name.Length - nameof(Attribute).Length);
                var args = new List<(string, object)>();
                foreach (var pi in attr.GetType().GetProperties())
                {
                    if (pi.DeclaringType == typeof(Attribute))
                        continue;
                    var pv = pi.GetValue(attr);
                    if (pv != null)
                        args.Add((pi.Name, pv));
                }
                var sargs = args.Select(x => x.Item1 + " = " + BuildLiteral(x.Item2));
                //can be optimized to use constructor, attr.GetType().GetConstructors()
                //if only 1, it is possible easy
                //but how to map name of property to ctor arg?
                w.WriteLine($"[{name}({String.Join(", ", sargs)})]");
            }
        }

        public string BuildLiteral(object value, bool niceNumbers = true)
        {
            if (value == null)
                return "null";
            if (value is bool b)
                return b ? "true" : "false";
            if (value is int i)
            {
                //in WPF double is used
                var s = i.ToString();
                if (niceNumbers)
                    if (i > 10000 || i < -10000) s += $" (0x{i:X})";
                return s;
            }
            if (value is double d)
                return d.ToString(CultureInfo.InvariantCulture);
            if (value is Type t)
                return $"typeof({GetTypeName(t)})";
            if (value.GetType().IsEnum)
                return $"{GetTypeName(value.GetType())}.{value}";
            return $"\"{value}\"";
        }

        private void GenerateFields(IndentableWriter w, Type type)
        {
            foreach (var fi in type.GetFields())
            {
                GenerateField(w, fi);
            }
        }

        private void GenerateField(IndentableWriter w, FieldInfo fi)
        {
            var parts = new List<string>();
            if (fi.IsPublic)
                parts.Add("public");
            if (fi.IsStatic)
                parts.Add("static");
            GenerateAttributes(w, fi);
            parts.Add(GetTypeName(fi.FieldType, useShort: false));
            parts.Add(fi.Name);
            w.WriteLine(string.Join(" ", parts) + ";");
        }
        private void GenerateMethods(IndentableWriter w, Type type)
        {
            foreach (var mi in type.GetMethods())
            {
                if (mi.DeclaringType != type)
                    continue;
                if (mi.IsSpecialName)
                    continue;
                GenerateMethod(w, mi);
            }
        }

        private void GenerateMethod(IndentableWriter w, MethodInfo mi)
        {
            GenerateAttributes(w, mi);
            w.WriteLine(BuildMethodSignature(mi) + ";");
        }

        public string BuildMethodSignature(MethodInfo mi, bool returnTypeAtEnd = false)
        {
            var parts = new List<string>();
            if (mi.IsPublic) parts.Add("public");
            if (mi.IsPrivate) parts.Add("private");
            if (mi.IsFinal) parts.Add("sealed");
            if (mi.IsStatic) parts.Add("static");
            var returnType = mi.ReturnType == null || mi.ReturnType == typeof(void) ? "void" : GetTypeName(mi.ReturnType, true);
            if (!returnTypeAtEnd)
                parts.Add(returnType);
            var name = BuildMethodName(mi);
            parts.Add(name);
            if (returnTypeAtEnd)
                parts.Add(": " + returnType);
            return string.Join(" ", parts);
        }

        private string BuildMethodName(MethodInfo mi)
        {
            var name = mi.Name;
            if (mi.IsGenericMethod)
                name = $"{name}<{String.Join(", ", mi.GetGenericArguments().Select(ar => GetTypeName(ar, true)))}>";
            var args = new List<string>();
            foreach (var arg in mi.GetParameters())
            {
                args.Add(BuildMethodParameter(arg));
            }
            return $"{name}({string.Join(", ", args)})";
        }

        private string BuildMethodParameter(ParameterInfo arg)
        {
            var parts = new List<string>();
            var at = BuildAttributes(arg.GetCustomAttributes().ToArray());
            if (!string.IsNullOrEmpty(at))
                parts.Add(at.Replace("\n"," "));
            if (arg.IsIn)
                parts.Add("in");
            if (arg.IsOut)
                parts.Add("out");
            if (arg.ParameterType.IsByRef)
                parts.Add("ref");
            var type = GetTypeName(arg.ParameterType, true);
            if (arg.IsOptional)
                type += "?";
            parts.Add(type);
            parts.Add(arg.Name);
            if (arg.HasDefaultValue)
                parts.Add(BuildLiteral(arg.DefaultValue));
            return string.Join(" ", parts);
        }

        private void GenerateProperties(IndentableWriter w, Type type)
        {
            foreach (var pi in type.GetProperties())
            {
                if (pi.IsSpecialName)
                    continue;
                GenerateProperty(w, pi);
            }
        }

        private void GenerateProperty(IndentableWriter w, PropertyInfo pi)
        {
            GenerateAttributes(w, pi);
            var keys = new List<string>();
            if (pi.CanRead || pi.CanWrite)
                keys.Add("public");
            else
                keys.Add("private");
            if (pi.GetGetMethod()?.IsStatic == true || pi.GetSetMethod()?.IsStatic == true)
                keys.Add("static");
            keys.Add(GetTypeName(pi.PropertyType, useShort: false));
            keys.Add(pi.Name);
            keys.Add("{");
            if (pi.CanRead)
                keys.Add("get;");
            if (pi.CanWrite)
                keys.Add("set;");
            keys.Add("}");
            var s = String.Join(" ", keys);
            //TODO: ?? get/set attributes? private/public get/set
            w.WriteLine(s);
        }

    }
}
