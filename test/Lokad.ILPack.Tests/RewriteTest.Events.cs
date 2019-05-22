using Xunit;


namespace Lokad.ILPack.Tests
{
    partial class RewriteTest
    {
        [Fact]
        public async void NoParamEvent()
        {
            Assert.Equal(99, await Invoke(
                @"  int cbVal = 0; 
                    x.NoParamEvent += () => cbVal = 99;
                    x.InvokeNoParamEvent()",
                   
                "cbVal"));
        }

        [Fact]
        public async void InvokeNoParamEventWithNoListeners()
        {
            Assert.Equal(true, await Invoke(
                @"x.InvokeNoParamEvent()",
                "true"));
        }

        [Fact]
        public async void IntParamEvent()
        {
            Assert.Equal(77, await Invoke(
                @"  int cbVal = 0; 
                    x.IntParamEvent += (val) => cbVal = val;
                    x.InvokeIntParamEvent(77)",
                   
                "cbVal"));
        }

        [Fact]
        public async void InvokeIntParamEventWithNoListeners()
        {
            Assert.Equal(true, await Invoke(
                @"x.InvokeIntParamEvent(77)",
                "true"));
        }

    }
}
