using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace Lokad.ILPack.Tests
{
    public class AssemblyGeneratorTest
    {
        private static string SerializeAssembly(Assembly asm, string fileName)
        {
            var current = Directory.GetCurrentDirectory();
            var path = Path.Combine(current, fileName);

            using (var generator = new AssemblyGenerator(asm))
            {
                generator.GenerateAssembly(path);
            }

            return path;
        }

        private static string SerializeAndVerifyAssembly(Assembly asm, string fileName)
        {
            var path = SerializeAssembly(asm, fileName);

            // Unfortunately, until .NET Core 3.0 we cannot unload assemblies.
            var asmFromDisk = Assembly.LoadFile(path);
            var types = asmFromDisk.GetTypes(); // force to access metadata

            return path;
        }

        [Fact]
        public void TestBareMinimum()
        {
            // create assembly name
            var assemblyName = new AssemblyName {Name = "FactorialAssembly"};

            // create assembly with one module
            var newAssembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var newModule = newAssembly.DefineDynamicModule("MFactorial");

            SerializeAndVerifyAssembly(newAssembly, "BareMinimum.dll");
        }

        [Fact]
        public void TestFactorial()
        {
            var asm = SampleFactorialFromEmission.EmitAssembly(10);
            SerializeAndVerifyAssembly(asm, "SampleFactorial.dll");
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

            SerializeAndVerifyAssembly(newAssembly, "TypeSerialization.dll");
        }
    }
}