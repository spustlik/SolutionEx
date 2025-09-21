using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SolutionExtensions.Reflector
{
    public delegate void DumperDelegate(object o, XElement parent);
    public delegate void DumperDelegate<T>(T o, XElement parent);
    public class ReflectionXmlDumper
    {
        public ReflectionXmlDumper(params Type[] skipTypes)
        {
            this.skipTypes = new HashSet<Type>(skipTypes);
        }
        //warn about subclasses
        private HashSet<Type> skipTypes { get; }

        private HashSet<Type> usedTypes = new HashSet<Type>();
        private Dictionary<Type, DumperDelegate> dumpers = new Dictionary<Type, DumperDelegate>();
        public ReflectionCOM ComReflection { get; } = new ReflectionCOM();
        private ReflectionBuilderCS builder = new ReflectionBuilderCS();
        public void AddDumper(Type type, DumperDelegate action)
        {
            dumpers[type] = action;
        }
        public void AddDumper<T>(DumperDelegate<T> action)
        {
            AddDumper(typeof(T), (v, e) => action((T)v, e));
        }
        private DumperDelegate FindDumper(Type type)
        {
            if (dumpers.TryGetValue(type, out var result))
                return result;
            //try search for base type
            if (type.BaseType != null && type.BaseType != typeof(object))
            {
                result = FindDumper(type.BaseType);
            }
            //try search for interfaces
            if (result == null)
            {
                var interfaces = type.GetInterfaces();
                foreach (var iface in interfaces)
                {
                    result = FindDumper(iface);
                    if (result != null)
                        break;
                }
            }
            //remember also that not found
            dumpers[type] = result;
            return result;
        }

        public void DumpReflectedValue(object o, XElement parent, HashSet<object> knownObjects, int depth)
        {
            //metody presunout nekam mimo
            //umoznit known types , pouzit i na URL
            /*
                        <_Item _type="Microsoft.VisualStudio.ProjectSystem.VS.Implementation.Package.Automation.OAProperty" 
            _interfaces="EnvDTE.Property">
                          <_Property Name="Application" _isNull="true" />
                          <_Property Name="Collection" _valueType="Microsoft.VisualStudio.ProjectSystem.VS.Implementation.Package.Automation.OAProperties" _type="Microsoft.VisualStudio.ProjectSystem.VS.Implementation.Package.Automation.OAProperties" _skipped="referenced" />
                          <_Property Name="DTE" _valueType="__ComObject" _type="__ComObject" _skipped="referenced" />
                          <_Property Name="Name" _type="String" Value="Link" />
                          <_Property Name="NumIndices" _type="Int16" Value="0" />
                          <_Property Name="Object" _valueType="Microsoft.VisualStudio.ProjectSystem.VS.Implementation.PropertyPages.DynamicTypeBrowseObject" _type="Microsoft.VisualStudio.ProjectSystem.VS.Implementation.PropertyPages.DynamicTypeBrowseObject" _skipped="referenced" />
                          <_Property Name="Parent" _valueType="Microsoft.VisualStudio.ProjectSystem.VS.Implementation.Package.Automation.OAProperties" _type="Microsoft.VisualStudio.ProjectSystem.VS.Implementation.Package.Automation.OAProperties" _skipped="referenced" />
                          <_Property Name="Value" _valueType="String" _type="String" Value="" />
                          <_Method Name="set_IndexedValue" signature="set_IndexedValue(Object index1Object index2Object index3Object index4Object value)" />
                          <_Method Name="get_IndexedValue" signature="get_IndexedValue(Object index1Object index2Object index3Object index4): Object" _returnType="Object" />
                          <_Method Name="let_Value" signature="let_Value(Object value)" />
                        </_Item>             
            */
            if (o == null)
            {
                parent.Add(new XAttribute("_isNull", true));
                return;
            }
            parent.Add(new XAttribute("_type", GetTypeName(o.GetType())));
            if (skipTypes.Contains(o.GetType()))
            {
                parent.Add(new XAttribute("_skipped", "skip type"));
                return;
            }
            if (o is Task)
            {
                parent.Add(new XAttribute("_skipped", "task"));
                return;
            }
            if (o is string || o.GetType().IsPrimitive)
            {
                parent.Add(new XAttribute("Value", o));
                return;
            }
            var dumper = FindDumper(o.GetType());
            if (dumper != null)
            {
                dumper(o, parent);
                return;
            }
            if (knownObjects.Contains(o))
            {
                parent.Add(new XAttribute("_skipped", "referenced"));
                return;
            }
            knownObjects.Add(o);
            if (knownObjects.Count > 100 || depth > 8)
            {
                parent.Add(new XAttribute("_skipped", $"too deep ({depth})"));
                return;
            }
            var interfaces = o.GetType().GetInterfaces();
            if (interfaces.Length > 0)
            {
                foreach (var intf in interfaces) usedTypes.Add(intf);
                parent.Add(new XAttribute("_interfaces", string.Join(", ", interfaces.Select(i => GetTypeName(i)))));
            }
            if (ReflectionCOM.IsCOMObjectType(o.GetType()))
            {
                var com = ComReflection.GetInterfaces(o).ToArray();
                if (com.Length > 0)
                {
                    foreach (var intf in com) usedTypes.Add(intf);
                    parent.Add(new XAttribute("_COMinterfaces", string.Join(", ", com.Select(i => GetTypeName(i)))));
                }
            }
            if (o is IEnumerable a)
            {
                DumpEnumerable(o, parent, knownObjects, depth, a);
                //dump also props & methods
            }
            DumpProperties(parent, o, knownObjects, depth);
            usedTypes.Add(o.GetType());
        }

        private string GetTypeName(Type type)
        {
            return builder.GetTypeName(type);
        }

        public void DumpUsedTypes(XElement parent)
        {
            if (usedTypes.Count == 0)
                return;
            var e = new XElement("UsedTypes");
            parent.Add(e);
            foreach (var t in usedTypes)
            {
                var te = new XElement("Type",
                    new XAttribute("_type", GetTypeName(t)),
                    new XAttribute("_assembly", t.Assembly.GetName().Name),
                    new XAttribute("_location", t.Assembly.Location)
                );
                e.Add(te);
                if (t.GUID != Guid.Empty)
                    te.Add(new XAttribute("GUID", t.GUID));
                DumpMethods(te, t);
                DumpPropertiesOfType(te, t);
            }

        }

        public void DumpPropertiesOfType(XElement parent, Type type)
        {
            foreach (var pi in type.GetProperties())
            {
                var pe = new XElement("_Property", new XAttribute("Name", pi.Name), new XAttribute("_type", GetTypeName(pi.PropertyType)));
                if (!pi.CanWrite) { pe.Add(new XAttribute("readonly", true)); }
                parent.Add(pe);
            }
        }

        public void DumpMethods(XElement parent, Type type)
        {
            foreach (var m in type.GetMethods())
            {
                if (m.DeclaringType.Namespace.StartsWith(nameof(System)) ||
                    //m.DeclaringType == typeof(object) ||
                    //    m.DeclaringType.FullName == "System.__ComObject" ||
                    //    m.DeclaringType == typeof(MarshalByRefObject) ||
                    //    m.DeclaringType == typeof(Array) ||
                    IsImplementingInterface(m, typeof(IDisposable)) ||
                    IsImplementingInterface(m, typeof(IEnumerable))
                    )
                    continue;
                //if (m.IsSpecialName)
                //    continue;
                var me = new XElement("_Method", new XAttribute("Name", m.Name), new XAttribute("signature", builder.BuildMethodSignature(m)));
                if (m.ReturnType != typeof(void))
                    me.Add(new XAttribute("_returnType", GetTypeName(m.ReturnType)));
                parent.Add(me);
            }
        }

        public void DumpProperties(XElement parent, object o, HashSet<object> knownObjects, int depth)
        {
            foreach (var pi in o.GetType().GetProperties())
            {
                var pe = new XElement("_Property", new XAttribute("Name", pi.Name));
                parent.Add(pe);
                try
                {
                    var value = pi.GetValue(o, null);
                    var valueType = value?.GetType();
                    if (value != null && valueType.MetadataToken != pi.PropertyType.MetadataToken)
                    {
                        pe.Add(new XAttribute("_valueType", GetTypeName(valueType)));
                    }
                    DumpReflectedValue(value, pe, knownObjects, depth + 1);
                }
                catch (Exception ex)
                {
                    pe.Add(new XAttribute("_error", ex.Message));
                }
            }
        }

        private void DumpEnumerable(object o, XElement parent, HashSet<object> knownObjects, int depth, IEnumerable a)
        {
            parent.Add(new XAttribute("_enumerable", o.GetType()));
            if (o.GetType().HasElementType)
            {
                parent.Add(new XAttribute("_elementType", GetTypeName(o.GetType().GetElementType())));
            }
            foreach (var item in a)
            {
                var ae = new XElement("_Item");
                parent.Add(ae);
                DumpReflectedValue(item, ae, knownObjects, depth + 1);
            }
        }

        public XElement DumpUntypedObject(object o, string name, HashSet<object> knownObjects = null, int depth = 0)
        {
            if (knownObjects == null)
                knownObjects = new HashSet<object>();
            var e = new XElement("_ReflectedObject", new XAttribute("Name", name));
            DumpReflectedValue(o, e, knownObjects, depth);
            return e;
        }


        public bool IsImplementingInterface(MethodInfo m, Type interfaceType)
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