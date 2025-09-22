using System;
using System.Linq;

namespace SolutionExtensions.Reflector
{
    public abstract class ReflectorNodeBuilder<TR>
    {
        public virtual TR Build(ReflectorNode node)
        {
            return BuildNode(node);
        }

        public TR BuildNodeRec(ReflectorNode parentNode,
            Action<(TR parent, TR child, ReflectorNode parentNode, ReflectorNode childNode)> adder)
        {
            TR parent = BuildNode(parentNode);
            foreach (var childNode in parentNode.Children)
            {
                var child = BuildNodeRec(childNode, adder);
                adder((parent, child, parentNode, childNode));
            }
            return parent;
        }
        public TR BuildNodeDeep(ReflectorNode root, Action<TR, TR[]> add)
        {
            var childResults = root.Children.Select(node => BuildNodeDeep(node, add)).ToArray();
            var r = BuildNode(root);
            add(r, childResults);
            return r;
        }
        public TR BuildNode(ReflectorNode node)
        {
            switch (node)
            {
                case ReflectorPropertyValue propNode:
                    return BuildProperty(propNode);
                case ReflectorMethod methodNode:
                    return BuildMethod(methodNode);
                case ReflectorEnumItem enumNode:
                    return BuildEnum(enumNode);
                case ReflectorInterface interfaceNode:
                    return BuildInterface(interfaceNode);
                case ReflectorRoot rootNode:
                    return BuildRoot(rootNode);
                default:
                    throw new NotImplementedException();
            }
        }

        protected abstract TR BuildRoot(ReflectorRoot node);
        protected abstract TR BuildInterface(ReflectorInterface node);
        protected abstract TR BuildEnum(ReflectorEnumItem node);
        protected abstract TR BuildMethod(ReflectorMethod node);
        protected abstract TR BuildProperty(ReflectorPropertyValue node);
    }
}
