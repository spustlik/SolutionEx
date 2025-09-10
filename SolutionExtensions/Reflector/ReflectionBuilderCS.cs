using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SolutionExtensions.Reflector
{
    public class ReflectionBuilderCS
    {
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
                if (_useShort || t.Namespace == nameof(System))
                {
                    return GetPrimitiveTypeName(t) ?? getGenTypeName(t);
                }
                var s = t.Namespace; // can be null in spec. 
                if (s != null) s += ".";
                s += getGenTypeName(t);
                return s;
            }
            return getTypeName(type, useShort);
        }

        public static string GetPrimitiveTypeName(Type t)
        {
            if (!t.IsPrimitive)
                return null;
            if (t == typeof(string))
                return "string";
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

        public string GetMethodSignature(System.Reflection.MethodInfo m, bool returnTypeAtEnd = true)
        {
            var returnType = m.ReturnType == null || m.ReturnType == typeof(void) ? "void" : GetTypeName(m.ReturnType, true);
            var s = new StringBuilder();
            if (!returnTypeAtEnd)
                s.Append(returnType).Append(" ");
            s.Append(m.Name);
            if (m.IsGenericMethod)
                s.Append($"<{String.Join(", ", m.GetGenericArguments().Select(ar => GetTypeName(ar, true)))}>");
            s.Append("(");
            foreach (var arg in m.GetParameters())
            {
                if (arg.IsOut)
                    s.Append("out ");
                s.Append(GetTypeName(arg.ParameterType, true));
                if (arg.IsOptional)
                    s.Append("?");
                s.Append(" " + arg.Name);
                if (arg.HasDefaultValue)
                    s.Append(arg.DefaultValue);
                if (arg != m.GetParameters().Last())
                    s.Append(", ");
            }
            s.Append(")");
            if (returnTypeAtEnd)
                s.Append(": " + returnType);
            return s.ToString();
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
                    sb.Append("  ");
                sb.Append(s);
            }
        }
        internal string GenerateAbstractClass(Type type)
        {
            var w = new IndentableWriter();
            w.WriteLine($"namespace {type.Namespace}");
            w.WriteLine("{");
            w.PushIndent();
            GenerateAttributes(w, type);
            var s = $"public abstract class {GetTypeName(type, true)}";
            var bases = new List<Type>();
            if (type.BaseType != null && type.BaseType != typeof(object))
            {
                bases.Add(type.BaseType);
            }
            //bases.AddRange(type.GetInterfaces());
            if (bases.Count > 0)
                s += " : " + String.Join(", ", bases.Select(t => GetTypeName(t)));
            w.WriteLine(s);
            w.WriteLine("{");
            w.PushIndent();
            GenerateProperties(w, type);
            GenerateMethods(w, type);
            w.PopIndent();
            w.WriteLine("}");
            w.PopIndent();
            w.WriteLine("}");
            return w.Text;
        }

        private void GenerateAttributes(IndentableWriter w, MemberInfo info)
        {
            var attrs = info.GetCustomAttributes(false);
            foreach (var attr in attrs)
            {
                var name = GetTypeName(attr.GetType());
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
                var sargs = args.Select(x => x.Item1 + " = " + GenerateLiteral(x.Item2));
                //can be optimized to use constructor
                w.WriteLine($"[{name}({String.Join(", ", sargs)})]");
            }
        }

        private string GenerateLiteral(object value)
        {
            if (value == null)
                return "null";
            if (value is bool b)
                return b ? "true" : "false";
            if (value is int i)
                return i.ToString();
            if (value is double d)
                return d.ToString(CultureInfo.InvariantCulture);
            if (value is Type t)
                return $"typeof({GetTypeName(t)})";
            if (value.GetType().IsEnum)
                return $"{GetTypeName(value.GetType())}.{value}";
            return $"\"{value}\"";
        }

        private void GenerateMethods(IndentableWriter w, Type type)
        {
            foreach (var mi in type.GetMethods(
                BindingFlags.DeclaredOnly))
            {
                GenerateMethod(w, mi);
            }
        }

        private void GenerateMethod(IndentableWriter w, MethodInfo mi)
        {
            GenerateAttributes(w, mi);
            var keys = new List<string>();
            if (mi.IsPublic) keys.Add("public");
            if (mi.IsPrivate) keys.Add("private");
            if (mi.IsFinal) keys.Add("sealed");
            if (mi.IsStatic) keys.Add("static");
            var s = String.Join(" ", keys);
            s += " ";
            //TODO: arguments attributes, in, out, ref, ...
            w.WriteLine(s + GetMethodSignature(mi, false) + ";");

        }

        private void GenerateProperties(IndentableWriter w, Type type)
        {
            foreach (var pi in type.GetProperties(BindingFlags.DeclaredOnly))
            {
                GenerateProperty(w, pi);
            }
        }

        private void GenerateProperty(IndentableWriter w, PropertyInfo pi)
        {
            GenerateAttributes(w, pi);
            var keys = new List<string>();
            if (pi.CanRead || pi.CanWrite) keys.Add("public");
            else keys.Add("private");
            //if (pi.IsStatic) keys.Add("static");
            keys.Add(pi.Name);
            if (pi.CanRead)
                keys.Add("get;");
            if (pi.CanWrite)
                keys.Add("set;");
            var s = String.Join(" ", keys);
            //TODO: ?? get/set attributes? private/public get/set
            w.WriteLine(s);
        }

        public string GenerateInterface(Type type)
        {
            var w = new IndentableWriter();
            w.WriteLine($"namespace {type.Namespace}");
            w.WriteLine("{");
            w.PushIndent();
            GenerateAttributes(w, type);
            w.WriteLine($"interface ${GetTypeName(type, true)}");
            w.WriteLine("{");
            w.PushIndent();
            GenerateProperties(w, type);
            GenerateMethods(w, type);
            w.PopIndent();
            w.WriteLine("}");
            w.PopIndent();
            w.WriteLine("}");
            return w.Text;
        }

    }
}
