// This project defines a set of types that will be rewritten by the test cases to a new
// dll named "ClonedTestSubject".  The test cases then compare the final type information of both
// assemblies to confirm everything was re-written correction

// SEE RewriteTest in Lokad.ILPack.Tests


namespace TestSubject
{
    public partial struct MyStruct
    {
        // Binary operators
        public static MyStruct operator +(MyStruct a, MyStruct b)
        {
            return new MyStruct(a.x + b.x, a.y + b.y);
        }

        // Unary operator
        public static MyStruct operator -(MyStruct a)
        {
            return new MyStruct(-a.x, -a.y);
        }

        // Explicit operator
        public static explicit operator double(MyStruct a)
        {
            return ((double)a.x + (double)a.y) / 2;
        }

        // Implicit operator
        public static implicit operator string (MyStruct a)
        {
            return $"[{a.x},{a.y}]";
        }
    }
}
