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

        [Fact]
        public async void MethodWithJaggedArray()
        {
            Assert.Equal(36, await Invoke(
                @"
                    var array = new int[][]
                    {
                        new int[] { 1, 2, 3, 4, 5, },
                        new int[] { 10, 11 },
                    };
                    var r = x.MethodWithJaggedArray(array);",
                "r"
                ));
        }

        [Fact]
        public async void MethodReturningJaggedArray()
        {
            var expected = new int[][]
            {
                new int[] { 1, 2, 3, 4, 5, },
                new int[] { 10, 11 },
            };
            Assert.Equal(expected, await Invoke(
                @"var r = x.MethodReturningJaggedArray();",
                "r"
                ));
        }

        [Fact]
        public async void MethodWithSZArrayOfUserType()
        {
            Assert.Equal(21, await Invoke(
                @"
                    var array = new MyStruct[]
                    {
                        new MyStruct(1, 2),
                        new MyStruct(3, 4),
                        new MyStruct(5, 6),
                    };
                    var r = x.MethodWithSZArrayOfUserType(array);",
                "r"
                ));
        }

        [Fact]
        public async void MethodReturningSZArrayOfUserType()
        {
            Assert.Equal((3,4), await Invoke(
                @"
                var r = x.MethodReturningSZArrayOfUserType();",
                "(r[1].x, r[1].y)"
                ));
        }


        [Fact]
        public async void MethodWithMultiDimArrayOfUserType()
        {
            Assert.Equal(36, await Invoke(
                @"
                    var array = new MyStruct[,]
                    {
                        { new MyStruct(1, 2), new MyStruct(3, 4), },
                        { new MyStruct(5, 6), new MyStruct(7, 8), }
                    };
                    var r = x.MethodWithMultiDimArrayOfUserType(array);",
                "r"
                ));
        }

        [Fact]
        public async void MethodReturningMultiArrayOfUserType()
        {
            Assert.Equal((3,4), await Invoke(
                @"
                var r = x.MethodReturningMultiDimArrayOfUserType();",
                "(r[0,1].x, r[0,1].y)"
                ));
        }    }
}
