using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Lokad.ILPack.Tests
{
    /*
     * These test cases work by taking the RewriteOriginal project, passing it
     * through ILPack to generate a new assembly and then checking that the
     * newly generated assembly is correct by actually loading it and invoking
     * methods and properties on it.
     * 
     * It works as follows:
     * 
     * 1. `RewriteOriginal.dll` is loaded through a project reference and found
     *    with a simple typeof(MyClass)
     *    
     * 2. ILPack is used to to rewrite a new assembly `RewriteClone.dll`
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

    public class RewriteTest
    {
        static RewriteTest()
        {
            // Get the original assembly
            var original = typeof(RewriteOriginal.MyClass).Assembly;
            var originalAssembly = original.Location;

            // Generate the cloned assembly
            // NB: putting it in the "cloned" sub directory prevents an
            //     issue with someone (VisStudio perhaps) having the file open
            //     and preventing rewrite on subsequent run. 
            var outDir = Path.Join(Path.GetDirectoryName(originalAssembly), "cloned");
            Directory.CreateDirectory(outDir);
            var clonedAssembly = Path.Join(outDir, "RewriteClone.dll");

            // Rewrite it (renaming the assembly and namespaces in the process)
            var generator = new AssemblyGenerator();
            generator.RenameForTesting("RewriteOriginal", "RewriteClone");
            generator.GenerateAssembly(original, clonedAssembly);

            _namespaceName = "RewriteClone";
            _assembly = clonedAssembly;

            // Uncomment these two lines to run with the original uncloned assembly
            // (handy to check if test case is wrong)
            //_namespaceName = "RewriteOriginal";
            //_assembly = originalAssembly;
        }

        static string _namespaceName;
        static string _assembly;

        async Task<object> Invoke(string setup, string resultExpression)
        {
            var script = CSharpScript
                .Create($"var x = new {_namespaceName}.MyClass();", 
                        ScriptOptions.Default.WithReferences(_assembly))
                .ContinueWith(setup)
                .ContinueWith(resultExpression);

            return (await script.RunAsync()).ReturnValue;
        }

        [Fact]
        public async void ReadOnlyProperty()
        {
            Assert.Equal(23, await Invoke(
                "",
                "x.ReadOnlyProperty"));
        }

        [Fact]
        public async void WriteOnlyProperty()
        {
            Assert.Equal(true, await Invoke(
                "x.WriteOnlyProperty = 99;",
                "true"));
        }

        [Fact]
        public async void ReadWriteOnlyProperty()
        {
            Assert.Equal(101, await Invoke(
                "x.ReadWriteProperty = 101;",
                "x.ReadWriteProperty"));
        }

        [Fact]
        public async void VoidMethod()
        {
            Assert.Equal(true, await Invoke(
                "x.VoidMethod();",
                "true"));
        }

        [Fact]
        public async void IntMethod()
        {
            Assert.Equal(33, await Invoke(
                "var r = x.IntMethod();",
                "r"));
        }

        [Fact]
        public async void IntMethodWithParameters()
        {
            Assert.Equal(30, await Invoke(
                "var r = x.IntMethodWithParameters(10,20);",
                "r"));
        }

        [Fact]
        public async void NoParamEvent()
        {
            Assert.Equal(99, await Invoke(
                @"  int cbVal = 0; 
                    x.NoParamEvent += () => cbVal = 99;
                    x.InvokeNoParamEvent()",
                   
                "cbVal"));
        }

        [Fact]
        public async void IntParamEvent()
        {
            Assert.Equal(77, await Invoke(
                @"  int cbVal = 0; 
                    x.IntParamEvent += (val) => cbVal = val;
                    x.InvokeIntParamEvent(77)",
                   
                "cbVal"));
        }

    }
}
