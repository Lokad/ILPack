using System;
using System.Collections.Generic;
using System.Text;

namespace TestSubject
{
    public class MyAttribute : Attribute
    {
        public int[] Values { get; set; }

        public MyAttribute(params int[] values)
        {
           this.Values = values;
        }

        public string Named { get; set; }

        public int[] NamedArray { get; set; }
    }   
}
