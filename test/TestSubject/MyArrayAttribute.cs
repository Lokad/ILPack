using System;

namespace TestSubject
{
    public class MyArrayAttribute : Attribute
    {
        public MyArrayAttribute(object[] value)
        {
            Value = value;
        }

        public object[] Value { get; }
    }
}
