using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using Lokad.ILPack.Metadata;
using Xunit;

namespace Lokad.ILPack.Tests
{
    public class AssemblyGeneratorTest
    {
        private static readonly string _basePath;

        static AssemblyGeneratorTest()
        {
            _basePath = Path.Combine(Directory.GetCurrentDirectory(), "generated");
            Directory.CreateDirectory(_basePath);
        }

        private static string GetPathForAssembly(string fileName) => Path.Combine(_basePath, fileName);

        private static void SerializeAssembly(Assembly assembly, string fileName)
        {
            var path = GetPathForAssembly(fileName);

            var generator = new AssemblyGenerator();
            generator.GenerateAssembly(assembly, path);
        }

        private static Type[] VerifyAssembly(string fileName)
        {
            var path = GetPathForAssembly(fileName);

            // Unfortunately, until .NET Core 3.0 we cannot unload assemblies.
            var assembly = Assembly.LoadFile(path);
            return assembly.GetTypes(); // force to access metadata
        }

        private static Type[] SerializeAndVerifyAssembly(Assembly assembly, string fileName)
        {
            SerializeAssembly(assembly, fileName);
            return VerifyAssembly(fileName);
        }

        private static Assembly LoadAssembly(string fileName)
        {
            var path = GetPathForAssembly(fileName);

            // Unfortunately, until .NET Core 3.0 we cannot unload assemblies.
            return Assembly.LoadFile(path);
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
                var propertySetter = typeBuilder.DefineMethod(propertySetterName, MethodAttributes.Public, null,
                    new Type[] { propertyType });

                var il = propertySetter.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stfld, backingField);
                il.Emit(OpCodes.Ret);

                propertyBuilder.SetSetMethod(propertySetter);
            }

