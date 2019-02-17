using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Lokad.ILPack.Tests
{
    public class SampleFactorialFromEmission
    {
        // emit the assembly using op codes
        public static Assembly EmitAssembly(int theValue)
        {
            // create assembly name
            var assemblyName = new AssemblyName {Name = "FactorialAssembly"};

            // create assembly with one module
            var newAssembly =
                AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var newModule = newAssembly.DefineDynamicModule("MFactorial");

            // define a public class named "CFactorial" in the assembly
            var myType = newModule.DefineType("Namespace.CFactorial", TypeAttributes.Public);

            // Mark the class as implementing IFactorial.
            // myType.AddInterfaceImplementation(typeof(IFactorial));

            // define myfactorial method by passing an array that defines
            // the types of the parameters, the type of the return type,
            // the name of the method, and the method attributes.

            var paramTypes = new Type[0];
            var returnType = typeof(int);
            var simpleMethod = myType.DefineMethod("myfactorial",
                MethodAttributes.Public | MethodAttributes.Virtual,
                returnType,
                paramTypes);

            // obtain an ILGenerator to emit the IL
            var generator = simpleMethod.GetILGenerator();

            // Ldc_I4 pushes a supplied value of type int32
            // onto the evaluation stack as an int32.
            // push 1 onto the evaluation stack.
            // foreach i less than theValue,
            // push i onto the stack as a constant
            // multiply the two values at the top of the stack.
            // The result multiplication is pushed onto the evaluation
            // stack.
            generator.Emit(OpCodes.Ldc_I4, 1);

            for (var i = 1; i <= theValue; ++i)
            {
                generator.Emit(OpCodes.Ldc_I4, i);
                generator.Emit(OpCodes.Mul);
            }

            // emit the return value on the top of the evaluation stack.
            // Ret returns from method, possibly returning a value.

            generator.Emit(OpCodes.Ret);

            // encapsulate information about the method and
            // provide access to the method metadata
            // MethodInfo factorialInfo = typeof(IFactorial).GetMethod("myfactorial");

            // specify the method implementation.
            // pass in the MethodBuilder that was returned
            // by calling DefineMethod and the methodInfo just created
            // myType.DefineMethodOverride(simpleMethod, factorialInfo);

            // create the type and return new on-the-fly assembly
            myType.CreateType();

            return newAssembly;
        }
    }
}