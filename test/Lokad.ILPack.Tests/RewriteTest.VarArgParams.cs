using System;
using System.Collections.Generic;
using System.Text;
using Xunit;


namespace Lokad.ILPack.Tests
{
    partial class RewriteTest
    {
        [Fact]
        public async void MethodWithVarArgs()
        {
            Assert.Equal("ApplesPearsBananas", await Invoke(
                @"var r = x.MethodWithVarArgs(""Apples"", ""Pears"", ""Bananas"");",
                "r"));
        }

        [Fact]
        public async void MethodWithFixedAndVarArgs()
        {
            Assert.Equal("99: ApplesPearsBananas", await Invoke(
                @"var r = x.MethodWithFixedAndVarArgs(99, ""Apples"", ""Pears"", ""Bananas"");",
                "r"));
        }

    }
}
