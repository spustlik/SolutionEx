using SolutionExtensions.Reflector;

namespace SolutionExtensions
{
    public class ReflectorTextBuilder : ReflectorNodeBuilder<string>
    {
        public ReflectionBuilderCS Builder { get; }
        public ReflectorTextBuilder(ReflectionBuilderCS builder)
        {
            Builder = builder;
        }

        protected override string BuildEnum(ReflectorEnumItem node)
        {
            var s = $"[{node.Index}]";
            if (!string.IsNullOrEmpty(node.ItemText))
                s += $" {node.ItemText}";
            s += $" {node.ValueTypeName}";
            if (node.ValueSimpleText != null)
                s += $" {node.ValueSimpleText}";
            return s;
        }

        protected override string BuildInterface(ReflectorInterface node)
        {
            return $"interface {node.ValueType.FullName} {node.ValueType.GUID:B}";
        }

        protected override string BuildMethod(ReflectorMethod node)
        {
            return Builder.BuildMethodSignature(node.MethodInfo, returnTypeAtEnd: false);
        }

        protected override string BuildProperty(ReflectorPropertyValue node)
        {
            return node.HasValue ?
                $"{node.PropertyName} = {node.Value}" :
                $"{node.PropertyName}: {node.ValueType?.FullName ??"(null)"}";
        }

        protected override string BuildRoot(ReflectorRoot node)
        {
            return $"{node.RootType}: {node.ValueTypeName}";
        }

    }
}