using System;
using System.Collections.Generic;
using System.Text;

namespace TestSubject
{
    public struct GenericClass<T>
    {
        public T Value;
    }

    public partial class MyClass
    {
        private GenericClass<int> _genericInt = new GenericClass<int>() { Value = 5 };

        public GenericClass<int> GenericInt => _genericInt;

        public GenericClass<T> GenericConstructedMethod<T>(T value)
        {
            return new GenericClass<T>() { Value = value };
        }
    }
}
