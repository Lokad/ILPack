using System;

namespace TestSubject
{
    public class MyArrayAttribute : Attribute
    {
        public MyArrayAttribute(string[] values)
        {
            Values = values;
        }

        public string[] Values { get; }
    }
}
