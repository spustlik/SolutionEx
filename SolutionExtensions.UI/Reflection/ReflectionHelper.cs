using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SolutionExtensions.Reflector
{
    public static class ReflectionHelper
    {
        public static bool TryCallMethod<T>(object o, out T result, string methodName, params object[] arguments)
        {
            result = default;
            bool ParamsMatch(MethodInfo m)
            {
                //TODO: optional, defaultValue, byName? etc.
                var p = m.GetParameters();
                if (p.Length != arguments.Length)
                    return false;
                for (var i = 0; i < p.Length; i++)
                {
                    var a = arguments[i];
                    if (a == null) continue;
                    if (!p[i].ParameterType.IsAssignableFrom(a.GetType()))
                        return false;
                }
                return true;
            }
            var methods = o.GetType().GetMethods();
            methods = methods.Where(m => m.Name == methodName).ToArray();
            if (methods.Length != 1)
                methods = methods.Where(m => ParamsMatch(m)).ToArray();
            if (methods.Length != 1)
                return false;
            var r = methods[0].Invoke(o, arguments);
            if (r is T || typeof(T) == typeof(object))
            {
                result = (T)r;
                return true;
            }
            return false;
        }
        public static bool TryGetProperty<T>(object o, string propertyName, out T value)
        {
            value = default;
            var pc = o.GetType().GetProperty(propertyName);
            object v;
            if (pc == null)
            {
                var mi = o.GetType().GetMethod("get_" + propertyName);
                if (mi == null || mi.GetParameters().Length != 0)
                    return false;
                v = mi.Invoke(o, new object[0]);
            }
            else
            {
                v = pc.GetValue(o);
            }
            if (v is T tv)
            {
                value = tv;
                return true;
            }
            return false;
        }
        public static Type FindGenericBaseType(this Type type, Type genericType)
        {
            if (type == null || genericType == null)
                return null;

            if (type == genericType)
                return type;

            // See if any of the base types implement the type
            while (type != null && type != typeof(object))
            {
                Type currentType = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
                if (genericType == currentType)
                    return currentType;

                // See if any of the interfaces implement the type
                Type interfaceType = type.GetInterfaces()
                    .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == genericType);

                if (interfaceType != null)
                    return interfaceType;
                type = type.BaseType;
            }
            return null;
        }

        private static Dictionary<Type, Func<object, object>> _converters = new Dictionary<Type, Func<object, object>>();
        public static object CastToType(object obj, Type type)
        {
            if(!_converters.TryGetValue(type, out var fn))
            {
                var body = Expression.Convert(Expression.Constant(obj), type);
                var expr = Expression.Lambda<Func<object, object>>(body, Expression.Parameter(typeof(object), "obj"));
                fn = expr.Compile();
                _converters[type] = fn;
            }
            return fn(obj);
        }

        public static Type GetDefaultInterface(Type type)
        {
            var at = type.GetCustomAttribute<ComDefaultInterfaceAttribute>();
            return at?.Value;
        }
        public static bool IsAssemblyDebugBuild(this Assembly assembly)
        {
            return assembly.GetCustomAttributes(false).OfType<DebuggableAttribute>().Any(da => da.IsJITTrackingEnabled);
        }

        public static string GetDescription(this MemberInfo mi)
        {
            return mi.GetCustomAttribute<DescriptionAttribute>()?.Description;
        }

        public static bool IsImplementingInterface<T>(this MethodInfo m)
        {
            return m.IsImplementingInterface(typeof(T));
        }
        public static bool IsImplementingInterface(this MethodInfo m, Type interfaceType)
        {
            if (!m.DeclaringType.GetInterfaces().Any(i => interfaceType.MetadataToken == i.MetadataToken))
                return false;
            if (m.DeclaringType.IsInterface)
                return true;
            var im = m.DeclaringType.GetInterfaceMap(interfaceType);
            //return im.TargetMethods.Contains(m);
            return im.TargetMethods.Any(tm => tm.MetadataToken == m.MetadataToken);
        }

    }
}