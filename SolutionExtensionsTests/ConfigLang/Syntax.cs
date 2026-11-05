using System;
using System.Linq;
using System.Runtime.Serialization;

namespace SolutionExtensions.ConfigLang
{
    /*
    grammar:
    line=varSet|fnCall
    varSet=$IDENT=expr
    fnCall=$IDENT(expr[argEps])
    argEps=,expr[argEps]|ε
    IDENT=[LETTER]?[LETTER|DIGIT]*
    expr=NUMBER|STRING
    NUMBER=[DIGIT]+
    STRING='[any]'|"[any]"    
    */
    public class Syntax
    {
        private Tokenizer tokenizer;
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
        public TO Analyze(string s)
        {
            tokenizer = new Tokenizer();
            tokens = tokenizer.Parse(s).ToArray();
            index = 0;
            throw new NotImplementedException();
            //return ReadFormulaExpression();
        }
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
