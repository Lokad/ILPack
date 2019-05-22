﻿using System;
using System.Threading.Tasks;

// This project defines a set of types that will be rewritten by the test cases to a new
// dll named "ClonedTestSubject".  The test cases then compare the final type information of both
// assemblies to confirm everything was re-written correction

// SEE RewriteTest in Lokad.ILPack.Tests


namespace TestSubject
{
    public partial class MyClass
    {
        public int MethodWithSZArray(int[] vals)
        {
            int sum = 0;
            for (int i = 0; i < vals.Length; i++)
            {
                sum += vals[i];
            }
            return sum;
        }

        public int[] MethodReturningSZArray()
        {
            return new int[] { 1, 2, 3 };
        }

        public int MethodWithMultiDimArray(int[,,] vals)
        {
            int sum = 0;
            for (int i = 0; i < vals.GetLength(0); i++)
            {
                for (int j = 0; j < vals.GetLength(1); j++)
                {
                    for (int k = 0; k < vals.GetLength(2); k++)
                    {
                        sum += vals[i, j, k];
                    }
                }
            }
            return sum;
        }

        public int[,,] MethodReturningMultiDimArray()
        {
            return new int[,,]
            {
                { { 1, 2 }, { 3, 4 }, {5, 6 } },
                { { 7, 8 }, { 9, 10 }, {11, 12 } },
            };
        }
    }
}
