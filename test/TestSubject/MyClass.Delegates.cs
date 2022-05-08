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
        public delegate void MyAction();

        public void DelegateAction(Action cb)
        {
            cb();
        }

        public void DelegateMyAction(MyAction cb)
        {
            cb();
        }

        public void DelegateActionWithParam(Action<int> cb, int val)
        {
            cb(val);
        }

        public int DelegateFunc(Func<int> cb)
        {
            return cb();
        }

        public int DelegateFuncWithParam(Func<int, int> cb, int val)
        {
            return cb(val);
        }
    }
}
