

// This project defines a set of types that will be rewritten by the test cases to a new
// dll named "ClonedTestSubject".  The test cases then compare the final type information of both
// assemblies to confirm everything was re-written correction

// SEE RewriteTest in Lokad.ILPack.Tests


using System;

namespace TestSubject
{
    public partial class MyClass : IMyItf
    {
        int IMyItf.InterfaceMethod1()
        {
            return 1001;
        }

        public int InterfaceMethod2()
        {
            return 1002;
        }

        TResult IMyItf.InterfaceMethod3<TResult>(Func<TResult> f)
        {
            return f();
        }
    }

    interface Itf1
    {
    }
    interface Itf2
    {
    }
    interface Itf3 : Itf2
    {
    }

    /// <summary> Challenging the interface metadata ordering. </summary>
    class MyImpl : Itf2, Itf1
    {
    }
}
