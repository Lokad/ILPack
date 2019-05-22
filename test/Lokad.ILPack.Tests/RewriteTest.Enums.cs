using Xunit;


namespace Lokad.ILPack.Tests
{
    partial class RewriteTest
    {
        [Fact]
        public async void BasicEnumTest()
        {
            Assert.Equal(150, await Invoke(
                $"int r = (int)MyEnum.Pears;",
                "r"));
        }

    }
}
