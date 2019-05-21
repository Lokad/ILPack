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
        public static T StaticGenericMethod<T>(T x)
        {
            return x;
        }

        public static void StaticGenericMethodWithByRef<T>(ref T x, ref T y)
        {
            T temp = x;
            x = y;
            y = temp;
        }

        public T GenericMethod<T>(T x)
        {
            return x;
        }

        public void GenericMethodWithByRef<T>(ref T x, ref T y)
        {
            T temp = x;
            x = y;
            y = temp;
        }

    }
}
