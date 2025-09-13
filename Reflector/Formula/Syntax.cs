using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace Reflector.Formula
{
    /*
    grammar:
    accessExpr=ident[index]{.}accessExpr|ident args
    part=ident[index]
    index={[}indexExpr{]}
    indexExpr=number|staticProp
    staticProp=expr
    args={()}|{(}argsArray{)}
    argsArray=[name{:}]arg[{,}argsArray]
    arg=expr
    expr=accessExpr|number|string|{null}|{true}|{false}
    */
    public class Syntax<TO>
    {
        private Tokenizer tokenizer;
        private TokenRecord[] tokens;
        private int index;
        private bool Eof => index >= tokens.Length;
        public ISyntaxResultBuilder<TO> Builder { get; set; }
        private void PushBack(TokenRecord r)
        {
            index--;
            if (!Equals(Peek(), r))
                throw new Exception($"Internal error, connot go back");
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
        public TO Analyze(string s)
        {
            tokenizer = new Tokenizer();
            tokens = tokenizer.Parse(s).ToArray();
            index = 0;
            return ReadFormulaExpression();
        }

        protected virtual TO ReadFormulaExpression()
        {
            //formulaExpr=ident[index][{.}formulaExpr]|ident{(}args{)}
            /*
            formula=identParts[{(}args{)}]
            identParts = (ident|ident{.}identParts)

            f=
            aaa
            aaa(...args)
            aaa[2]
            bbb.aaa
            bbb.aaa(...args)
            bbb.aaa[2]
            bbb[1].aaa
            bbb[1].aaa(...args)
            bbb[1].aaa[2]
            !not:
            aaa[1](...args)
            bbb[1].aaa[1](...args)
            bbb(...args).aaa
             */
            var o = ReadIdentParts(default, out var methodName);
            var next = Peek();
            if (next.IsSeparator("("))
            {
                if (methodName == null)
                    throw new SyntaxException($"Cannot call method after indexer", next);
                var args = ReadArgs();
                o = Builder.CreateMethodCall(o, methodName, args);
                if (!Eof)
                    throw new SyntaxException($"Unexpected characters after method call", Peek());
                return o;
            }
            return o;
        }
        protected virtual TO ReadIdentParts(TO left, out string methodName)
        {
            //identParts = (ident|ident{.}identParts)
            //ident = id[index]
            var t = ExpectAndRead(Token.Identifier);
            var ident = t.String;
            methodName = null;
            var next = Peek();
            TO o = default;
            TO index = default;
            if (next.IsSeparator("["))
            {
                index = ReadIndex();
                next = Peek();
            }
            else
            {
                if (next.IsSeparator("("))
                {
                    methodName = ident;
                    return o;
                }
            }
            o = Builder.CreateProperty(left, ident, index);
            if (next.IsSeparator("."))
            {
                Read();
                o = ReadIdentParts(o, out methodName);
            }
            return o;
        }

        protected virtual TO ReadExpression()
        {
            //expr = accessExpr | number | string |{ null}|{ true}|{ false}
            var t = Peek();
            if (t.Token == Token.Number)
                return ReadNumber();
            if (t.Token == Token.String)
                return ReadString();
            if (IsLiteralKeyword(t))
                return ReadLiteralKeyword();
            if (t.Token == Token.Identifier)
                return ReadFormulaExpression();
            throw new SyntaxException($"Expected expression", t);
        }

        private bool IsLiteralKeyword(TokenRecord r)
        {
            var k = "null,true,false".Split(',');
            return r.Token == Token.Keyword && k.Contains(r.String);
        }
        protected virtual TO ReadLiteralKeyword()
        {
            var token = ExpectAndRead(Token.Keyword, t => IsLiteralKeyword(t), " null,true or false");
            if (token.String == "null")
                return Builder.CreateNull();
            return Builder.CreateValue(token.String == "true");
        }

        protected virtual TO ReadIndex()
        {
            //index="["indexExpr"]"
            ExpectAndRead(Token.Separator, t => t.String == "[", " [");
            var r = ReadIndexExpr();
            ExpectAndRead(Token.Separator, t => t.String == "]", " ]");
            return r;
        }

        protected virtual TO ReadIndexExpr()
        {
            //indexExpr=number|staticProp
            var t = Peek();
            if (t.Token == Token.Number)
                return ReadIntegerNumber();
            if (t.Token == Token.Identifier)
                return ReadExpression();
            throw new SyntaxException($"Expected number or identifier", t);
        }

        protected virtual TO ReadIntegerNumber()
        {
            var token = ExpectAndRead(Token.Number, t => t.Number is int, " (int)");
            return Builder.CreateValue((int)token.Number);
        }
        protected virtual TO ReadNumber()
        {
            var token = ExpectAndRead(Token.Number);
            return Builder.CreateNumberValue(token.Number);
        }
        protected virtual TO ReadString()
        {
            var token = ExpectAndRead(Token.String);
            return Builder.CreateValue(token.String);
        }

        protected virtual TO ReadArgs()
        {
            //args={()}|{(}argsArray{)}
            ExpectAndRead(Token.Separator, t => t.String == "(", " (");
            var next = Peek();
            TO o = default;
            if (!next.IsSeparator(")"))
            {
                o = ReadArgsArray();
            }
            ExpectAndRead(Token.Separator, t => t.String == ")", " )");
            return o;
        }
        protected virtual TO ReadArgsArray()
        {
            //argsArray=[name{:}]arg[{,}argsArray]
            var t = Peek();
            string name = null;
            if (t.Token == Token.Identifier)
            {
                Read();//name
                var next = Peek();
                if (!next.IsSeparator(":"))
                {
                    PushBack(t);
                }
                else
                {
                    Read(); //:
                    name = t.String;
                    //is named arg
                }
            }
            var arg = ReadExpression();
            var o = Builder.CreateArgsList(arg, name);

            if (Peek().IsSeparator(","))
            {
                Read();
                var moreArgs = ReadArgsArray();
                o = Builder.CreateArgsList(o, moreArgs);
            }
            return o;
        }

    }

    public interface ISyntaxResultBuilder<TO>
    {
        TO CreateProperty(TO left, string propertyName, TO index);
        TO CreateMethodCall(TO value, string methodName, TO args);
        TO CreateArgsList(TO arg, string argumentName);
        TO CreateArgsList(TO list, TO append);
        TO CreateNull();
        TO CreateValue(bool value);
        TO CreateValue(int value);
        TO CreateValue(string s);
        TO CreateNumberValue(object number);
    }

    [Serializable]
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

        protected SyntaxException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
