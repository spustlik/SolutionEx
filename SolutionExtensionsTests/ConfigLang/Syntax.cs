using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace SolutionExtensions.ConfigLang
{
    public class Syntax
    {
        private TokenRecord[] tokens;
        private int index;
        private bool Eof => index >= tokens.Length;
        //public SyntaxResultBuilder Builder { get; set; }
        private void PushBack(TokenRecord r)
        {
            index--;
            if (!Equals(Peek(), r))
                throw new Exception($"Internal error, cannot go back");
        }
        private TokenRecord Read() { return Eof ? default : tokens[index++]; }
        private TokenRecord Peek() { return Eof ? default : tokens[index]; }
        private TokenRecord ExpectAndRead(Token token,
            Func<TokenRecord, bool> more = null,
            string moreError = null)
        {
            var r = Read();
            if (r.Token == token && (more == null || more(r)))
                return r;
            throw new SyntaxException($"Expected {token}{moreError}", r);
        }
        private TokenRecord ExpectAndRead(string separator)
        {
            return ExpectAndRead(Token.Separator, t => t.IsSeparator(separator));
        }
        public IEnumerable<AstNode> Analyze(string s)
        {
            var tokenizer = new Tokenizer();
            var tokens = tokenizer.Parse(s).ToArray();
            return Analyze(tokens);
        }
        public IEnumerable<AstNode> Analyze(TokenRecord[] tokens)
        {
            index = 0;
            this.tokens = tokens;
            while (!Eof)
            {
                yield return ReadLine();
            }
        }

        private AstNode ReadLine()
        {
            var ident = ExpectAndRead(Token.Identifier);
            if (Peek().IsSeparator("("))
                return ReadFunctionCall(ident);
            else
                return ReadVarSet(ident);
        }

        private AstNode ReadVarSet(TokenRecord ident)
        {
            var expr = ReadExpresion();
            ExpectAndRead("=");
            return new VarNode() { Ident = ident.String, Expression = expr };
        }

        private AstNode ReadFunctionCall(TokenRecord ident)
        {
            ExpectAndRead("(");
            var arguments = new List<ExpressionNode>();
            while (!Eof)
            {
                var expr = ReadExpresion();
                arguments.Add(expr);
                if (Peek().IsSeparator(")"))
                {
                    Read();
                    break;
                }
                ExpectAndRead(",");
            }
            return new FunctionCallNode() { Ident = ident.String, Arguments = arguments.ToArray() };
        }

        private ExpressionNode ReadExpresion()
        {
            if (Peek().Token == Token.String)
                return ReadString();
            return ReadNumber();
        }

        private ExpressionNode ReadNumber()
        {
            var str = ExpectAndRead(Token.Number);
            return new NumberNode() { Value = str.Number as double};
        }

        private ExpressionNode ReadString()
        {
            var str = ExpectAndRead(Token.String);
            return new StringNode() { Value = str.String };
        }
    }

    public abstract class AstNode
    {
    }

    public sealed class VarNode : AstNode
    {
        public string Ident { get; set; }
        public ExpressionNode Expression { get; set; }
    }
    public sealed class FunctionCallNode : AstNode
    {
        public string Ident { get; set; }
        public ExpressionNode[] Arguments { get; set; }
    }
    public abstract class ExpressionNode : AstNode
    {

    }
    public sealed class StringNode : ExpressionNode
    {
        public string Value { get; internal set; }
    }
    public sealed class NumberNode : ExpressionNode
    {
        public double Value { get; set; }
    }
    public class SyntaxException : Exception
    {
        public int Line { get; }
        public int Column { get; }
        public SyntaxException(string message, TokenRecord r)
            : base(message + $"\nAt line {r.LineNumber + 1}, column {r.Column + 1}")
        {
            this.Line = r.LineNumber;
            Column = r.Column;
        }
    }
}
