using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace Lokad.ILPack.Tests
{
    public class AssemblyGeneratorTest
    {
        private static void SerializeAndVerify(Assembly asm, string fileName)
        {
            var current = Directory.GetCurrentDirectory();
            var path = Path.Combine(current, fileName);

            using (var generator = new AssemblyGenerator(asm))
            {
                generator.GenerateAssembly(path);
            }

            // Unfortunately, until .NET Core 3.0 we cannot unload assemblies.
            Assembly.LoadFile(path);
        }

        private void VerifyPE(string fileName, Stream peStream)
        {
            var current = Directory.GetCurrentDirectory();
            var path = Path.Combine(current, fileName);

            using (var file = File.Create(path))
            {
                peStream.Position = 0;
                peStream.CopyTo(file);
            }

            // Unfortunately, until .NET Core 3.0 we cannot unload assemblies.
            Assembly.LoadFile(path);
        }

        [Fact]
        public void TestBareMinimum()
        {
            // create assembly name
            var assemblyName = new AssemblyName {Name = "FactorialAssembly"};

            // create assembly with one module
            var newAssembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var newModule = newAssembly.DefineDynamicModule("MFactorial");

            SerializeAndVerify(newAssembly, "BareMinimum.dll");
        }

        [Fact]
        public void TestFactorial()
        {
            var asm = SampleFactorialFromEmission.EmitAssembly(10);
            SerializeAndVerify(asm, "SampleFactorial.dll");
        }

        [Fact]
        public void TestTypeSerialization()
        {
            // create assembly name
            var assemblyName = new AssemblyName {Name = "FactorialAssembly"};

            // create assembly with one module
            var newAssembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var newModule = newAssembly.DefineDynamicModule("MFactorial");

            // define a public class named "CFactorial" in the assembly
            var myType = newModule.DefineType("Namespace.CFactorial", TypeAttributes.Public);
            myType.CreateType();

            SerializeAndVerify(newAssembly, "TypeSerialization.dll");
        }
    }
}