using System;

namespace ReferencedSubject
{
    public static class MyReferencedStaticClass<T>
    {
        public static readonly T Instance;

        static MyReferencedStaticClass()
        {
            MyReferencedStaticClass<T>.Instance = default;
        }
    }
}