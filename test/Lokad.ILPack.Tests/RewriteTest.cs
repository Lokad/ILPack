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
            _asmOriginal = typeof(RewriteOriginal.MyClass).Assembly;
            var originalAssembly = _asmOriginal.Location;

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
            generator.GenerateAssembly(_asmOriginal, clonedAssembly);

            _namespaceName = "RewriteClone";
            _assembly = clonedAssembly;

            // Uncomment these two lines to run with the original uncloned assembly
            // (handy to check if test case is wrong)
            //_namespaceName = "RewriteOriginal";
            //_assembly = originalAssembly;

            _asmCloned = Assembly.LoadFrom(_assembly);
        }

        static string _namespaceName;
        static string _assembly;
        static Assembly _asmOriginal;
        static Assembly _asmCloned;

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
            // Currently failing with:
            // 
            // Message: System.TypeLoadException : Could not load type 'System.Threading.Interlocked' from assembly 'System.Runtime, Version=4.2.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'.
            //
            // MSIL from original is:
            //
            //    IL_001e:  call       !!0 [System.Threading]System.Threading.Interlocked::CompareExchange<class [System.Runtime]System.Action>(!!0&,
            // 
            // MSIL from clone is:
            //
            //    IL_001e:  call       class [System.Runtime]System.Action [System.Runtime]System.Threading.Interlocked::CompareExchange(class [System.Runtime]System.'Action&',

            Assert.Equal(99, await Invoke(
                @"  int cbVal = 0; 
                    x.NoParamEvent += () => cbVal = 99;
                    x.InvokeNoParamEvent()",
                   
                "cbVal"));
        }

        [Fact]
        public async void InvokeNoParamEventWithNoListeners()
        {
            // This test highlights an issue with incorrect MSIL generation

            // CLONED:
            /* 
              .method public hidebysig instance void 
                      InvokeNoParamEvent() cil managed
              {
                // Code size       20 (0x14)
                .maxstack  8
                IL_0000:  nop
                IL_0001:  ldarg.0
                IL_0002:  ldfld      class [System.Runtime]System.Action RewriteClone.MyClass::NoParamEvent
                IL_0007:  dup
                IL_0008:  brtrue.s   IL_000a            <<<<<<<<<<<<< WRONG (SEE BELOW)

                IL_000a:  pop
                IL_000b:  br.s       IL_000e            <<<<<<<<<<<<< WRONG (SEE BELOW)

                IL_000d:  callvirt   instance void [System.Runtime]System.Action::Invoke()
                IL_0012:  nop
                IL_0013:  ret
              } // end of method MyClass::InvokeNoParamEvent
            */

            // ORIGINAL:
            /*
              .method public hidebysig instance void 
                      InvokeNoParamEvent() cil managed
              {
                // Code size       20 (0x14)
                .maxstack  8
                IL_0000:  nop
                IL_0001:  ldarg.0
                IL_0002:  ldfld      class [System.Runtime]System.Action RewriteOriginal.MyClass::NoParamEvent
                IL_0007:  dup
                IL_0008:  brtrue.s   IL_000d

                IL_000a:  pop
                IL_000b:  br.s       IL_0013

                IL_000d:  callvirt   instance void [System.Runtime]System.Action::Invoke()
                IL_0012:  nop
                IL_0013:  ret
              } // end of method MyClass::InvokeNoParamEvent
             */


            Assert.Equal(true, await Invoke(
                @"x.InvokeNoParamEvent()",
                "true"));
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

        [Fact]
        public async void InvokeIntParamEventWithNoListeners()
        {
            // This test highlights an issue trying to load Action<int> from wrong assembly
            //
            // Message: System.TypeLoadException : Could not load type 'System.Action`1' from assembly 'RewriteClone, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'.
            //                                                         ^^^^^^^^^^^^^^^^^                ^^^^^^^^^^^^ huh?

            Assert.Equal(true, await Invoke(
                @"x.InvokeIntParamEvent(77)",
                "true"));
        }

        [Fact]
        public void AssemblyTargetFramework()
        {
            var targetFrameworkOriginal = _asmOriginal.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>();
            var targetFrameworkClone = _asmCloned.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>();

            Assert.Equal(targetFrameworkOriginal.FrameworkDisplayName, targetFrameworkClone.FrameworkDisplayName);
            Assert.Equal(targetFrameworkOriginal.FrameworkName, targetFrameworkClone.FrameworkName);
        }

        [Fact]
        public void AssemblyName()
        {
            var titleOriginal = _asmOriginal.GetCustomAttribute<System.Reflection.AssemblyTitleAttribute>();
            var titleClone = _asmCloned.GetCustomAttribute<System.Reflection.AssemblyTitleAttribute>();

            Assert.Equal(titleOriginal.Title, titleClone.Title);
        }

    }
}
