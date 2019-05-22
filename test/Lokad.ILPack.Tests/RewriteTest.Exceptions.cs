using Xunit;


namespace Lokad.ILPack.Tests
{
    partial class RewriteTest
    {
        [Fact]
        public async void ThrowGuardedException()
        {
            Assert.Equal(true, await Invoke(
                $"var r = x.ThrowGuardedException();",
                "r"
                ));
        }

        [Fact]
        public async void ThrowGuardedExceptionWithFinally()
        {
            Assert.Equal(0b0000_0111, await Invoke(
                $"int r = 0; x.ThrowGuardedExceptionWithFinally(ref r);",
                "r"
                ));
        }

        [Fact]
        public async void ThrowNestedGuardedExceptionWithFinally()
        {
            Assert.Equal(0b0111_0111, await Invoke(
                $"int r = 0; x.ThrowNestedGuardedExceptionWithFinally(ref r);",
                "r"
                ));
        }

        [Fact]
        public async void ThrowGuardedExceptionWithUntypedCatchAndFinally()
        {
            Assert.Equal(0b0000_0111, await Invoke(
                $"int r = 0; x.ThrowGuardedExceptionWithUntypedCatchAndFinally(ref r);",
                "r"
                ));
        }


    }
}
