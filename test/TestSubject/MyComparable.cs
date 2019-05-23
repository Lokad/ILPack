using System;
using System.Collections.Generic;
using System.Text;

namespace TestSubject
{
    public class MyComparable : IComparable
    {
        public MyComparable(int value)
        {
            Value = value;
        }

        public int Value { get; }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            if (obj is MyComparable other)
            {
                return other.Value.CompareTo(this.Value);
            }

            throw new ArgumentException("Object is not a MyComparable");
        }

    }
}
