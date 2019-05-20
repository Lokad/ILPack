using System;
using System.Threading.Tasks;

// This project defines a set of types that will be rewritten by the test cases to a new
// dll named "ClonedTestSubject".  The test cases then compare the final type information of both
// assemblies to confirm everything was re-written correction

// SEE RewriteTest in Lokad.ILPack.Tests


namespace TestSubject
{
    public interface IMyItf
    {
        int InterfaceMethod1();
        int InterfaceMethod2();
    }

    public enum MyEnum
    {
        Apples = 100,
        Pears = 150,
        Bananas = 200,
    }
    public struct MyStruct
    {
        public int x;
        public int y;
    }


    public class MyClass : IMyItf
    {
        public int ReadOnlyProperty
        {
            get;
        } = 23;

        public int WriteOnlyProperty
        {
            set { }
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
            return 33;
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

        public void InvokeNoParamEvent()
        {
            NoParamEvent?.Invoke();
        }

        public void InvokeIntParamEvent(int withValue)
        {
            IntParamEvent?.Invoke(withValue);
        }

        public void ByRefParam(ref int value)
        {
            value = 34;
        }

        public void OutParam(out int value)
        {
            value = 35;
        }

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

        int IMyItf.InterfaceMethod1()
        {
            return 1001;
        }

        public int InterfaceMethod2()
        {
            return 1002;
        }

        public MyStruct GetMyStruct()
        {
            return new MyStruct()
            {
                x = 20,
                y = 30,
            };
        }

        /*
        public async Task<int> AsyncMethod(int x, int y)
        {
            await Task.Delay(100);
            return x + y;
        }
        */
    }
}
