namespace TestSubject
{
    public abstract class MyAbstractClass
    {
        private string _foo;
        private string _bar;

        protected MyAbstractClass(string foo, string bar = null)
        {
            _foo = foo;
            _bar = bar;
        }

        public abstract int Bar();
    }

    public sealed class MyImplementation : MyAbstractClass
    {
        public MyImplementation(string foo) : base(foo)
        {
        }

        public override int Bar()
        {
            return 1;
        }
    }
}
