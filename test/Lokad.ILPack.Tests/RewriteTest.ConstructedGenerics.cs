using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Lokad.ILPack.Tests
{
    public partial class RewriteTest
    {
        [Fact]
        public async void GenericInt()
        {
            Assert.Equal(5, await Invoke(
                @"var r = x.GenericInt;",
                "r.Value"
                ));
        }

        [Fact]
        public async void GenericConstructedMethod()
        {
            Assert.Equal("Hello Generic World!", await Invoke(
                @"var r = x.GenericConstructedMethod(""Hello Generic World!"");",
                "r.Value"
                ));
        }
    }
}
