using System;
using System.Threading.Tasks;

// This project defines a set of types that will be rewritten by the test cases to a new
// dll named "ClonedTestSubject".  The test cases then compare the final type information of both
// assemblies to confirm everything was re-written correction

// SEE RewriteTest in Lokad.ILPack.Tests


namespace TestSubject
{
    public partial class MyClass : IMyItf
    {
        public void VoidMethod()
        {
        }

        public int IntMethod()
        {
            return 33;
        }

        public int IntMethodWithParameters(int a, int b)
        {
            return a + b;
        }

        public int AnotherParameterlessMethod()
        {
            // Argless methods must be rewritten using the next available param id
            // even if they have not parameters.
            return 0;
        }

        public int AnotherMethodWithParams(int a, int b)
        {
            return a + b;
        }

    }
}
