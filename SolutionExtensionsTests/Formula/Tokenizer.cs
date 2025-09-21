using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace SolutionExtensions.Formula
{

    public enum Token
    {
        Eof,
        Identifier,
        Keyword,
        String,
        Number,
        Separator,
    }
    public struct TokenRecord
    {
        public TokenRecord(Token token, int line, int column)
        {
            Token = token;
            LineNumber = line;
            Column = column;
            String = null;
            Number = null;
        }
        public Token Token { get; set; }
        public string String { get; set; }
        public object Number { get; set; }
        public int LineNumber { get; set; }
        public int Column { get; set; }
        public override string ToString()
        {
            return $"{Token} {String}{Number} at {LineNumber + 1}:{Column + 1}";
        }
        public bool IsSeparator(string s)
        {
            return Token == Token.Separator && String == s;
        }
    }
    public class Tokenizer
    {
        private string data;
        private int index;
        private int startColumn;
        private int column;
        private int line;
        public bool Eof => index >= data.Length;
        private char PeekChar()
        {
            if (Eof) return '\0';
            return data[index];
        }

        private char ReadChar()
        {
            var c = PeekChar();
            index++;
            column++;
            return c;
        }
        public IEnumerable<TokenRecord> Parse(string text)
        {
            data = text;
            index = 0;
            startColumn = 0;
            line = 0;
            column = 0;
            while (!Eof)
            {
                var c = ReadChar();
                if (c == '/')
                {
                    if (TryParseComment())
                        continue;
                }
                if (c == '\n')
                {
                    c = PeekChar();
                    while (Char.IsWhiteSpace(c) && !Eof)
                    {
                        c = ReadChar();
                    }
                    line++;
                    startColumn = 0;
                    column = 0;
                }
                if (Char.IsWhiteSpace(c))
                {
                    continue;
                }
                if (TryParseToken(c, out var token))
                {
                    yield return token;
                    startColumn = column + 1;
                    continue;
                }
                throw new ParseException($"Unexpected char '{c}'", line, startColumn);
            }
        }

        private bool TryParseToken(char c, out TokenRecord result)
        {
            if (c == '@')
            {
                c = ReadChar();
                if (c == '"')
                {
                    result = ParseString(isVerbatim: true);
                    return true;
                }
                throw new ParseException($"Unexpected @ not followed by quotes", line, column);
            }
            if (c == '"')
            {
                result = ParseString(isVerbatim: false);
                return true;
            }
            if (IsDigit(c) || c == '-')
            {
                result = ParseNumber(c);
                return true;
            }
            if (TryParseSeparator(c, out result))
                return true;
            if (IsIdChar(c, true))
            {
                result = ParseIdentifier(c);
                return true;
            }
            return false;
        }

        private bool IsIdChar(char c, bool firstChar)
        {
            if (firstChar)
                return c == '_' || Char.IsLetter(c);
            return c == '_' || Char.IsLetterOrDigit(c);
        }

        private TokenRecord ParseIdentifier(char c)
        {
            var s = "" + c;
            while (!Eof)
            {
                c = PeekChar();
                if (!IsIdChar(c, false))
                    break;
                ReadChar();
                s += c;
            }
            return new TokenRecord(IsKeyword(s) ? Token.Keyword : Token.Identifier, line, startColumn) { String = s };
        }

        private bool IsKeyword(string s)
        {
            var k = "null,true,false".Split(',');
            return k.Contains(s);
        }

        private bool TryParseSeparator(char c, out TokenRecord token)
        {
            const string separators = "()[],.:"; //+-*/
            token = default;
            if (!separators.Contains(c))
                return false;
            token = new TokenRecord(Token.Separator, line, startColumn) { String = "" + c };
            return true;
        }

        private TokenRecord ParseNumber(char c)
        {
            var n = ParseNumberString(c);
            return new TokenRecord(Token.Number, line, startColumn) { Number = n };
        }

        private bool TryParseComment()
        {
            var c = PeekChar();
            if (c != '*')
                return false;
            ReadChar();
            while (!Eof)
            {
                c = ReadChar();
                if (c == '*')
                {
                    c = PeekChar();
                    if (c == '/')
                    {
                        ReadChar();
                        //end of comment
                        return true;
                    }
                }
            }
            throw new ParseException($"Expected end of comment */", line, startColumn);
        }

        private TokenRecord ParseString(bool isVerbatim)
        {
            var s = "";
            while (!Eof)
            {
                var c = ReadChar();
                if (c < ' ')
                    throw new ParseException($"Unsupported string character 0x{((int)c):X2}", line, startColumn);
                if (c == '"')
                {
                    if (isVerbatim && PeekChar() == '"')
                        ReadChar();
                    else
                        return new TokenRecord(Token.String, line, column) { String = s };
                }
                if (c == '\\')
                    s += ParseStringEscape(isVerbatim);
                else
                    s += c;

            }
            throw new ParseException($"Unterminated string", line, startColumn);
        }

        private string ParseStringEscape(bool isVerbatim)
        {
            var c = ReadChar();
            //https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/strings/#string-escape-sequences
            //https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/tokens/verbatim
            switch (c)
            {
                case '\'': return "'";
                case '\"': return "\"";
                //case '\\': return "\\";
                case '0': return "\0";
                case 'a': return "\a"; //alert
                case 'b': return "\b"; //backspace
                //case 'e': return "\e"; //escape?
                case 'f': return "\f";
                case 'n': return "\n";
                case 'r': return "\r";
                case 't': return "\t";
                case 'v': return "\v"; //vertical tab
            }
            if (TryParseHexNumberString(c, out var ns))
                return ns;
            if (isVerbatim)
                return c + "";
            switch (c)
            {
                //case '\"': return "\"";
                case '\\': return "\\";
            }
            throw new ParseException($"Unsupported escape character '{c}'", line, startColumn);
        }

        private bool TryParseHexNumberString(char type, out string s)
        {
            s = "";
            const string types = "xuU";
            if (!types.Contains(type))
                return false;
            /*\uHHHH (range: 0000 - FFFF)
              \U00HHHHHH (range: 000000 - 10FFFF)
              \xH[H][H][H] (range: 0 - FFFF)
            */
            int max = 4;
            if (type == 'U') max = 8;
            while (s.Length < max)
            {
                var c = PeekChar();
                if (!IsDigit(c) && !IsHexLetter(c))
                {
                    if (type == 'x')
                        break;
                    throw new ParseException($"Bad hex code '{c}' in escaped character", line, column);
                }
                ReadChar();
                s += c;
            }
            if (!Int32.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out var utf))
                throw new ParseException($"Invalid escaped number '{s}'", line, column);
            s = "" + Char.ConvertFromUtf32(utf);
            return true;
        }

        [Flags]
        enum NumberType
        {
            Int = 1,
            Hex = 2,
            Float = 4,
            Double = 8,
            Decimal = 16,
            All = Int | Hex | Float | Double
        }
        private object ParseNumberString(char c)
        {
            var numType = NumberType.Int;
            var s = "" + c;
            c = PeekChar();
            if (s + c == "0x")
            {
                ReadChar();
                s = "";
                numType = NumberType.Hex;
                while (!Eof)
                {
                    c = ReadChar();
                    if (!IsDigit(c) && !IsHexLetter(c))
                        throw new ParseException($"Unsupported hex char '{c}'", line, startColumn);
                    s += c;
                }
                if (!Int32.TryParse(s, NumberStyles.HexNumber, null, out int number))
                    throw new ParseException($"Invalid number '{s}'", line, startColumn);
                return number;
            }
            while (!Eof)
            {
                c = PeekChar();
                c = Char.ToUpperInvariant(c);
                if (IsDigit(c) || c == '.' || c == 'E' || c == '-')
                {
                    s += c;
                    if (c == '.' || c == 'E')
                        numType = NumberType.Double;
                    ReadChar();
                    continue;
                }
                if (c == 'D')
                {
                    numType = NumberType.Double;
                    ReadChar();
                    break;
                }
                if (c == 'F')
                {
                    numType = NumberType.Float;
                    ReadChar();
                    break;
                }
                if (c == 'M')
                {
                    numType = NumberType.Decimal;
                    ReadChar();
                    break;
                }
                break;
            }
            if (numType == NumberType.Int)
            {
                if (!Int32.TryParse(s, out var i))
                    throw new ParseException($"Invalid number '{s}'", line, startColumn);
                return i;
            }
            if (numType == NumberType.Decimal)
            {
                if (!decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var dec))
                    throw new ParseException($"Invalid number '{s}'", line, startColumn);
                return dec;
            }
            if (numType == NumberType.Float)
            {
                if (!float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var f))
                    throw new ParseException($"Invalid number '{s}'", line, startColumn);
                return f;
            }
            //if (numType == NumberType.Double)
            {
                if (!Double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var dec))
                    throw new ParseException($"Invalid number '{s}'", line, startColumn);
                return dec;
            }
        }

        private bool IsHexLetter(char c)
        {
            c = Char.ToUpper(c);
            return c >= 'A' && c <= 'F';
        }

        private static bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

    }

    [Serializable]
    public class ParseException : Exception
    {

        public ParseException(string message, int line, int column)
            : base(message + $"\nAt line {line + 1}, column {column + 1}")
        {
            Line = line;
            Column = column;
        }

        protected ParseException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public int Line { get; }
        public int Column { get; }
    }
}
