// This project defines a set of types that will be rewritten by the test cases to a new
// dll named "ClonedTestSubject".  The test cases then compare the final type information of both
// assemblies to confirm everything was re-written correction

// SEE RewriteTest in Lokad.ILPack.Tests


using System;

namespace TestSubject
{
    public interface IMyItf
    {
        int InterfaceMethod1();
        int InterfaceMethod2();

        TResult InterfaceMethod3<TResult>(Func<TResult> f);

#if NETCOREAPP3_0_OR_GREATER
        void DefaultInterfaceMethod() { }
#endif
    }
}
