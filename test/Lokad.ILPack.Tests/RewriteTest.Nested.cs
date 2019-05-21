using System;
using System.Collections.Generic;
using System.Text;
using Xunit;


namespace Lokad.ILPack.Tests
{
    partial class RewriteTest
    {
        [Fact]
        public async void NestedClass()
        {
            Assert.Equal(9, await Invoke(
                $"var r = new MyClass.NestedClass().Method();",
                "r"
            ));
        }
    }
}
