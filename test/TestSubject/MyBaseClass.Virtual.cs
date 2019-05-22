using System;
using System.Collections.Generic;
using System.Text;

namespace TestSubject
{
    public abstract partial class MyBaseClass
    {
        public abstract string AbstractMethod();

        public virtual string VirtualMethod()
        {
            return "MyBaseClass.VirtualMethod";
        }

        public string HiddenMethod()
        {
            return "MyBaseClass.HiddenMethod";
        }

        public virtual string HiddenVirtualMethod()
        {
            return "MyBaseClass.HiddenVirtualMethod";
        }
    }
}
