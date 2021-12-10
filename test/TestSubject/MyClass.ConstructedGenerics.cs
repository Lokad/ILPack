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
        private GenericClass<int> _genericInt = new() { Value = 5 };

        public GenericClass<int> GenericInt => _genericInt;

        public GenericClass<T> GenericMethod<T>(T value)
        {
            return new() { Value = value };
        }
    }
}
