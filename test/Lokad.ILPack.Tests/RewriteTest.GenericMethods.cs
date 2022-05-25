using Xunit;


namespace Lokad.ILPack.Tests
{
    partial class RewriteTest
    {
        [Fact]
        public async void PartiallyResolvedGenericMethod()
        {
            Assert.Equal(42, await Invoke(
                $"int r = MyClass.PartiallyResolvedGenericMethod();",
                "r"));
        }

        [Fact]
        public async void StaticGenericMethod()
        {
            Assert.Equal(36, await Invoke(
                $"int r = MyClass.StaticGenericMethod<int>(36);",
                "r"));
        }

        [Fact]
        public async void StaticGenericMethodWithByRef()
        {
            Assert.Equal((38,37), await Invoke(
                $"int a = 37; int b = 38; MyClass.StaticGenericMethodWithByRef<int>(ref a, ref b);",
                "(a,b)"));
        }

        [Fact]
        public async void GenericMethod()
        {
            Assert.Equal(36, await Invoke(
                $"int r = x.GenericMethod<int>(36);",
                "r"));
        }

        [Fact]
        public async void GenericMethodWithByRef()
        {
            Assert.Equal((38,37), await Invoke(
                $"int a = 37; int b = 38; x.GenericMethodWithByRef<int>(ref a, ref b);",
                "(a,b)"));
        }

    }
}
