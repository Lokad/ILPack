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

        [Fact]
        public async void CallExplicitlyImplementedGenericInterfaceMethod()
        {
            Assert.Equal(4711, await Invoke(
                $"int r = (x as IMyItf).InterfaceMethod3<int>(() => 4711);",
                "r"));
        }

        [Fact]
        public async void ImplementImportedInterface()
        {
            Assert.Equal(1, await Invoke(
                $"var a = new MyComparable(10); var b = new MyComparable(20);",
                "a.CompareTo(b)"
                ));
        }

        [Fact]
        public async void ImplementImportedGenericInterface()
        {
            Assert.Equal(1, await Invoke(
                $"var a = new MyComparableT(10); var b = new MyComparableT(20);",
                "a.CompareTo(b)"
                ));
        }

    }
}
