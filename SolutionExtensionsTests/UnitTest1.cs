using Microsoft.VisualStudio.TestTools.UnitTesting;
using SolutionExtensions.Formula;
using System.Linq;

namespace SolutionExtensionsTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestTokens()
        {
            var tokenizer = new Tokenizer();
            var tokens = tokenizer.Parse("ident1,\"string1\" (ident2) ident3[55]").ToArray();
            Assert.AreEqual(10, tokens.Length);
            var pos = 0;
            Assert.AreEqual(Token.Identifier, tokens[pos++].Token);
            Assert.AreEqual(",", tokens[pos].String);
            Assert.AreEqual(Token.String, tokens[pos++].Token);
            Assert.AreEqual("string1", tokens[pos].String);
            Assert.AreEqual(Token.String, tokens[pos++].Token);
            Assert.AreEqual("(", tokens[pos].String);
            Assert.AreEqual(Token.String, tokens[pos++].Token);
            Assert.AreEqual(Token.Identifier, tokens[pos++].Token);
            Assert.AreEqual(")", tokens[pos].String);
            Assert.AreEqual(Token.String, tokens[pos++].Token);

            Assert.AreEqual(Token.Identifier, tokens[pos++].Token);
            Assert.AreEqual("[", tokens[pos].String);
            Assert.AreEqual(Token.String, tokens[pos++].Token);
            Assert.AreEqual(55, tokens[pos].Number);
            Assert.AreEqual(Token.Number, tokens[pos++].Token);
            Assert.AreEqual("]", tokens[pos].String);
            Assert.AreEqual(Token.String, tokens[pos++].Token);
        }
        [TestMethod]
        public void TestStrings()
        {
            var tokenizer = new Tokenizer();
            var tokens = tokenizer.Parse("\"\" \"str!@#$+ěšč\" \"a\\tb\" \"\\u0012\" \"\\U00103040\" \"\\x23aG\"").ToArray();
            Assert.AreEqual(6, tokens.Length);
            Assert.IsTrue(tokens.All(x => x.Token == Token.String));
            var pos = 0;
            Assert.AreEqual("", tokens[pos++].String);
            Assert.AreEqual("str!@#$+ěšč", tokens[pos++].String);
            Assert.AreEqual("a\tb", tokens[pos++].String);
            Assert.AreEqual("\x12", tokens[pos++].String);
            Assert.AreEqual("\U00103040", tokens[pos++].String);
            Assert.AreEqual("\u023aG", tokens[pos++].String);
        }

        [TestMethod]
        public void TestNumbers()
        {
            var tokenizer = new Tokenizer();
            var tokens = tokenizer.Parse("1 0.2 3d 4f 5.6d 7.88f -9m 10e-4 0x1a").ToArray();
            Assert.AreEqual(9, tokens.Length);
            Assert.IsTrue(tokens.All(x => x.Token == Token.Number));
            var pos = 0;
            Assert.AreEqual(1, tokens[pos++].Number);
            Assert.AreEqual((double)0.2, tokens[pos++].Number);
            Assert.AreEqual((double)3d, tokens[pos++].Number);
            Assert.AreEqual((float)4f, tokens[pos++].Number);
            Assert.AreEqual((double)5.6d, tokens[pos++].Number);
            Assert.AreEqual((float)7.88f, tokens[pos++].Number);
            Assert.AreEqual((decimal)-9m, tokens[pos++].Number);
            Assert.AreEqual(0.001, tokens[pos++].Number);
            Assert.AreEqual(26, tokens[pos++].Number);
        }

        [TestMethod]
        public void TestSA()
        {
            var syntax = new Syntax<SyntaxNode>() { Builder = new SyntaxTreeBuilder() };
            var tree = syntax.Analyze(@"aaa");
            tree = syntax.Analyze(@"aaa.bbb");
            tree = syntax.Analyze(@"aaa[1]");
            tree = syntax.Analyze(@"aaa()");
            tree = syntax.Analyze(@"aaa(1)");
            tree = syntax.Analyze(@"aaa(1,true)");
            tree = syntax.Analyze(@"aaa(b:1,a:true, null)");
        }

        [TestMethod]
        public void TestEval()
        {
            var b = new ReflectionBinder();
            var o = new TestClass()
            {
                Text = "Some text",
                Value = 42,
                Child = new TestClass() { Text = "This is child" },
                Texts = new[] { "Text1", "Text2" }
            };

            var v = b.Evaluate(o, "Text");
            Assert.AreEqual("Some text", (string)v);
            v = b.Evaluate(o, "Value");
            Assert.AreEqual(42, v);
            v = b.Evaluate(o, "Child.Text");
            Assert.AreEqual("This is child", v);

            //indexers works somehow different...
            //v = b.Evaluate(o, "Texts[0]");
            //Assert.AreEqual("Text1", v);
            v = b.Evaluate(o, "Child[0]");
            Assert.AreEqual("1", v);
        }
    }
    class TestClass
    {
        public string Text { get; set; }
        public int Value { get; set; }
        public TestClass Child { get; set; }
        public string[] Texts { get; set; }
        public string this[int index]
        {
            get
            {
                return (index + 1).ToString();
            }
        }
    }
}
