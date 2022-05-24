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

        [Fact]
        public async void AnotherMethodWithDefaultParameterValues()
        {
            Assert.Equal("Hallo, world!", await Invoke(
                "var r = x.AnotherMethodWithDefaultParameterValues(0);",
                "r"));
            Assert.Equal(string.Empty, await Invoke(
                "var r = x.AnotherMethodWithDefaultParameterValues(0, \"\");",
                "r"));
            Assert.Equal(string.Empty, await Invoke(
                "var r = x.AnotherMethodWithDefaultParameterValues(0, \"\", \"\");",
                "r"));
            Assert.Equal(string.Empty, await Invoke(
                "var r = x.AnotherMethodWithDefaultParameterValues(0, \"\", \"\", 27);",
                "r"));

            Assert.Null(await Invoke(
                "var r = x.AnotherMethodWithDefaultParameterValues(1);",
                "r"));
            Assert.Null(await Invoke(
                "var r = x.AnotherMethodWithDefaultParameterValues(1, \"\");",
                "r"));
            Assert.Equal("Hallo, world!", await Invoke(
                "var r = x.AnotherMethodWithDefaultParameterValues(1, \"\", \"Hallo, world!\");",
                "r"));
            Assert.Equal("Hallo, world!", await Invoke(
                "var r = x.AnotherMethodWithDefaultParameterValues(1, \"\", \"Hallo, world!\", 27);",
                "r"));

            Assert.Equal(4711, await Invoke(
                "var r = x.AnotherMethodWithDefaultParameterValues(2);",
                "r"));
            Assert.Equal(4711, await Invoke(
                "var r = x.AnotherMethodWithDefaultParameterValues(2, \"\");",
                "r"));
            Assert.Equal(4711, await Invoke(
                "var r = x.AnotherMethodWithDefaultParameterValues(2, \"\", \"\");",
                "r"));
            Assert.Equal(27, await Invoke(
                "var r = x.AnotherMethodWithDefaultParameterValues(2, \"\", \"\", 27);",
                "r"));
        }

        [Fact]
        public async void VirtualMethodWithInModifier()
        {
            Assert.Equal("Hello", await Invoke(@"
    var s = new StringSpan("" Hello "",1,5);
    var r = new MyClassWithInModifier().Print(in s);
", "r"));
        }

        [Fact]
        public async void FunctionPointerWithGenericsCallback()
        {
            Assert.Equal(42, await Invoke(@"var r = x.MethodWithGenericCallback();", "r"));
        }

        [Fact]
        public async void FunctionPointerWithCallback()
        {
            Assert.Equal((byte)42, await Invoke(@"var r = x.MethodWithSimpleCallback();", "r"));
        }

        [Fact]
        public async void FunctionPointerWithModifiersCallback()
        {
            Assert.Equal("Hello", await Invoke(@"var r = x.MethodWithModifiersCallback();", "r"));
        }
    }
}
