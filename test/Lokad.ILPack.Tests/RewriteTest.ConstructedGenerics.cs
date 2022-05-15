using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Lokad.ILPack.Tests
{
    public partial class RewriteTest
    {
        [Fact]
        public async void GenericStructInt()
        {
            Assert.Equal(5, await Invoke(
                @"var r = x.GenericStructInt;",
                "r.Value"
                ));
        }

        [Fact]
        public async void GenericStructConstructedMethod()
        {
            Assert.Equal("Hello Generic World!", await Invoke(
                @"var r = x.GenericStructConstructedMethod(""Hello Generic World!"");",
                "r.Value"
                ));
        }

        [Fact]
        public async void GenericClassInt()
        {
            Assert.Equal(5, await Invoke(
                @"var r = x.GenericClassInt;",
                "r.Value"
            ));
        }

        [Fact]
        public async void GenericClassConstructedMethod()
        {
            Assert.Equal("Hello Generic World!", await Invoke(
                @"var r = x.GenericClassConstructedMethod(""Hello Generic World!"");",
                "r.Value"
            ));
        }
    }
}
