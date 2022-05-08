using System;
using System.Collections.Generic;
using System.Text;
using Xunit;


namespace Lokad.ILPack.Tests
{
    partial class RewriteTest
    {
        [Fact]
        public async void DelegateAction()
        {
            Assert.Equal(100, await Invoke(
                $"var r = 0; x.DelegateAction(() => r = 100);",
                "r"
            ));
        }

        [Fact]
        public async void DelegateMyAction()
        {
            Assert.Equal(100, await Invoke(
                $"var r = 0; x.DelegateMyAction(() => r = 100);",
                "r"
            ));
        }

        [Fact]
        public async void DelegateActionWithParam()
        {
            Assert.Equal(101, await Invoke(
                $"var r = 0; x.DelegateActionWithParam((v) => r = v, 101);",
                "r"
            ));
        }

        [Fact]
        public async void DelegateFunc()
        {
            Assert.Equal(102, await Invoke(
                $"var r = x.DelegateFunc(() => 102);",
                "r"
            ));
        }

        [Fact]
        public async void DelegateFuncWithParam()
        {
            Assert.Equal(103, await Invoke(
                $"var r = x.DelegateFuncWithParam((v) => v, 103);",
                "r"
            ));
        }

    }
}
