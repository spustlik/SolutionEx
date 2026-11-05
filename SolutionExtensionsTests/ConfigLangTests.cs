using Microsoft.VisualStudio.TestTools.UnitTesting;
using SolutionExtensions.ConfigLang;
using System;

namespace SolutionExtensionsTests
{
    [TestClass]
    public class ConfigLangTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            var tok = new Tokenizer();
            Test("$A=1\n$B=3\n$EXEC($A,$B)"tok.Parse()
        }
    }
}
