using System;
using System.Collections.Generic;
using System.Text;

namespace TestSubject
{
    public class MyComparableT : IComparable<MyComparableT>
    {
        public MyComparableT(int value)
        {
            Value = value;
        }

        public int Value { get; }

        public int CompareTo(MyComparableT obj)
        {
            if (obj == null) return 1;

            return obj.Value.CompareTo(this.Value);
        }

    }
}
