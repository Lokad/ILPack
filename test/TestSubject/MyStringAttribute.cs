using System;

namespace TestSubject
{
    public class MyStringAttribute : Attribute
    {
        public MyStringAttribute(string value)
        {
            Value = value;
        }

        public string Value { get; }
    }
}
