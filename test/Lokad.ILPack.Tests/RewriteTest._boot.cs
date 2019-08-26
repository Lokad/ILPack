// Uncomment this to test against the original TestSubject instead of the cloned one
//#define USE_ORIGINAL_TESTSUBJECT

using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Lokad.ILPack.Tests
{
    /*
     * These test cases work by taking the TestSubject project, passing it
     * through ILPack to generate a new assembly and then checking that the
     * newly generated assembly is correct by actually loading it and invoking
     * methods and properties on it.
     * 
     * It works as follows:
     * 
     * 1. `TestSubject.dll` is loaded through a project reference and found
     *    with a simple typeof(MyClass)
     *        
     * 2. ILPack is used to to rewrite a new assembly `ClonedTestSubject.dll`
     * 
     * 3. To allow the second DLL to be loaded into the same process (we don't
     *    have AppDomains under net core), we use ILPack's RenameForTesting method
     *    to change the names of the assembly and the contained namespace(s)
     *    
     * 4. Use CSharpScript to load the newly cloned assembly and poke it in 
     *    various ways to make sure it still works.
     *    
     * Also, in the Lokad.ILPack.Tests project folder there's a dump.bat script
     * which on Windows will run ildasm and mddumper on both the original and the
     * cloned assemblies.  Handy for comparison when diagnosing issues. (ildasm
     * and mddumper both need to be on your path)
     * 
     */

    public partial class RewriteTest
    {
        static RewriteTest()
        {
            // Get the original assembly
            _asmOriginal = typeof(TestSubject.MyClass).Assembly;
            _asmCloned = typeof(TestSubject.MyClass).Assembly;
            var originalAssembly = _asmOriginal.Location;


#if USE_ORIGINAL_TESTSUBJECT

            _namespaceName = "TestSubject";
            _assembly = originalAssembly;

#else

            // Generate the cloned assembly
            // NB: putting it in the "cloned" sub directory prevents an
            //     issue with someone (VisStudio perhaps) having the file open
            //     and preventing rewrite on subsequent run. 
            var outDir = Path.Combine(Path.GetDirectoryName(originalAssembly), "cloned");
            Directory.CreateDirectory(outDir);
            var clonedAssembly = Path.Combine(outDir, "ClonedTestSubject.dll");

            // Rewrite it (renaming the assembly and namespaces in the process)
            var generator = new AssemblyGenerator();
            generator.RenameForTesting("TestSubject", "ClonedTestSubject");
            generator.GenerateAssembly(_asmOriginal, clonedAssembly);

            _namespaceName = "ClonedTestSubject";
            _assembly = clonedAssembly;

            _asmCloned = Assembly.LoadFrom(_assembly);

#endif
        }

        static string _namespaceName;
        static string _assembly;
        static Assembly _asmOriginal;
        static Assembly _asmCloned;

        async Task<object> Invoke(string setup, string resultExpression)
        {
            var script = CSharpScript
                .Create($"var x = new MyClass();", 
                        ScriptOptions.Default
                            .WithReferences(_assembly)
                            .WithImports(_namespaceName, "System.Reflection")
                        )
                .ContinueWith(setup)
                .ContinueWith(resultExpression);

            return (await script.RunAsync()).ReturnValue;
        }
    }
}
