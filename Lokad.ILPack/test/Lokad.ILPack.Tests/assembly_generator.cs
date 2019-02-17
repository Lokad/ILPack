using System.IO;
using System.Reflection;
using Xunit;

namespace Lokad.ILPack.Tests
{
    public class assembly_generator
    {
        [Fact]
        public void from_emission()
        {
            var asm = SampleFactorialFromEmission.EmitAssembly(10);
            using (var generator = new AssemblyGenerator(asm))
            {
                generator.GenerateAssembly("sample_factorial.dll");
            }

            var current = Directory.GetCurrentDirectory();
            Assembly.LoadFile(Path.Combine(current, "sample_factorial.dll"));
        }
    }
}
