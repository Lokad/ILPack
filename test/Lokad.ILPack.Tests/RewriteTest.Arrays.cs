using System;
using System.Collections.Generic;
using System.Text;
using Xunit;


namespace Lokad.ILPack.Tests
{
    partial class RewriteTest
    {
        [Fact]
        public async void MethodWithSZArray()
        {
            Assert.Equal(6, await Invoke(
                @"var r = x.MethodWithSZArray(new int[] { 1,2,3 });",
                "r"
                ));
        }

        [Fact]
        public async void MethodReturningSZArray()
        {
            Assert.Equal(new int[] { 1, 2, 3 }, await Invoke(
                @"var r = x.MethodReturningSZArray();",
                "r"
                ));
        }

        [Fact]
        public async void MethodWithMultiDimArray()
        {
            Assert.Equal(78, await Invoke(
                @"
                    var array = new int[,,]
                    {
                        { { 1, 2 }, { 3, 4 }, {5, 6 } },
                        { { 7, 8 }, { 9, 10 }, {11, 12 } },
                    };
                    var r = x.MethodWithMultiDimArray(array);",
                "r"
                ));
        }

        [Fact]
        public async void MethodReturningMultiDimArray()
        {
            var expected = new int[,,]
            {
                { { 1, 2 }, { 3, 4 }, {5, 6 } },
                { { 7, 8 }, { 9, 10 }, {11, 12 } },
            };

            Assert.Equal(expected, await Invoke(
                @"var r = x.MethodReturningMultiDimArray();",
                "r"
                ));
        }
    }
}
