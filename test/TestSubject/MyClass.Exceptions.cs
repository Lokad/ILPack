using System;

// This project defines a set of types that will be rewritten by the test cases to a new
// dll named "ClonedTestSubject".  The test cases then compare the final type information of both
// assemblies to confirm everything was re-written correction

// SEE RewriteTest in Lokad.ILPack.Tests


namespace TestSubject
{
    public partial class MyClass
    {
        public bool ThrowGuardedException()
        {
            try
            {
                throw new InvalidOperationException();
            }
            catch (InvalidOperationException)
            {
                return true;
            }
        }

        public void ThrowGuardedExceptionWithFinally(ref int hitPoints)
        {
            try
            {
                hitPoints |= 0b0000_0001;
                throw new InvalidOperationException();
            }
            catch (InvalidOperationException)
            {
                hitPoints |= 0b0000_0010;
                return;
            }
            finally
            {
                hitPoints |= 0b0000_0100;
            }
        }

        public void ThrowNestedGuardedExceptionWithFinally(ref int hitPoints)
        {
            try
            {

                hitPoints |= 0b0000_0001;

                try
                {
                    hitPoints |= 0b0001_0000;
                    throw new InvalidOperationException();
                }
                catch (InvalidOperationException)
                {
                    hitPoints |= 0b0010_0000;
                    throw;
                }
                finally
                {
                    hitPoints |= 0b0100_0000;
                }

            }
            catch (InvalidOperationException)
            {
                hitPoints |= 0b0000_0010;
                return;
            }
            finally
            {
                hitPoints |= 0b0000_0100;
            }
        }


        public void ThrowGuardedExceptionWithUntypedCatchAndFinally(ref int hitPoints)
        {
            try
            {
                hitPoints |= 0b0000_0001;
                throw new InvalidOperationException();
            }
            catch
            {
                hitPoints |= 0b0000_0010;
                return;
            }
            finally
            {
                hitPoints |= 0b0000_0100;
            }
        }

    }
}
