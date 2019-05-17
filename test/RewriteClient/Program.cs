using RewriteOriginal;
using System;

namespace RewriteClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var x = new MyClass();
            Console.WriteLine($"ReadOnlyProperty: {x.ReadOnlyProperty}");
            x.WriteOnlyProperty = 24;
            x.ReadWriteProperty = 25;
            Console.WriteLine($"ReadWriteProperty: {x.ReadWriteProperty}");
            x.VoidMethod();
            Console.WriteLine($"IntMethod(): {x.IntMethod()}");
            Console.WriteLine($"IntMethodWithParameters(): {x.IntMethodWithParameters(10, 20)}");
            x.NoParamEvent += () => Console.WriteLine("--> NoParamEvent triggered");
            x.InvokeNoParamEvent();
            x.IntParamEvent += (val) => Console.WriteLine($"--> IntParamEvent triggered ({val})");
            x.InvokeIntParamEvent(26);
        }
    }
}
