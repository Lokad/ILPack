using Xunit;


namespace Lokad.ILPack.Tests
{
    partial class RewriteTest
    {
        [Fact]
        public async void Indexer()
        {
            Assert.Equal(10, await Invoke(
                $"x[1] = 10; var r = x[1];",
            "r"
            ));
        }
    }
}
