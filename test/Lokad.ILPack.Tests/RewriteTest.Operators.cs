using System;
using System.Collections.Generic;
using System.Text;
using Xunit;


namespace Lokad.ILPack.Tests
{
    partial class RewriteTest
    {
        [Fact]
        public async void OperatorBinary()
        {
            Assert.Equal((40, 60), await Invoke(
                $"var s = new MyStruct(10,20) + new MyStruct(30, 40);",
                "(s.x,s.y)"));
        }

        [Fact]
        public async void OperatorUnary()
        {
            Assert.Equal((-10, -20), await Invoke(
                $"var s = -(new MyStruct(10,20));",
                "(s.x,s.y)"));
        }

        [Fact]
        public async void OperatorCastExplicit()
        {
            Assert.Equal("[10,20]", await Invoke(
                $"string s = new MyStruct(10,20);",
                "s"));
        }

        [Fact]
        public async void OperatorCastImplicit()
        {
            Assert.Equal(5.5, await Invoke(
                $"double r = (double)new MyStruct(1, 10);",
                "r"));
        }

    }
}
