using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflector.Formula
{
    public class ReflectionBinder
    {
        public object Evaluate(object root, string formula)
        {
            var syntax = new Syntax<SyntaxNode>() { Builder = new SyntaxTreeBuilder() };
            var node = syntax.Analyze(formula);
            return Evaluate(root, node);
        }
        public object Evaluate(object root, SyntaxNode expression)
        {
            if (expression == null) return null;
            switch (expression)
            {
                case SyntaxNodeMethodCall mc:
                    return EvaluateMethodCall(root, mc);
                case SyntaxNodeProperty prop:
                    return EvaluateProperty(root, prop);
                case SyntaxNodeValue value:
                    return EvaluateValue(value);
                default:
                    throw new InvalidOperationException($"Unknown syntax node {expression.GetType().Name}");
            }
        }

        private object EvaluateValue(SyntaxNodeValue value)
        {
            return value.Value;
        }

        private object EvaluateProperty(object root, SyntaxNodeProperty prop)
        {
            var obj = root;
            if (prop.Value != null)
                obj = Evaluate(root, prop.Value);
            var pi = root.GetType().GetProperty(prop.PropertyName);
            if (prop.Index != null)
            {
                var index = Evaluate(root, prop.Index);
                return pi.GetValue(obj, new[] { index });
            }
            return pi.GetValue(obj);
        }

        private object EvaluateMethodCall(object root, SyntaxNodeMethodCall method)
        {
            var obj = root;
            if (method.Value != null)
                obj = Evaluate(root, method.Value);
            var mi = obj.GetType().GetMethod(method.MethodName);
            if (mi == null)
                throw new InvalidOperationException($"Cannot find method '{method.MethodName}' in object '{obj}'");
            var parameters = mi.GetParameters().ToList();
            var args = new object[parameters.Count];
            if (method.Arguments != null)
            {
                for (int i = 0; i < method.Arguments.Arguments.Count; i++)
                {
                    var arg = method.Arguments.Arguments[i];
                    var argIndex = i;
                    if (!string.IsNullOrEmpty(arg.ArgumentName))
                    {
                        argIndex = parameters.FindIndex(x => x.Name == arg.ArgumentName);
                        if (argIndex < 0)
                            throw new InvalidOperationException($"Cannot find parameter named '{arg.ArgumentName}' of method {mi.Name}'");
                    }
                    var argValue = Evaluate(root, arg.Argument);
                    args[argIndex] = argValue;
                }
            }
            return mi.Invoke(obj, args);
        }
    }
}
