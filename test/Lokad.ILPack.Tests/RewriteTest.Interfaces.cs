using System;
using System.Collections.Generic;
using System.Text;
using Xunit;


namespace Lokad.ILPack.Tests
{
    partial class RewriteTest
    {
        [Fact]
        public async void CallExplicitlyImplementedInterfaceMethod()
        {
            Assert.Equal(1001, await Invoke(
                $"int r = (x as IMyItf).InterfaceMethod1();",
                "r"));
        }

        [Fact]
        public async void CallImplicitlyImplementedInterfaceMethodThroughInterface()
        {
            Assert.Equal(1002, await Invoke(
                $"int r = (x as IMyItf).InterfaceMethod2();",
                "r"));
        }

        [Fact]
        public async void CallImplicitlyImplementedInterfaceMethodThroughClass()
        {
            Assert.Equal(1002, await Invoke(
                $"int r = x.InterfaceMethod2();",
                "r"));
        }

    }
}
