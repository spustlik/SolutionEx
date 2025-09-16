using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SolutionExtensions.Reflector
{
    public class ReflectionCOM
    {
        public static bool IsCOMObjectType(Type t)
        {
            return t.Namespace == nameof(System) && t.Name == "__ComObject";
        }
        private Dictionary<Guid, Type> knownInterfaces = new Dictionary<Guid, Type>();
        public void RegisterInterfaces(Assembly assembly)
        {
            try
            {
                var types = assembly.GetTypes();
                foreach (var t in types)
                {
                    if (!t.IsInterface || t.GUID == Guid.Empty)
                        continue;
                    if (!knownInterfaces.ContainsKey(t.GUID))
                    {
                        knownInterfaces.Add(t.GUID, t);
                    }
                }
            }
            catch (Exception)
            {
                //ignore
            }
        }
        public void RegisterInterfacesFromAppDomain()
        {
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                RegisterInterfaces(a);
            }
        }
        public IEnumerable<Type> GetInterfaces(object comObject)
        {
            var iunknown = Marshal.GetIUnknownForObject(comObject);
            foreach (var pair in knownInterfaces)
            {
                var iid = pair.Key;
                var hr = Marshal.QueryInterface(iunknown, ref iid, out var ipointer);
                if (hr == 0 && ipointer != IntPtr.Zero)
                    yield return pair.Value; //Type of interface
            }
        }
        public static object QueryInterface(object comObject, Type interfaceType)
        {
            var iid = interfaceType.GUID;
            return QueryInterface(comObject, iid);
        }

        public static object QueryInterface(object comObject, Guid iid)
        {
            var iunknown = Marshal.GetIUnknownForObject(comObject);
            var hr = Marshal.QueryInterface(iunknown, ref iid, out var ipointer);
            if (hr == 0 && ipointer != IntPtr.Zero)
                return Marshal.GetObjectForIUnknown(ipointer);
            return null;
        }

        public static T QueryInterface<T>(object comObject) where T : class
        {
            return QueryInterface(comObject, typeof(T)) as T;
        }

    }
}
