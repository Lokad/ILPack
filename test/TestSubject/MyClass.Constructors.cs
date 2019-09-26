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
        public MyClass()
        {
            CtorStringB = "none";
        }

        public MyClass(string a) : base(a)
        {
            CtorStringB = "none";
        }

        public MyClass(string a, string b) : this(a)
        {
            CtorStringB = b;
        }
        public string CtorStringB { get; }

    }

    public class ClassWithProtectedCtor<T>
    {
        protected ClassWithProtectedCtor(int foo)
        {
        }
    }

    public class ClassCallingProtectedCtor : ClassWithProtectedCtor<int>
    {
        public ClassCallingProtectedCtor(int foo) : base(foo)
        {
        }
    }
}
