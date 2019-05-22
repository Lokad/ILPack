using System;
using System.Collections.Generic;
using System.Text;

// This project defines a set of types that will be rewritten by the test cases to a new
// dll named "ClonedTestSubject".  The test cases then compare the final type information of both
// assemblies to confirm everything was re-written correction

// SEE RewriteTest in Lokad.ILPack.Tests

namespace TestSubject
{
    public static class MyStringExtensions
    {
        public static string Repeated(this string This)
        {
            return This + This;
        }
    }   
}
