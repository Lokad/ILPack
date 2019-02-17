using System.IO;
using System.Reflection;
using Xunit;

namespace Lokad.ILPack.Tests
{
    public class AssemblyGeneratorTest
    {
        [Fact]
        public void FromEmission()
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