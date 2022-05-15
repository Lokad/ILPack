using System;
using System.Collections.Generic;
using System.Text;

namespace TestSubject
{
    public struct GenericStruct<T>
    {
        public T Value;
    }

    public class GenericClassBase<TBase>
    {
        public TBase BaseValue;

        public GenericClassBase() { }
    }

    public class GenericClass<T> : GenericClassBase<T>
    {
        public T Value;

        public GenericClass() { }
    }

    public partial class MyClass
    {
        private GenericStruct<int> _genericStructInt = new GenericStruct<int>() { Value = 5 };

        public GenericStruct<int> GenericStructInt => _genericStructInt;

        public GenericStruct<T> GenericStructConstructedMethod<T>(T value)
        {
            return new GenericStruct<T>() { Value = value };
        }

        private GenericClass<int> _genericClassInt = new GenericClass<int>() { Value = 5 };

        public GenericClass<int> GenericClassInt => _genericClassInt;

        public GenericClass<T> GenericClassConstructedMethod<T>(T value)
        {
            return new GenericClass<T>() { Value = value };
        }
    }
}
