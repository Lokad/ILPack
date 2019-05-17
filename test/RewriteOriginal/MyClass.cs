using System;

// This project defines a set of types that will be rewritten by the test cases to a new
// dll named "RewriteClone".  The test cases then compare the final type information of both
// assemblies to confirm everything was re-written correction

// SEE RewriteTest in Lokad.ILPack.Tests


namespace RewriteOriginal
{
    public class MyClass
    {
        public int ReadOnlyProperty
        {
            get;
        }

        public int WriteOnlyProperty
        {
            get;
        }

        public int ReadWriteProperty
        {
            get;
            set;
        }

        public void VoidMethod()
        {
        }

        public int IntMethod()
        {
            return 0;
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

        public event Action NoParamEvent;
        public event Action<int> IntParamEvent;
    }
}
