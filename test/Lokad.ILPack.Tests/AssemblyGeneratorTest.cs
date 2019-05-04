using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
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

            var generator = new AssemblyGenerator();
            generator.GenerateAssembly(asm, path);

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

        private static PropertyBuilder CreateProperty(TypeBuilder typeBuilder, Type propertyType, string propertyName,
            bool hasGetter, bool hasSetter)
        {
            if (!hasGetter && !hasSetter)
            {
                throw new ArgumentException("A property should have at least a getter or setter.");
            }

            // Define backing field
            var backingFieldName = $"<${propertyName}>k__BackingField";
            var backingField = typeBuilder.DefineField(backingFieldName, propertyType, FieldAttributes.Private);

            // Define property
            var propertyBuilder =
                typeBuilder.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);

            if (hasGetter)
            {
                // Define get method
                var propertyGetterName = $"get_${propertyName}";
                var propertyGetter = typeBuilder.DefineMethod(propertyGetterName, MethodAttributes.Public, propertyType,
                    Type.EmptyTypes);

                var il = propertyGetter.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, backingField);
                il.Emit(OpCodes.Ret);

                propertyBuilder.SetGetMethod(propertyGetter);
            }

            if (hasSetter)
            {
                // Define set method
                var propertySetterName = $"set_${propertyName}";
                var propertySetter = typeBuilder.DefineMethod(propertySetterName, MethodAttributes.Public, propertyType,
                    Type.EmptyTypes);

                var il = propertySetter.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stfld, backingField);
                il.Emit(OpCodes.Ret);

                propertyBuilder.SetSetMethod(propertySetter);
            }

            return propertyBuilder;
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
        public void TestBasicInheritance()
        {
            // Define assembly and module
            var assemblyName = new AssemblyName {Name = "MyAssembly"};
            var newAssembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var newModule = newAssembly.DefineDynamicModule("MyModule");

            // Define following dependent types:
            //
            // namespace Namespace {
            //   public class Animal {}
            //   public class Cat : Animal {}
            //   public class Dog : Animal {}
            //   public class Purrfect : Cat {}
            //   public class Furdinand : Dog {}
            // }
            //

            var animalTypeBuilder = newModule.DefineType("Namespace.Animal", TypeAttributes.Public);
            var animalType = animalTypeBuilder.CreateType();

            var catTypeBuilder = newModule.DefineType("Namespace.Cat", TypeAttributes.Public, animalType);
            var catType = catTypeBuilder.CreateType();

            var dogTypeBuilder = newModule.DefineType("Namespace.Dog", TypeAttributes.Public, animalType);
            var dogType = dogTypeBuilder.CreateType();

            var purrfectTypeBuilder = newModule.DefineType("Namespace.Purrfect", TypeAttributes.Public, catType);
            purrfectTypeBuilder.CreateType();

            var furdinandTypeBuilder = newModule.DefineType("Namespace.Furdinand", TypeAttributes.Public, dogType);
            furdinandTypeBuilder.CreateType();

            SerializeAndVerifyAssembly(newAssembly, "TestBasicInheritance.dll");
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
        public void TestInlineConstructorReference()
        {
            // Define assembly and module
            var assemblyName = new AssemblyName {Name = "MyAssembly"};
            var newAssembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var newModule = newAssembly.DefineDynamicModule("MyModule");

            // Define following class to test inline constructor reference.
            // Notice that System.Net.Http.HttpClient type which is a referenced type isn't referenced
            // at anywhere except method body. So, during serialization of IL instructions,
            // first we need to resolve referenced type, then its constructor reference.
            //
            // public class MyClass
            // {
            //   public IDisposable MyMethod()
            //   {
            //     return new System.Net.Http.HttpClient();
            //   }
            // }
            //
            var httpClientType = typeof(HttpClient);
            var httpClientTypeCtor = httpClientType.GetConstructor(Type.EmptyTypes);

            // Define a type with no namespace
            var myType = newModule.DefineType("MyClass", TypeAttributes.Public);

            // Define a method which returns a new instance of HttpClient as IDisposable type.
            // So, we can mask type reference until relevant instruction is processed.
            var myMethod =
                myType.DefineMethod("MyMethod", MethodAttributes.Public, typeof(IDisposable), Type.EmptyTypes);
            var generator = myMethod.GetILGenerator();

            generator.Emit(OpCodes.Newobj, httpClientTypeCtor);
            generator.Emit(OpCodes.Ret);

            myType.CreateType();

            SerializeAndVerifyAssembly(newAssembly, "InlineConstructorReference.dll");
        }

        [Fact]
        public void TestMethodReferencingSerialization()
        {
            // Define assembly and module
            var assemblyName = new AssemblyName {Name = "MyAssembly"};
            var newAssembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var newModule = newAssembly.DefineDynamicModule("MyModule");

            // Define following code to test method referencing from external assembly.
            //
            // namespace Namespace {
            //   public class Greeter {
            //     public void Greet() {
            //       Console.WriteLine("Hello, World!");
            //     }
            //   }
            // }
            //

            var myType = newModule.DefineType("Namespace.Greeter", TypeAttributes.Public);

            var myMethod = myType.DefineMethod("Greet", MethodAttributes.Public);
            var generator = myMethod.GetILGenerator();

            generator.Emit(OpCodes.Ldstr, "Hello, World!");
            generator.Emit(OpCodes.Call, typeof(Console).GetMethod(nameof(Console.WriteLine), new[] {typeof(string)}));
            generator.Emit(OpCodes.Ret);

            myType.CreateType();

            SerializeAndVerifyAssembly(newAssembly, "MethodReferencingSerialization.dll");
        }

        [Fact]
        public void TestNullStrings()
        {
            // Define assembly and module
            var assemblyName = new AssemblyName {Name = "MyAssembly"};
            var newAssembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var newModule = newAssembly.DefineDynamicModule("MyModule");

            // Define following type to test null strings (no namespace) and anonymous types
            //
            // public class MyClass {
            //   public object MyMethod() {
            //     return new {
            //     };
            //   }
            // }
            //

            // Define anonymous type (technically, it's compiler generated type with no namespace in .NET world)
            var anonymousType = newModule.DefineType("<>f__AnonymousType0", TypeAttributes.NotPublic);
            var anonymousTypeCtor = anonymousType.DefineDefaultConstructor(MethodAttributes.Public);

            // Define a type with no namespace
            var myType = newModule.DefineType("MyClass", TypeAttributes.Public);

            // Define a method to just return a new instance of anonymous type.
            var myMethod = myType.DefineMethod("MyMethod", MethodAttributes.Public, typeof(object), Type.EmptyTypes);
            var generator = myMethod.GetILGenerator();

            generator.Emit(OpCodes.Newobj, anonymousTypeCtor);
            generator.Emit(OpCodes.Ret);

            anonymousType.CreateType();
            myType.CreateType();

            SerializeAndVerifyAssembly(newAssembly, "NullStringsSerialization.dll");
        }

        [Fact]
        public void TestPropertySerialization()
        {
            // Define assembly and module
            var assemblyName = new AssemblyName {Name = "MyAssembly"};
            var newAssembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var newModule = newAssembly.DefineDynamicModule("MyModule");

            // Define following class to test inline constructor reference.
            // Notice that System.Net.Http.HttpClient type which is a referenced type isn't referenced
            // at anywhere except method body. So, during serialization of IL instructions,
            // first we need to resolve referenced type, then its constructor reference.
            //
            // public class Vector
            // {
            //   public float X { get; set; }
            //   public float Y { get; set; }
            //   public float Sum() {
            //     return X + Y;
            //   }
            // }
            //

            // Define a type with no namespace
            var vectorType = newModule.DefineType("Vector", TypeAttributes.Public);

            // Define properties
            var xProperty = CreateProperty(vectorType, typeof(float), "X", true, true);
            var yProperty = CreateProperty(vectorType, typeof(float), "Y", true, true);

            // Define a method which returns sum of components.
            var sumMethod = vectorType.DefineMethod("Sum", MethodAttributes.Public, typeof(float), Type.EmptyTypes);
            var generator = sumMethod.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Call, xProperty.GetGetMethod());
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Call, yProperty.GetGetMethod());
            generator.Emit(OpCodes.Add);
            generator.Emit(OpCodes.Ret);

            vectorType.CreateType();

            SerializeAndVerifyAssembly(newAssembly, "PropertySerialization.dll");
        }

        [Fact]
        public void TestSelfReferencingSerialization()
        {
            // Define assembly and module
            var assemblyName = new AssemblyName {Name = "MyAssembly"};
            var newAssembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var newModule = newAssembly.DefineDynamicModule("MyModule");

            // Define following singleton pattern to test self referencing.
            //
            // namespace Namespace {
            //   public class MyClass {
            //     private static MyClass _instance;
            //
            //     public static MyClass GetInstance() {
            //       if (_instance == null) {
            //         _instance = new MyClass();
            //       }
            //       return _instance;
            //     }
            //   }
            // }
            //

            var myType = newModule.DefineType("Namespace.MyClass", TypeAttributes.Public);
            var myField = myType.DefineField("_instance", myType, FieldAttributes.Private | FieldAttributes.Static);
            var ctor = myType.DefineDefaultConstructor(MethodAttributes.Private);

            var myMethod = myType.DefineMethod("GetInstance", MethodAttributes.Public | MethodAttributes.Static, myType,
                Type.EmptyTypes);
            var generator = myMethod.GetILGenerator();
            var isInitializedLabel = generator.DefineLabel();

            generator.Emit(OpCodes.Ldsfld, myField);
            generator.Emit(OpCodes.Brtrue_S, isInitializedLabel);

            generator.Emit(OpCodes.Newobj, ctor);
            generator.Emit(OpCodes.Stsfld, myField);

            // isInitialized:
            generator.MarkLabel(isInitializedLabel);
            generator.Emit(OpCodes.Ldsfld, myField);
            generator.Emit(OpCodes.Ret);

            myType.CreateType();

            SerializeAndVerifyAssembly(newAssembly, "SelfReferencingSerialization.dll");
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