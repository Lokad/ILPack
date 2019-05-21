using System;
using System.Collections.Generic;
using System.Text;
using Xunit;


namespace Lokad.ILPack.Tests
{
    partial class RewriteTest
    {
        [Fact]
        public async void VoidMethod()
        {
            Assert.Equal(true, await Invoke(
                "x.VoidMethod();",
                "true"));
        }

        [Fact]
        public async void IntMethod()
        {
            Assert.Equal(33, await Invoke(
                "var r = x.IntMethod();",
                "r"));
        }

        [Fact]
        public async void IntMethodWithParameters()
        {
            Assert.Equal(30, await Invoke(
                "var r = x.IntMethodWithParameters(10,20);",
                "r"));
        }

    }
}
