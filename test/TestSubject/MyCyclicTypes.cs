namespace TestSubject
{
    // Illustration of circular dependencies

    // TODO: [vermorel] #87, commented-out because it breaks all the rewrite unit tests.
    // This code should be uncommented when the problem is fixed.

    //public class MyBase<T>
    //{
    //}
    //public class Derived : MyBase<Derived>
    //{
    //}

    //public class MyOtherBase
    //{
    //    public class MyNested<T>
    //    {
    //    }

    //    public void Foo()
    //    {
    //        var x = new MyNested<MyOtherBase>();
    //    }
    //}
}
