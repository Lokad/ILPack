// #define USE_ORIGINAL_SANDBOXSUBJECT

using Lokad.ILPack;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

/*
 * Sandbox
 *
 * This is intended as a place to work on problem areas that currently aren't even
 * stable enough to write unit tests for yet.
 * 
 * In particular if something in the TestSubject project causes ILPack to throw an
 * exception, all the unit tests will fail since the re-written assembly isn't produced.
 * The idea is to work on these kinds of problems here and once working, move the test
 * code into the unit tests.
 * 
 * Other advantages to Sandbox over unit tests:
 * 
 *  - SandboxSubject can be kept very small and targeted to a particular problem allowing 
 *    for easier inspection of ildasm/mddumper analysis
 *    
 *  - Quick F5 debug/fix/re-run cycle (instead of selecting a specific unit test to run)
 * 
 * Sandbox works almost identically to the RewriteTest cases - it rewrites a test assembly
 * (in this case SandboxSubject) and then invokes it with CSharpScript.
 * 
 * Use the `dump.bat` script to produce ildasm and mddumper listings for original and
 * cloned assemblies. (requires those tools on your path)
 * 
 * Uncomment the #define at the top of this file this to test against the original 
 * SandboxSubject instead of the cloned one. (to test your test case)
 * 
 */
 
namespace Sandbox
{
    class Program
    {
        string _namespaceName;
        string _assembly;
        Assembly _asmOriginal;
        Assembly _asmCloned;


        async Task<object> Invoke(string setup, string resultExpression)
        {
            var script = CSharpScript
                .Create($"var x = new MyClass();",
                        ScriptOptions.Default
                            .WithReferences(_assembly)
                            .WithImports(_namespaceName)
                            )
                .ContinueWith(setup)
                .ContinueWith(resultExpression);

            return (await script.RunAsync()).ReturnValue;
        }


        static async Task Main(string[] args)
        {
            try
            {
                await new Program().Run();
            }
            catch (Exception x)
            {
                Console.WriteLine(x.Message); //-V5621
                Console.WriteLine(x.StackTrace); //-V5621
                throw;
            }
        }

        async Task Run()
        {
            // Get the original assembly
            _asmOriginal = typeof(SandboxSubject.MyClass).Assembly;
            _asmCloned = typeof(SandboxSubject.MyClass).Assembly;
            var originalAssembly = _asmOriginal.Location;


#if USE_ORIGINAL_SANDBOXSUBJECT

            _namespaceName = "SandboxSubject";
            _assembly = originalAssembly;

#else

            // Generate the cloned assembly
            // NB: putting it in the "cloned" sub directory prevents an
            //     issue with someone (VisStudio perhaps) having the file open
            //     and preventing rewrite on subsequent run. 
            var outDir = Path.Join(Path.GetDirectoryName(originalAssembly), "cloned");
            Directory.CreateDirectory(outDir);
            var clonedAssembly = Path.Join(outDir, "ClonedSandboxSubject.dll");

            // Rewrite it (renaming the assembly and namespaces in the process)
            var generator = new AssemblyGenerator();
            generator.RenameForTesting("SandboxSubject", "ClonedSandboxSubject");
            generator.GenerateAssembly(_asmOriginal, clonedAssembly);

            _namespaceName = "ClonedSandboxSubject";
            _assembly = clonedAssembly;

            _asmCloned = Assembly.LoadFrom(_assembly);

#endif


            // Invoke the cloned assembly...

            var result = await Invoke(
                $"var r = x.Test();",
                "r"
                );

            Console.WriteLine(result);
        }
    }
}
