namespace TestSubject
{
    public static class MySingleton<T>
    {
        public static readonly T Instance;

        static MySingleton()
        {
            Instance = default;
        }
    }
}
