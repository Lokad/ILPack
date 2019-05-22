using Xunit;


namespace Lokad.ILPack.Tests
{
    partial class RewriteTest
    {
        [Fact]
        public async void Nullable()
        {
            Assert.Equal(false, await Invoke(
                $"var r = x.GetNullable();",
                "r"
            ));
        }
    }
}
