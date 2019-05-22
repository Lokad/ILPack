using System;
using System.Threading.Tasks;

// This project defines a set of types that will be rewritten by the test cases to a new
// dll named "ClonedTestSubject".  The test cases then compare the final type information of both
// assemblies to confirm everything was re-written correction

// SEE RewriteTest in Lokad.ILPack.Tests


namespace TestSubject
{
    public partial class MyClass
    {
        public string MethodWithVarArgs(params string[] values)
        {
            return string.Join("", values);
        }

        public string MethodWithFixedAndVarArgs(int number, params string[] values)
        {
            return $"{number}: {string.Join("", values)}";
        }
    }
}
