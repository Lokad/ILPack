using System;
using System.Collections.Generic;
using System.Text;
using Xunit;


namespace Lokad.ILPack.Tests
{
    partial class RewriteTest
    {
        [Fact]
        public async void BasicStructTest()
        {
            Assert.Equal((20, 30), await Invoke(
                $"var s = x.GetMyStruct();",
                "(s.x,s.y)"));
        }

    }
}
