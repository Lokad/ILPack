using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
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

        private static void VerifyAssembly(string path)
        {
            // Unfortunately, until .NET Core 3.0 we cannot unload assemblies.
            var asm = Assembly.LoadFile(path);
            var types = asm.GetTypes(); // force to access metadata
        }

        private static string SerializeAndVerifyAssembly(Assembly asm, string fileName)
        {
            var path = SerializeAssembly(asm, fileName);
            VerifyAssembly(path);
            return path;
        }

        private string SerializeGenericsLibrary(string fileName)
        {
            // Define assembly and module
            var assemblyName = new AssemblyName {Name = "GenericsAssembly"};
            var newAssembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var newModule = newAssembly.DefineDynamicModule("GenericsModule");

            // Define a generics type which have the following signature:
            //
            // namespace Namespace {
            //   public interface INoise<TFirst, TSecond> 
            //     where TFirst : class, new()
            //     where TSecond : IEnumerable, ISerializable, Random
            //   }
            // }
            //
            var myType = newModule.DefineType("Namespace.INoise",
                TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);

            var typeParamNames = new[] {"TFirst", "TSecond"};
            var typeParams = myType.DefineGenericParameters(typeParamNames);

            var paramFirst = typeParams[0];
            var paramSecond = typeParams[1];

            // Apply constraints to first type parameters. It must be
            // a reference type and must have a parameterless constructor.
            paramFirst.SetGenericParameterAttributes(
                GenericParameterAttributes.DefaultConstructorConstraint |
                GenericParameterAttributes.ReferenceTypeConstraint);

            // Apply constraints to second type parameters.
            // It must implement IEnumerable and ISerializable, and
            // inherit from Random class.

            var baseType = typeof(object);
            var interfaceA = typeof(IEnumerable);
            var interfaceB = typeof(ISerializable);

            paramSecond.SetBaseTypeConstraint(baseType);
            var interfaceTypes = new[] {interfaceA, interfaceB};
            paramSecond.SetInterfaceConstraints(interfaceTypes);

            myType.CreateType();

            // Now define another class which references a runtime generics type
            var listType = typeof(List<float>);
            var vectorType = newModule.DefineType("Namespace.Vector", TypeAttributes.Public);
            vectorType.DefineField("Elements", listType, FieldAttributes.Public);
            vectorType.CreateType();

            return SerializeAssembly(newAssembly, fileName);
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
        public void TestFieldAccessSerialization()
        {
            // Define assembly and module
            var assemblyName = new AssemblyName {Name = "MyAssembly"};
            var newAssembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var newModule = newAssembly.DefineDynamicModule("MyModule");

            // Define following class for field access test
            //
            // namespace Namespace {
            //   public class MyClass {
            //     private bool _flag;
            //
            //     public void Test() {
            //       _flag = true;
            //     }
            //   }
            // }
            //
            var myType = newModule.DefineType("Namespace.MyClass", TypeAttributes.Public);
            var myField = myType.DefineField("_flag", typeof(bool), FieldAttributes.Private);
            var myMethod = myType.DefineMethod("Test", MethodAttributes.Public);

            var generator = myMethod.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldc_I4_1);
            generator.Emit(OpCodes.Stfld, myField);
            generator.Emit(OpCodes.Ret);

            myType.CreateType();

            SerializeAndVerifyAssembly(newAssembly, "TestFieldAccess.dll");
        }

        [Fact]
        public void TestGenericsType()
        {
            var path = SerializeGenericsLibrary("GenericsSerialization.dll");
            VerifyAssembly(path);
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