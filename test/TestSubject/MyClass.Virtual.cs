

// This project defines a set of types that will be rewritten by the test cases to a new
// dll named "ClonedTestSubject".  The test cases then compare the final type information of both
// assemblies to confirm everything was re-written correction

// SEE RewriteTest in Lokad.ILPack.Tests


namespace TestSubject
{
    public partial class MyClass : MyBaseClass
    {
        public override string AbstractMethod()
        {
            return "MyClass.AbstractMethod";
        }

        public override string VirtualMethod()
        {
            return "MyClass.VirtualMethod";
        }

        public new string HiddenMethod()
        {
            return "MyClass.HiddenMethod";
        }

        public virtual new string HiddenVirtualMethod()
        {
            return "MyClass.HiddenVirtualMethod";
        }
    }
}
