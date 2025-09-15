using SolutionExtensions.Reflector;
using System;
using System.Xml.Linq;

namespace SolutionExtensions
{
    public class ReflectorXmlBuilder : ReflectorNodeBuilder<XElement>
    {
        public ReflectionBuilderCS Builder { get; } = new ReflectionBuilderCS();
        public ReflectorXmlBuilder(ReflectionBuilderCS builder)
        {
            Builder = builder;
        }

        public override XElement Build(ReflectorNode node)
        {
            return BuildNodeRec(node, (args) =>
            {
                args.parent.Add(args.child);
            });
        }
        protected override XElement BuildEnum(ReflectorEnumItem node)
        {
            var ele = new XElement("Item", 
                new XAttribute("Index", node.Index),
                GetTypeAttr(node));
            if (!String.IsNullOrEmpty(node.ItemDefaultName))
                ele.Add(new XAttribute("_" + node.ItemDefaultName, node.ItemDefaultValue));                
            return ele;

        }

        protected override XElement BuildInterface(ReflectorInterface node)
        {
            var ele = new XElement("Interface",
                GetTypeAttr(node),
                new XAttribute("GUID", node.ValueType.GUID));
            return ele;
        }

        protected override XElement BuildMethod(ReflectorMethod node)
        {
            var ele = new XElement("Method",
                new XAttribute("Signature", Builder.BuildMethodSignature(node.MethodInfo, returnTypeAtEnd: false)));
            return ele;
        }

        protected override XElement BuildProperty(ReflectorPropertyValue node)
        {
            var ele = new XElement("Property",
                new XAttribute("Name", node.PropertyName),
                GetTypeAttr(node));
            if (node.Value == null)
                ele.Add(new XAttribute("isNull", true));
            if (node.Value != null && node.IsSimpleType)
                ele.Add(new XAttribute("Value", node.Value));
            return ele;
        }

        protected override XElement BuildRoot(ReflectorRoot node)
        {
            return new XElement(node.RootType, GetTypeAttr(node));
        }

        private static XAttribute GetTypeAttr(ReflectorTypeNode node)
        {
            if (node.ValueType == null)
                return null;
            return new XAttribute("type", node.ValueTypeName);
        }

    }
}