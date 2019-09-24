

// This project defines a set of types that will be rewritten by the test cases to a new
// dll named "ClonedTestSubject".  The test cases then compare the final type information of both
// assemblies to confirm everything was re-written correction

// SEE RewriteTest in Lokad.ILPack.Tests


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
    }

    interface Itf1
    {
    }
    interface Itf2
    {
    }

    /// <summary> Challenging the interface metadata ordering. </summary>
    class MyImpl : Itf2, Itf1
    {
    }
}
