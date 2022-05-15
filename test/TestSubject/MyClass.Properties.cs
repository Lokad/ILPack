

// This project defines a set of types that will be rewritten by the test cases to a new
// dll named "ClonedTestSubject".  The test cases then compare the final type information of both
// assemblies to confirm everything was re-written correction

// SEE RewriteTest in Lokad.ILPack.Tests


namespace TestSubject
{
    public partial class MyClass
    {
        public int ReadOnlyProperty
        {
            get;
        } = 23;

        public int WriteOnlyProperty
        {
            set { }
        }

        public int ReadWriteProperty
        {
            get;
            set;
        }

        private int PrivateProperty
        {
            get;
            set;
        }

        public string WrappedSingleton
        {
            get
            {
                return MySingleton<string>.Instance;
            }
        }
    }
}
