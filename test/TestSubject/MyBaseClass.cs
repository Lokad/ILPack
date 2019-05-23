namespace TestSubject
{
    public partial class MyBaseClass
    {
        public MyBaseClass()
        {
            CtorStringA = "none";
        }

        public MyBaseClass(string str)
        {
            CtorStringA = str;
        }
        public string CtorStringA { get; }
    }
}
