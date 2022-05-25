

// This project defines a set of types that will be rewritten by the test cases to a new
// dll named "ClonedTestSubject".  The test cases then compare the final type information of both
// assemblies to confirm everything was re-written correction

// SEE RewriteTest in Lokad.ILPack.Tests


using System;
using System.Collections.Generic;

namespace TestSubject
{
    public partial class MyClass : IMyItf
    {
        private struct MyNestedGenericStruct<T>
        {
            public int Test(T key, Dictionary<T, int> storage) => Test(key, storage, Callback);

            private U Test<U>(T key, Dictionary<T, U> storage, Func<T, Dictionary<T, U>, U> getter) => getter(key, storage);

            private U Callback<U>(T key, Dictionary<T, U> storage) => storage[key];
        }

        public static int PartiallyResolvedGenericMethod()
        {
            return new MyNestedGenericStruct<string>()
                .Test("A", new Dictionary<string, int> { { "A", 42 }, { "B", 21 } });
        }

        public static T StaticGenericMethod<T>(T x)
        {
            return x;
        }

        public static void StaticGenericMethodWithByRef<T>(ref T x, ref T y) //-V3013
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
