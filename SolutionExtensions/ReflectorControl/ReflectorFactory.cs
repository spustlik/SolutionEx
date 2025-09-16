using SolutionExtensions.Reflector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace SolutionExtensions
{
    public class ReflectorFactory
    {
        public ReflectionCOM COM { get; }
        public ReflectionBuilderCS Builder { get; }
        public ReflectorXmlBuilder BuilderXml { get; }
        public ReflectorTextBuilder BuilderText { get; }
        public ReflectorFactory()
        {
            COM = new ReflectionCOM();
            Builder = new ReflectionBuilderCS();
            BuilderXml = new ReflectorXmlBuilder(Builder);
            BuilderText = new ReflectorTextBuilder(Builder);
        }
        public ReflectorRoot CreateRoot(string rootType, object value)
        {
            var root = new ReflectorRoot() { RootType = rootType };
            SetValue(root, value);
            return root;
        }
        private void SetValue(ReflectorValueNode node, object value, Type valueType = null)
        {
            node.Value = value;
            if (value == null)
            {
                node.IsNull = true;
                node.IsSimpleType = true;
                return;
            }
            node.IsNull = false;
            SetValueType(node, valueType ?? value.GetType());
            if (!node.IsSimpleType)
            {
                node.CanExpandEnumerable = node.Value is IEnumerable;
                if (node.CanExpandEnumerable)
                {
                    var pi = valueType.GetProperty("Count");
                    if (pi != null)
                        node.ValueSimpleText = pi.GetValue(node.Value).ToString();
                }
            }
            else
            {
                node.ValueSimpleText = Builder.BuildLiteral(value);
            }
        }
        private void SetValueType(ReflectorTypeNode node, Type valueType)
        {
            node.ValueType = valueType;
            node.ValueTypeName = Builder.GetTypeName(node.ValueType);
            node.IsSimpleType = node.ValueType == typeof(string) ||
                node.ValueType.IsPrimitive ||
                node.ValueType.IsEnum; //TODO: expand/generate enum to members
            var isCOM = ReflectionCOM.IsCOMObjectType(node.ValueType);
            if (!node.IsSimpleType)
            {
                node.CanExpandMethods = !isCOM && node.ValueType.GetMethods().Length > 0;
                node.CanExpandProperties = !isCOM && node.ValueType.GetProperties().Length > 0;
                node.CanExpandInterfaces = node.ValueType.GetInterfaces().Length > 0 || isCOM;
            }
        }
        public void ExpandMethods(ReflectorTypeNode parent)
        {
            if (!parent.CanExpandMethods)
                return;
            parent.CanExpandMethods = false;
            foreach (var mi in parent.ValueType.GetMethods().OrderBy(x => x.Name))
            {
                //if (mi.DeclaringType != parent.ValueType)
                //    continue;
                if (mi.IsPrivate)
                    continue;
                if (mi.IsSpecialName) //get/set
                    continue;
                var node = new ReflectorMethod()
                {
                    MethodInfo = mi,
                    MethodName = mi.Name,
                    Signature = Builder.BuildMethodSignature(mi, returnTypeAtEnd: true)
                };
                parent.Children.Add(node);
            }
        }
        public void ExpandProperties(ReflectorTypeNode parent)
        {
            if (!parent.CanExpandProperties)
                return;
            parent.CanExpandProperties = false;
            foreach (var pi in parent.ValueType.GetProperties().OrderBy(x => x.Name))
            {
                //if (pi.DeclaringType != parent.ValueType)
                //    continue;
                //var acc = pi.GetAccessors(nonPublic: false);
                bool isStatic = (pi.GetGetMethod() ?? pi.GetSetMethod())?.IsStatic == true;
                bool isPrivateGet = pi.GetGetMethod()?.IsPrivate != false;
                bool isPrivateSet = pi.GetSetMethod()?.IsPrivate != false;
                if (isPrivateGet)
                    continue;
                var modifiers = new List<string>();
                if (isStatic)
                    modifiers.Add("static");
                if (isPrivateSet)
                    modifiers.Add("readonly");
                var node = new ReflectorPropertyValue()
                {
                    PropertyInfo = pi,
                    PropertyModifiers = String.Join(" ", modifiers),
                    PropertyName = pi.Name,
                    PropertyType = pi.PropertyType,
                    PropertyTypeName = Builder.GetTypeName(pi.PropertyType),
                };
                if (parent is ReflectorValueNode parentv && !parentv.IsNull)
                {
                    try
                    {
                        var propValue = pi.GetValue(parentv.Value);
                        SetValue(node, propValue);
                        node.HasValue = true;
                    }
                    catch (Exception ex)
                    {
                        AddError(node, ex);
                    }
                }
                parent.Children.Add(node);
            }
        }
        public void ExpandEnumerable(ReflectorValueNode parent)
        {
            if (!parent.CanExpandEnumerable || !(parent.Value is IEnumerable))
                return;
            parent.CanExpandEnumerable = false;
            int index = 0;
            try
            {
                Type realItemType = null;
                PropertyInfo realDefProperty = null;
                if (parent is ReflectorPropertyValue pv)
                {
                    var parentType = pv.PropertyType;
                    var defMember = parentType.GetCustomAttribute<DefaultMemberAttribute>();
                    if (defMember != null)
                    {
                        var mi = parentType.GetMethod(defMember.MemberName);
                        if (mi != null)// && mi.GetParameters().Length == 1)
                        {
                            realItemType = mi.ReturnType;
                            defMember = realItemType.GetCustomAttribute<DefaultMemberAttribute>();
                            if (defMember != null)
                                realDefProperty = realItemType.GetProperty(defMember.MemberName);
                        }
                    }
                }
                foreach (var item in parent.Value as IEnumerable)
                {
                    var node = new ReflectorEnumItem()
                    {
                        Index = index++,
                    };
                    var obj = item;
                    //if (realItemType != null)
                    //    obj = ReflectionHelper.Cast(obj, realItemType);
                    SetValue(node, obj, realItemType);
                    if (realItemType != null)
                    {
                        node.CanExpandInterfaces = true;
                        node.ItemDefaultName = realDefProperty.Name;
                        node.ItemDefaultValue = realDefProperty.GetValue(obj) + "";
                    }
                    parent.Children.Add(node);
                }
            }
            catch (Exception ex)
            {
                AddError(parent, ex);
            }
        }
        public void ExpandInterfaces(ReflectorTypeNode parent)
        {
            if (!parent.CanExpandInterfaces)
                return;
            parent.CanExpandInterfaces = false;
            var interfaces = parent.ValueType.GetInterfaces()
                .Select(i => new { IsCOM = false, Type = i }).ToList();
            object value = null;
            var defaultInterface = ReflectionHelper.GetDefaultInterface(parent.ValueType);
            if (parent is ReflectorValueNode parentv && !parentv.IsNull)
            {
                value = parentv.Value;
                //add known interaces implemented by COM object and replace type-declared
                var com = COM.GetInterfaces(parentv.Value)
                    .Select(i => new { IsCOM = true, Type = i }).ToArray();
                foreach (var i in com)
                {
                    var found = interfaces.FirstOrDefault(x => x.Type == i.Type);
                    if (found != null)
                        interfaces.Remove(found);
                    if (defaultInterface == null)
                        defaultInterface = ReflectionHelper.GetDefaultInterface(i.Type);
                }
                interfaces.AddRange(com);
            }

            foreach (var x in interfaces
                .OrderBy(x => x.Type == defaultInterface ? 0 : 1)
                .ThenBy(x => x.Type.FullName))
            {
                if (parent.ValueType == x.Type)
                    continue;
                var node = new ReflectorInterface()
                {
                    IsCOM = x.IsCOM,
                };
                SetValue(node, value);
                node.ValueType = x.Type;
                node.ValueTypeName = Builder.GetTypeName(x.Type);
                node.CanExpandMethods = true;
                node.CanExpandProperties = true;

                parent.Children.Add(node);
            }

        }
        public void AddError(ReflectorNode node, Exception ex)
        {
            if (!string.IsNullOrEmpty(node.Error))
            {
                node.Error += "\n";
            }
            node.Error += ex.Message;
        }
        public void ClearChildren(ReflectorNode node)
        {
            node.Children.Clear();
            node.CanExpandEnumerable = true;
            node.CanExpandInterfaces = true;
            node.CanExpandMethods = true;
            node.CanExpandProperties = true;
        }
        public string BuildNodeSource(ReflectorNode node)
        {
            switch (node)
            {
                case ReflectorPropertyValue propNode:
                    //property with value ->class of value type
                    if (propNode.ValueType != null)
                        return Builder.BuildDeclaration(propNode.ValueType);
                    else
                        //property without value - property type, no:containing class (pi.declaringtype)
                        return Builder.BuildDeclaration(propNode.PropertyInfo.PropertyType);
                case ReflectorMethod methodNode:
                    return Builder.BuildDeclaration(methodNode.MethodInfo.DeclaringType);
                case ReflectorTypeNode typeNode:
                    return Builder.BuildDeclaration(typeNode.ValueType);
                default:
                    return null;
            }
        }
        public XElement BuildNodeXml(ReflectorVM vm)
        {
            var rootNode = vm.Children.OfType<ReflectorRoot>().FirstOrDefault();
            if (rootNode == null)
                throw new InvalidOperationException("Empty tree");
            return BuilderXml.Build(rootNode);
        }

    }
}
