

// This project defines a set of types that will be rewritten by the test cases to a new
// dll named "ClonedTestSubject".  The test cases then compare the final type information of both
// assemblies to confirm everything was re-written correction

// SEE RewriteTest in Lokad.ILPack.Tests


namespace TestSubject
{
    public partial class MyClass
    {
        [MyAttribute(
            10, 20, 30, 
            Named = "ILPack", 
            NamedArray = new int[] { 40, 50, 60 } 
            )]
        public void AttributeArrayTest()
        {
        }

        [MyAttribute(
            1, 2, 3,
            Named = "ILPack",
            NamedArray = new int[] { 40, 50, 60 }
            )]
        public int AttributeOnProperty { get; set; }

        [MyStringAttribute(null)]
        public void AttributeNullStringTest()
        {
        }

        [MyArrayAttribute(null)]
        public void AttributeNullArrayTest()
        {
        }

        [MyArrayAttribute(new string[] { null })]
        public void AttributeNullArrayValueTest()
        {
        }
    }
}
