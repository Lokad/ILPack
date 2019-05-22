using System;
using System.Collections.Generic;
using System.Text;
using Xunit;


namespace Lokad.ILPack.Tests
{
    partial class RewriteTest
    {
        [Fact]
        public async void ExtensionMethod()
        {
            Assert.Equal("HelloWorldHelloWorld", await Invoke(
                $"var r = \"HelloWorld\".Repeated();",
                "r"));
        }
    }
}
