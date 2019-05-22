// This project defines a set of types that will be rewritten by the test cases to a new
// dll named "ClonedTestSubject".  The test cases then compare the final type information of both
// assemblies to confirm everything was re-written correction

// SEE RewriteTest in Lokad.ILPack.Tests

namespace TestSubject
{
    public partial class MyClass : IMyItf
    {
        public unsafe int Pinned(int[] values)
        {
            int sum = 0;

            fixed (int* valuesPtr = &values[0])
            {
                for (int i = 0; i < values.Length; i++)
                {
                    sum += *(valuesPtr + i);
                }
            }

            return sum;
        }
    }
}
