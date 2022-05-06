namespace TestSubject
{
    public readonly struct StringSpan
    {
        public readonly string Source;
        public readonly int Length;
        public readonly int Offset;

        public StringSpan(string source, int offset, int length)
        {
            this.Source = source;
            this.Length = length;
            this.Offset = offset;
        }
    }

    public class MyClassWithInModifier
    {
        public virtual string Print(in StringSpan text)
        {
            return text.Source.Substring(text.Offset, text.Length);
        }        
    }
}
