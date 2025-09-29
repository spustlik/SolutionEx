using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SolutionExtensionsTests
{
    [TestClass]
    public class UnitTest2
    {
        [TestMethod]
        public void TestMethod1()
        {
            var pat = new Pattern("*.cs;*.ts;*.html");
            Assert.IsTrue(pat.Match("a.cs"));
            Assert.IsTrue(pat.Match("/foo/a.ts"));
            Assert.IsTrue(pat.Match("c:\\foo\\bar\\a.html"));
            Assert.IsFalse(pat.Match("a.css"));
            Assert.IsFalse(pat.Match("a.htm"));

        }
    }
    class Pattern
    {
        Regex regex;

        public Pattern(string filesPattern)
        {
            regex = CreatePattern(filesPattern);
        }

        private Regex CreatePattern(string s)
        {
            //s = s.Replace("\\", "/").Replace("/", Path.PathSeparator + "");
            var parts = s.Split(new[] { ';', ',' });
            var types = new List<string>();
            foreach (var part in parts)
            {
                types.Add(CreateTypePattern(part));
            }
            return new Regex(string.Join("|", types), RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }


        private string CreateTypePattern(string part)
        {
            string GetChar(char ch)
            {
                switch (ch)
                {
                    case '.': return "\\.";
                    case '*': return "[^\\.]*";
                    case '\\': return "/";
                    default: return Regex.Escape(ch + "");
                }
            }
            var t = part.Trim();
            var sb = new StringBuilder();
            foreach (var ch in t)
                sb.Append(GetChar(ch));
            sb.Append("$");
            return sb.ToString();

        }
        public bool Match(string fn)
        {
            return regex.Match(fn).Success;
        }
    }
}