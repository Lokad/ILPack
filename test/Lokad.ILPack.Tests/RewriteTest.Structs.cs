using System.Runtime.InteropServices;
using Xunit;


namespace Lokad.ILPack.Tests
{
    partial class RewriteTest
    {
        [Fact]
        public async void StructBasic()
        {
            Assert.Equal((20, 30), await Invoke(
                $"var s = x.GetMyStruct();",
                "(s.x,s.y)"));
        }

        [Fact]
        public async void StructExplicitLayout()
        {
            Assert.Equal(0x1234, await Invoke(
                $"var s = new MyExplicitLayoutStruct(); s.al=0x34; s.ah = 0x12;",
                "(int)s.ax"
                ));

        }

        [Fact]
        public void StructExplicitLayoutSize()
        {
            var t = _asmCloned.GetType($"{_namespaceName}.MyExplicitLayoutStruct");
            Assert.Equal(16, t.StructLayoutAttribute.Size);
            Assert.Equal(1, t.StructLayoutAttribute.Pack);
        }


    }
}
