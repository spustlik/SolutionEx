using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace SolutionExtensions.UI.Themes
{
    //just helper to dump used resources from VS WPF to simulator
    internal class ThemeDumper
    {
        private readonly FrameworkElement resElement;
        private readonly StringBuilder sb = new StringBuilder();
        public string Result => sb.ToString();
        public ThemeDumper(FrameworkElement resElement)
        {
            this.resElement = resElement;
        }
        internal void Dump(string name, object value)
        {
            var r = resElement.TryFindResource(value);
            if (r is SolidColorBrush brush)
                sb.AppendLine($"<SolidColorBrush x:Key=\"{GetKey(name)}\" Color=\"{brush.Color}\"/>");
            else if (r is Color color)
                sb.AppendLine($"<Color x:Key=\"{GetKey(name)}\">{color}</Color>");
            else if (r is Style style)
                DumpStyle(style, name, useKey: true);
            else
                sb.AppendLine($"<!-- {name}={value} : {(r ?? "(null)")} -->");
        }

        private string GetKey(string name)
        {
            return $"{{x:Static themes:ThemeKeys.{name}}}";
        }

        private void DumpStyle(Style style, string name, bool useKey)
        {
            string baseKey = null;
            if (style.BasedOn != null)
            {
                baseKey = name + "_base";
                DumpStyle(style.BasedOn, baseKey, useKey: false);
            }
            DumpStyleObject(style, useKey ? GetKey(name) : name, baseKey);
        }
        private void DumpStyleObject(Style style, string key, string basedOnKey)
        {
            var props = new List<PropertyValue>
            {
                new PropertyValue("x:Key", key),
                new PropertyValue("TargetType", style.TargetType?.Name),
                new PropertyValue("BasedOn", $"{{StaticResource {basedOnKey}}}",isNull:basedOnKey==null),
                new PropertyValue("", style.Setters.Select(s => GetDumpSetter(s, style.TargetType))),
                new PropertyValue("Triggers", style.Triggers.Select(t => GetDumpTrigger(t, style.TargetType))),
                new PropertyValue("Resources", GetDumpResources(style.Resources), isContent:true),
            };
            AppendElement(sb, "Style", props);
        }

        private string GetDumpResources(ResourceDictionary resources)
        {
            if (resources.Count == 0 && resources.MergedDictionaries.Count == 0)
                return null;
            return XamlWriter.Save(resources);
        }

        private string GetDumpTrigger(TriggerBase obj, Type targetType)
        {
            //obj.EnterActions,ExitActions - not in xaml
            var sb = new StringBuilder();
            if (obj is Trigger trigger)
            {
                AppendElement(sb, "Trigger", new[]
                {
                    new PropertyValue("Property", GetPropertyName(trigger.Property, targetType)),
                    new PropertyValue("SourceName", trigger.SourceName),
                    new PropertyValue("Value", GetValue(trigger.Value)),
                    new PropertyValue("Setters", trigger.Setters.Select(s=>GetDumpSetter(s, trigger.SourceName==null?null:targetType))),
                });
                return sb.ToString();
            }
            if (obj is DataTrigger dataTrigger)
            {
                AppendElement(sb, "DataTrigger", new[]
                {
                    new PropertyValue("Binding", GetValue(dataTrigger.Binding)),
                    new PropertyValue("Value", GetValue(dataTrigger.Value)),
                    new PropertyValue("Setters", dataTrigger.Setters.Select(s=>GetDumpSetter(s,targetType))),
                });
                return sb.ToString();
            }
            if (obj is MultiTrigger multiTrigger)
            {

            }
            return XamlWriter.Save(obj);
        }

        private string GetPropertyName(DependencyProperty property, Type type)
        {
            //on setter can be of another type - using targettype
            //throw new NotImplementedException();
            if (type != null && property.OwnerType.IsAssignableFrom(type))
                return property.Name;
            return property.OwnerType.Name + "." + property.Name;
        }

        private string GetDumpSetter(SetterBase obj, Type targetType)
        {
            var sb = new StringBuilder();
            if (obj is Setter setter)
            {
                AppendElement(sb, "Setter", new[]
                {
                    new PropertyValue("Property", GetPropertyName(setter.Property, setter.TargetName==null ? targetType:null)),
                    new PropertyValue("TargetName", setter.TargetName),
                    new PropertyValue("Value", GetValue(setter.Value)),
                });
                return sb.ToString();
            }
            if (obj is EventSetter eventSetter)
            {
                AppendElement(sb, "EventSetter", new[]
                {
                    new PropertyValue("Event", eventSetter.Event.Name),
                    new PropertyValue("Handler", eventSetter.Handler?.ToString()),
                });
                return sb.ToString();
            }
            return XamlWriter.Save(obj);
        }

        private string GetValue(object value)
        {
            if (value == null)
                return "{x:Null}";
            if (value is DynamicResourceExtension ex)
            {
                var r = resElement.TryFindResource(ex.ResourceKey);
                if (r == null)
                    return $"{{DynamicResource {ex.ResourceKey}}}";
                return GetValue(r);
            }
            if (value is Binding b)
            {
                var c = TypeDescriptor.GetConverter(b.Path);
                var s = c.ConvertToString(b.Path);
                return $"{{Binding {s}}}";
            }
            return value.ToString();
        }


        private static void AppendElement(StringBuilder sb, string elementName, IEnumerable<PropertyValue> props)
        {
            sb.Append("<").Append(elementName);
            foreach (var prop in props.Where(p => !p.IsContent && !String.IsNullOrEmpty(p.Value)))
            {
                sb.Append(" ")
                    .Append(prop.Name)
                    .Append('=')
                    .Append('"').Append(prop.Value).Append('"');
            }
            var contentProps = props.Where(p => p.IsContent && !String.IsNullOrEmpty(p.Value)).ToArray();
            if (contentProps.Length == 0)
            {
                sb.AppendLine("/>");
                return;
            }
            sb.AppendLine(">");
            foreach (var prop in contentProps)
            {
                var indent = 1;
                var name = prop.Name;
                var isDefault = String.IsNullOrEmpty(name);
                if (!isDefault && !name.Contains("."))
                    name = elementName + "." + name;

                if (!isDefault)
                {
                    Indent(sb, indent).Append("<").Append(name).AppendLine(">");
                    indent++;
                }
                var lines = prop.Value.Split('\n');
                foreach (var s in lines)
                    Indent(sb, indent).AppendLine(s.TrimEnd());
                if (!isDefault)
                {
                    indent--;
                    Indent(sb, indent).Append("</").Append(name).AppendLine(">");
                }
            }
            sb.Append("</").Append(elementName).AppendLine(">");
        }

        private static StringBuilder Indent(StringBuilder sb, int count)
        {
            for (var i = 0; i < count; i++)
                sb.Append("  ");
            return sb;
        }
    }
    internal struct PropertyValue
    {
        public string Name;
        public string Value;
        public bool IsContent;

        public PropertyValue(string name, IEnumerable<string> values)
        {
            Name = name;
            Value = String.Join("\n", values.Select(v => v.TrimEnd()));
            IsContent = true;
        }

        public PropertyValue(string name, string value, bool isContent = false, bool isNull = false)
        {
            Name = name;
            Value = value;
            if (isNull)
                Value = null;
            IsContent = isContent;
        }
    }
}
