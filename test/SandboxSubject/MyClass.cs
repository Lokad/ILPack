using System;

namespace SandboxSubject
{
    public class MyClass
    {
        public static T StaticGenericMethod<T>(T x)
        {
            return x;
        }

        /*
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

        public async Task<int> AsyncMethod(int x, int y)
        {
            await Task.Delay(100);
            return x + y;
        }
        */
    }
}
