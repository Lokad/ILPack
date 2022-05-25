using System;
using System.Collections.Generic;

namespace TestSubject
{
    internal unsafe class MyUnsafe<T>
    {
        public byte MethodWithSimpleCallback()
        {
            return MethodWithSimpleCallback(&SimpleCallback);
        }

        public string MethodWithModifiersCallback()
        {
            return MethodWithModifiersCallback(&ModifiersCallback);
        }

        public int MethodWithGenericCallback(T key, Dictionary<T, int> storage)
        {
            return MethodWithGenericCallback<int>(storage, key, &GenericCallback);
        }

        private U MethodWithGenericCallback<U>(
            Dictionary<T, U> storage, T key, delegate*<T, Dictionary<T, U>, U> getter)
        {
            return getter(key, storage);
        }

        private string MethodWithModifiersCallback(delegate*<in short[], ref byte[,], string> f)
        {
            var b = new byte[1,1];
            return f(Array.Empty<short>(), ref b);
        }

        private byte MethodWithSimpleCallback(delegate*<int*, bool, object, IntPtr, byte> f)
        {
            return f(null, true, null, IntPtr.Zero);
        }

        private static string ModifiersCallback(in short[] x, ref byte[,] y)
        {
            return "Hello";
        }

        private static byte SimpleCallback(int* a, bool b, object c, IntPtr d)
        {
            return 42;
        }

        private static U GenericCallback<U>(T key, Dictionary<T, U> storage)
        {
            return storage[key];
        }
    }
}
