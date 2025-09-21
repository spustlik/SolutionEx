using System.Collections.Generic;

namespace SolutionExtensions.Formula
{
    public class SyntaxTreeBuilder : ISyntaxResultBuilder<SyntaxNode>
    {
        public SyntaxNode CreateArgsList(SyntaxNode arg, string name) => new SyntaxNodeArgs(new SyntaxNodeArg(arg, name));

        public SyntaxNode CreateArgsList(SyntaxNode list, SyntaxNode append)
        {
            var args = (SyntaxNodeArgs)list;
            args.Arguments.AddRange(((SyntaxNodeArgs)append).Arguments);
            return args;
        }
        public SyntaxNode CreateMethodCall(SyntaxNode value, string method, SyntaxNode args)
            => new SyntaxNodeMethodCall(value, method, (SyntaxNodeArgs)args);
        public SyntaxNode CreateProperty(SyntaxNode left, string name, SyntaxNode index)
            => new SyntaxNodeProperty(left, name, index);
        public SyntaxNode CreateValue(bool value) => new SyntaxNodeValue(value);
        public SyntaxNode CreateNumberValue(object value) => new SyntaxNodeValue(value);
        public SyntaxNode CreateValue(int value) => new SyntaxNodeValue(value);
        public SyntaxNode CreateValue(string s) => new SyntaxNodeValue(s);
        public SyntaxNode CreateNull() => new SyntaxNodeValue(null);

    }

    public abstract class SyntaxNode
    {
    }
    public class SyntaxNodeMethodCall : SyntaxNode
    {
        public SyntaxNode Value;
        public string MethodName;
        public SyntaxNodeArgs Arguments;

        public SyntaxNodeMethodCall(SyntaxNode value, string method, SyntaxNodeArgs args)
        {
            this.Value = value;
            this.MethodName = method;
            this.Arguments = args;
        }
    }

    public class SyntaxNodeArgs : SyntaxNode
    {
        public List<SyntaxNodeArg> Arguments { get; } = new List<SyntaxNodeArg>();
        public SyntaxNodeArgs(SyntaxNodeArg arg)
        {
            Arguments.Add(arg);
        }
    }

    public class SyntaxNodeArg : SyntaxNode
    {
        public SyntaxNode Argument { get; }
        public string ArgumentName { get; }

        public SyntaxNodeArg(SyntaxNode arg, string name)
        {
            this.Argument = arg;
            this.ArgumentName = name;
        }
    }

    public class SyntaxNodeProperty : SyntaxNode
    {
        public SyntaxNode Value { get; }
        public string PropertyName { get; }
        public SyntaxNode Index { get; }

        public SyntaxNodeProperty(SyntaxNode value, string property, SyntaxNode index)
        {
            Value = value;
            PropertyName = property;
            Index = index;
        }
    }


    public class SyntaxNodeValue : SyntaxNode
    {
        public object Value { get; }

        public SyntaxNodeValue(object value)
        {
            Value = value;
        }
    }

}
