using Xunit;


namespace Lokad.ILPack.Tests
{
    partial class RewriteTest
    {
        [Fact]
        public async void ByRefParam()
        {
            Assert.Equal(34, await Invoke(
                $"int r=0; x.ByRefParam(ref r);",
                "r"));
        }

        [Fact]
        public async void OutParam()
        {
            Assert.Equal(35, await Invoke(
                $"int r; x.OutParam(out r);",
                "r"));
        }

    }
}