            return propertyBuilder;
        }

        private static void SerializeGenericsLibrary(string fileName)
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

            SerializeAssembly(newAssembly, fileName);
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
            var assembly = SampleFactorialFromEmission.EmitAssembly(10);

            SerializeAndVerifyAssembly(assembly, "SampleFactorial.dll");
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
            SerializeGenericsLibrary("GenericsSerialization.dll");
            VerifyAssembly("GenericsSerialization.dll");
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
        public void TestMetadataFriendlyName()
        {
            var type = typeof(object);
            var ctor = type.GetConstructor(Type.EmptyTypes);
            var method = type.GetMethod("ToString");

            Assert.Equal("\"null\"", MetadataHelper.GetFriendlyName<Type>(null));
            Assert.Equal($"\"{type.AssemblyQualifiedName}\"", MetadataHelper.GetFriendlyName(type));
            Assert.Equal($"\"{ctor}\" of \"{ctor.DeclaringType.AssemblyQualifiedName}\"",
                MetadataHelper.GetFriendlyName(ctor));
            Assert.Equal($"\"{method}\" of \"{method.DeclaringType.AssemblyQualifiedName}\"",
                MetadataHelper.GetFriendlyName(method));

            var anonymousObj = new
            {
                NestedType = new
                {
                    MyField = false
                }
            };
            var anonymousType = anonymousObj.GetType();
            var anonymousNestedType = anonymousObj.NestedType.GetType();
            var anonymousNestedProperty = anonymousNestedType.GetProperty(nameof(anonymousObj.NestedType.MyField));

            Assert.Equal($"\"{anonymousType.AssemblyQualifiedName}\"", MetadataHelper.GetFriendlyName(anonymousType));
            Assert.Equal($"\"{anonymousNestedType.AssemblyQualifiedName}\"",
                MetadataHelper.GetFriendlyName(anonymousNestedType));
            Assert.Equal($"\"{anonymousNestedProperty}\" of \"{anonymousNestedType.AssemblyQualifiedName}\"",
                MetadataHelper.GetFriendlyName(anonymousNestedProperty));
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
        public void TestAssemblyAttribute()
        {
            // Define assembly and module
            var assemblyName = new AssemblyName { Name = "MyAssembly" };
            var newAssembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var newModule = newAssembly.DefineDynamicModule("MyModule");

            // [assembly: CustomAttribute]
            // [AttributeUsage(AttributeTargets.Assembly)]
            // public class CustomAttribute : Attribute { }
            var attributeTypeBuilder = newModule.DefineType("CustomAttribute", TypeAttributes.Public | TypeAttributes.Class, typeof(Attribute));
            _ = attributeTypeBuilder.DefineDefaultConstructor(MethodAttributes.Public);

            // Add [AttributeUsage(AttributeTargets.Assembly)]
            var attributeUsageTypeInfo = typeof(AttributeUsageAttribute).GetTypeInfo();
            var attributeUsageConstructorInfo = attributeUsageTypeInfo.GetConstructor(new[] { typeof(AttributeTargets) });
            attributeTypeBuilder.SetCustomAttribute(new CustomAttributeBuilder(attributeUsageConstructorInfo, new object[] { AttributeTargets.Assembly }));

            // apply to assembly
            var attributeConstructor = attributeTypeBuilder.CreateTypeInfo().GetConstructor(Array.Empty<Type>());
            newAssembly.SetCustomAttribute(new CustomAttributeBuilder(attributeConstructor, Array.Empty<Object>()));

            SerializeAndVerifyAssembly(newAssembly, "AssemblyAttribute.dll");
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

        private unsafe delegate int SubParamPtr(int* op1, int op2, int op3, int op4);

        [Theory]
        [InlineData(1, 2, 3, 4)]
        public unsafe void TestParamPtr(int op1, int op2, int op3, int op4)
        {
            /* SAVE */
            var assemblyBldr = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("AsmParamPtr"), AssemblyBuilderAccess.Run);
            var moduleBldr = assemblyBldr.DefineDynamicModule("ModParamPtr");

            var typeBldr = moduleBldr.DefineType("Ns.ClassParamPtr", TypeAttributes.Public);

            var parameterTypes = new Type[] { typeof(int*), typeof(int), typeof(int), typeof(int) };
            var returnType = typeof(int);
            var methodBldr = typeBldr.DefineMethod("SubParamPtr", MethodAttributes.Public | MethodAttributes.Static, returnType, parameterTypes);

            for (int i = 1; i <= 4; i++)
            {
                methodBldr.DefineParameter(i, ParameterAttributes.None, $"op{i}");
            }

            var ilGen = methodBldr.GetILGenerator();

            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldind_I4);
            ilGen.Emit(OpCodes.Ldarg_1);
            ilGen.Emit(OpCodes.Add);
            ilGen.Emit(OpCodes.Ldarg_2);
            ilGen.Emit(OpCodes.Add);
            ilGen.Emit(OpCodes.Ldarg_3);
            ilGen.Emit(OpCodes.Add);
            ilGen.Emit(OpCodes.Ret);

            typeBldr.CreateType();

            SerializeAssembly(assemblyBldr, "TestParamPtr.dll");

            /* LOAD */
            var assembly = LoadAssembly("TestParamPtr.dll");

            var type = assembly.GetType("Ns.ClassParamPtr");

            var methodInfo = type.GetMethod("SubParamPtr", BindingFlags.Static | BindingFlags.Public);

            var parameters = methodInfo.GetParameters();

            var subParamPtr = (SubParamPtr)methodInfo.CreateDelegate(typeof(SubParamPtr));

            /* TESTS */
            for (int i = 0; i < parameters.Length; i++)
            {
                Assert.Equal($"op{i + 1}", parameters[i].Name);
                Assert.Equal(i == 0 ? typeof(int*) : typeof(int), parameters[i].ParameterType);
                Assert.Equal(i, parameters[i].Position);
            }

            Assert.Equal(op1 + op2 + op3 + op4, subParamPtr(&op1, op2, op3, op4));
        }

        private delegate int SubLocalByRef();

        [Theory]
        [InlineData(256)]
        public void TestLocalByRef(int value)
        {
            /* SAVE */
            var assemblyBldr = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("AsmLocalByRef"), AssemblyBuilderAccess.Run);
            var moduleBldr = assemblyBldr.DefineDynamicModule("ModLocalByRef");

            var typeBldr = moduleBldr.DefineType("Ns.ClassLocalByRef", TypeAttributes.Public);

            var methodBldr = typeBldr.DefineMethod("SubLocalByRef", MethodAttributes.Public | MethodAttributes.Static, typeof(int), null);

            var ilGen = methodBldr.GetILGenerator();

            var val = ilGen.DeclareLocal(typeof(int));
            ilGen.DeclareLocal(typeof(int).MakeByRefType()); // refToVal

            ilGen.Emit(OpCodes.Ldc_I4, value);
            ilGen.Emit(OpCodes.Stloc_0);
            ilGen.Emit(OpCodes.Ldloca_S, val);
            ilGen.Emit(OpCodes.Stloc_1);
            ilGen.Emit(OpCodes.Ldloc_1);
            ilGen.Emit(OpCodes.Ldloc_0);
            ilGen.Emit(OpCodes.Ldc_I4_2);
            ilGen.Emit(OpCodes.Mul);
            ilGen.Emit(OpCodes.Stind_I4);
            ilGen.Emit(OpCodes.Ldloc_0);
            ilGen.Emit(OpCodes.Ret);

            typeBldr.CreateType();

            SerializeAssembly(assemblyBldr, "TestLocalByRef.dll");

            /* LOAD */
            var assembly = LoadAssembly("TestLocalByRef.dll");

            var type = assembly.GetType("Ns.ClassLocalByRef");

            var methodInfo = type.GetMethod("SubLocalByRef", BindingFlags.Static | BindingFlags.Public);

            var locals = methodInfo.GetMethodBody().LocalVariables;

            var subLocalByRef = (SubLocalByRef)methodInfo.CreateDelegate(typeof(SubLocalByRef));

            /* TESTS */
            Assert.True(locals[0].LocalIndex == 0 && locals[0].LocalType == typeof(int));
            Assert.True(locals[1].LocalIndex == 1 && locals[1].LocalType == typeof(int).MakeByRefType());

            Assert.Equal(value * 2, subLocalByRef());
        }

        private static bool IsTinyMethod(MethodBase methodBase, bool hasDynamicStackAllocation = false)
        {
            var body = methodBase?.GetMethodBody() ?? throw new ArgumentNullException(nameof(methodBase));

            return body.GetILAsByteArray().Length < 64 && body.MaxStackSize <= 8 &&
                body.LocalSignatureMetadataToken == 0 && (!hasDynamicStackAllocation || !body.InitLocals) &&
                body.ExceptionHandlingClauses.Count == 0;
        }

        [Fact]
        public void TestIsTiny()
        {
            /* SAVE */
            var assemblyBldr = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("AsmIsTiny"), AssemblyBuilderAccess.Run);
            var moduleBldr = assemblyBldr.DefineDynamicModule("ModIsTiny");

            var typeBldr = moduleBldr.DefineType("Ns.ClassIsTiny", TypeAttributes.Public);

            var parameterTypes = new Type[] { typeof(int), typeof(int) };
            var returnType = typeof(int);
            var methodBldr = typeBldr.DefineMethod("SubIsTiny", MethodAttributes.Public | MethodAttributes.Static, returnType, parameterTypes);

            methodBldr.DefineParameter(1, ParameterAttributes.None, null);
            methodBldr.DefineParameter(2, ParameterAttributes.None, null);

            var ilGen = methodBldr.GetILGenerator();

            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldarg_1);
            ilGen.Emit(OpCodes.Xor);
            ilGen.Emit(OpCodes.Ret);

            typeBldr.CreateType();

            SerializeAssembly(assemblyBldr, "TestIsTiny.dll");

            /* LOAD */
            var assembly = LoadAssembly("TestIsTiny.dll");

            var type = assembly.GetType("Ns.ClassIsTiny");

            var methodInfo = type.GetMethod("SubIsTiny", BindingFlags.Static | BindingFlags.Public);
            var constructorInfo = type.GetConstructor(Type.EmptyTypes); // Default constructor.

            /* TESTS */
            Assert.True(IsTinyMethod(methodInfo));
            Assert.True(IsTinyMethod(constructorInfo));
        }

        [Fact]
        public void TestInterpolatedStrings()
        {
            var assemblyName = new AssemblyName { Name = "MyAssembly" };
            var newAssembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var newModule = newAssembly.DefineDynamicModule("MyModule");

            // Define a type with no namespace
            var myType = newModule.DefineType("MyClass", TypeAttributes.Public);

            // Define a method to just return a new instance of anonymous type.
            var myMethod = myType.DefineMethod("MyMethod", MethodAttributes.Public, typeof(string), Type.EmptyTypes);
            var generator = myMethod.GetILGenerator();
            var builder = generator.DeclareLocal(typeof(System.Runtime.CompilerServices.DefaultInterpolatedStringHandler));
            generator.Emit(OpCodes.Ldloca, builder.LocalIndex);
            generator.Emit(OpCodes.Ldc_I4, 0);
            generator.Emit(OpCodes.Ldc_I4, 0);

            var DefaultInterpolatedStringHandlerConstructorInfo = builder.LocalType.GetConstructor(new[] { typeof(int), typeof(int) })!;
            var ToStringAndClearInfo = builder.LocalType.GetMethod("ToStringAndClear", Type.EmptyTypes)!;

            generator.Emit(OpCodes.Call, DefaultInterpolatedStringHandlerConstructorInfo);
            generator.Emit(OpCodes.Ldloca, builder.LocalIndex);
            generator.Emit(OpCodes.Call, ToStringAndClearInfo);
            generator.Emit(OpCodes.Ret);

            myType.CreateType();

            SerializeAndVerifyAssembly(newAssembly, "InterpolatedStringsSerialization.dll");
        }

        [Fact]
        public void TestSpecialCharacters()
        {
            /* SAVE */
            var assemblyName = new AssemblyName { Name = "Assembly+" };

            var newAssembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var newModule = newAssembly.DefineDynamicModule("Assembly+");

            var myType = newModule.DefineType("Type+", TypeAttributes.Public);
            myType.CreateType();

            SerializeAndVerifyAssembly(newAssembly, "Assembly+.dll");

            /* LOAD */
            var assembly = LoadAssembly("Assembly+.dll");
            var type = assembly.GetType("Type\\+");
            Assert.NotNull(type);
        }
                
        [Fact]
        public void TestMoreSpecialCharacters()
        {
            /* SAVE */
            var assemblyName = new AssemblyName { Name = "Assembly++" };

            var newAssembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var newModule = newAssembly.DefineDynamicModule("Assembly++");

            var myType = newModule.DefineType("A.B*C.D&E", TypeAttributes.Public);
            myType.CreateType();
            
            var nested = myType.DefineNestedType("F", TypeAttributes.NestedPublic);
            nested.CreateType();

            SerializeAndVerifyAssembly(newAssembly, "Assembly++.dll");

            /* LOAD */
            Assembly assembly = LoadAssembly("Assembly++.dll");
            Type type = assembly.GetType(@"A.B\*C.D\&E+F");
            Assert.NotNull(type);
        }

        [Fact]
        public void TestUnescape()
        {
            Assert.Equal(@"", AssemblyGenerator.Unescape(@""));
            Assert.Equal(@"x", AssemblyGenerator.Unescape(@"x"));
            Assert.Equal(@"\", AssemblyGenerator.Unescape(@"\"));
            Assert.Equal(@"\", AssemblyGenerator.Unescape(@"\\"));
            Assert.Equal(@"\\", AssemblyGenerator.Unescape(@"\\\"));
            Assert.Equal(@"\\", AssemblyGenerator.Unescape(@"\\\\"));

            Assert.Equal(@"\x", AssemblyGenerator.Unescape(@"\x"));
            Assert.Equal(@"x\", AssemblyGenerator.Unescape(@"x\"));
            Assert.Equal(@"A.B*C.D&E+F", AssemblyGenerator.Unescape(@"A.B\*C.D\&E+F"));
        }
    }
}
