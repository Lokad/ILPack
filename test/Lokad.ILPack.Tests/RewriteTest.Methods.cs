using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
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

        [Fact]
        public async void AnotherMethodWithDifferentParameterTypes7()
        {
            Assert.Equal(CancellationToken.None, await Invoke(
                "var r = x.AnotherMethodWithDifferentParameterTypes7(false, 1.0f, 2, 3, 4, new object(), System.Threading.CancellationToken.None);",
                "r"));

            Assert.Equal(9, await Invoke(
                "var r = x.AnotherMethodWithDifferentParameterTypes7(true, 1.0f, 2, 3, 4, null, System.Threading.CancellationToken.None);",
                "r"));

            Assert.Equal(10, await Invoke(
                "var r = x.AnotherMethodWithDifferentParameterTypes7(true, 1.0f, 2, 3, 4, new object(), System.Threading.CancellationToken.None);",
                "r"));
        }
    }
}
