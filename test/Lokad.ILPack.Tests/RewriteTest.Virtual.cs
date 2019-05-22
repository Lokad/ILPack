using Xunit;

namespace Lokad.ILPack.Tests
{
    partial class RewriteTest
    {
        [Fact]
        public async void AbstractMethodThroughBase()
        {
            Assert.Equal("MyClass.AbstractMethod", await Invoke(
                "var bt = x as MyBaseClass;  var r = bt.AbstractMethod();",
                "r"
                ));
        }

        [Fact]
        public async void AbstractMethodThroughImpl()
        {
            Assert.Equal("MyClass.AbstractMethod", await Invoke(
                "var r = x.AbstractMethod();",
                "r"
                ));
        }

        [Fact]
        public async void VirtualMethodThroughBase()
        {
            Assert.Equal("MyClass.VirtualMethod", await Invoke(
                "var bt = x as MyBaseClass;  var r = bt.VirtualMethod();",
                "r"
                ));
        }

        [Fact]
        public async void VirtualMethodThroughImpl()
        {
            Assert.Equal("MyClass.VirtualMethod", await Invoke(
                "var r = x.VirtualMethod();",
                "r"
                ));
        }

        [Fact]
        public async void HiddenMethodThroughBase()
        {
            Assert.Equal("MyBaseClass.HiddenMethod", await Invoke(
                "var bt = x as MyBaseClass;  var r = bt.HiddenMethod();",
                "r"
                ));
        }

        [Fact]
        public async void HiddenMethodThroughImpl()
        {
            Assert.Equal("MyClass.HiddenMethod", await Invoke(
                "var r = x.HiddenMethod();",
                "r"
                ));
        }

        [Fact]
        public async void HiddenVirtualMethodThroughBase()
        {
            Assert.Equal("MyBaseClass.HiddenVirtualMethod", await Invoke(
                "var bt = x as MyBaseClass;  var r = bt.HiddenVirtualMethod();",
                "r"
                ));
        }

        [Fact]
        public async void HiddenVirtualMethodThroughImpl()
        {
            Assert.Equal("MyClass.HiddenVirtualMethod", await Invoke(
                "var r = x.HiddenVirtualMethod();",
                "r"
                ));
        }

    }
}
