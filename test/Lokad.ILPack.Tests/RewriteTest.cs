// Uncomment this to test against the original TestSubject instead of the cloned one
//#define USE_ORIGINAL_TESTSUBJECT

using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

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

    public class RewriteTest
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
            var outDir = Path.Join(Path.GetDirectoryName(originalAssembly), "cloned");
            Directory.CreateDirectory(outDir);
            var clonedAssembly = Path.Join(outDir, "ClonedTestSubject.dll");

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
                            .WithImports(_namespaceName)
                        )
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
            // Message: System.TypeLoadException : Could not load type 'System.Action`1' from assembly 'ClonedTestSubject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'.
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

        [Fact]
        public async void ByRefParam()
        {
            Assert.Equal(34, await Invoke(
                $"int r=0; x.ByRefParam(ref r);",
                "r"));
        }

        [Fact]
        public async void OutParam()
        {
            Assert.Equal(35, await Invoke(
                $"int r; x.OutParam(out r);",
                "r"));
        }

        [Fact]
        public async void StaticGenericMethod()
        {
            Assert.Equal(36, await Invoke(
                $"int r = {_namespaceName}.MyClass.StaticGenericMethod<int>(36);",
                "r"));
        }

        [Fact]
        public async void StaticGenericMethodWithByRef()
        {
            Assert.Equal((38,37), await Invoke(
                $"int a = 37; int b = 38; MyClass.StaticGenericMethodWithByRef<int>(ref a, ref b);",
                "(a,b)"));
        }

        [Fact]
        public async void GenericMethod()
        {
            Assert.Equal(36, await Invoke(
                $"int r = x.GenericMethod<int>(36);",
                "r"));
        }

        [Fact]
        public async void GenericMethodWithByRef()
        {
            Assert.Equal((38,37), await Invoke(
                $"int a = 37; int b = 38; x.GenericMethodWithByRef<int>(ref a, ref b);",
                "(a,b)"));
        }

        [Fact]
        public async void CallExplicitlyImplementedInterfaceMethod()
        {
            Assert.Equal(1001, await Invoke(
                $"int r = (x as IMyItf).InterfaceMethod1();",
                "r"));
        }

        [Fact]
        public async void CallImplicitlyImplementedInterfaceMethodThroughInterface()
        {
            Assert.Equal(1002, await Invoke(
                $"int r = (x as IMyItf).InterfaceMethod2();",
                "r"));
        }

        [Fact]
        public async void CallImplicitlyImplementedInterfaceMethodThroughClass()
        {
            Assert.Equal(1002, await Invoke(
                $"int r = x.InterfaceMethod2();",
                "r"));
        }

        [Fact]
        public async void BasicEnumTest()
        {
            Assert.Equal(150, await Invoke(
                $"int r = (int)MyEnum.Pears;",
                "r"));
        }

        [Fact]
        public async void BasicStructTest()
        {
            Assert.Equal((20, 30), await Invoke(
                $"var s = x.GetMyStruct();",
                "(s.x,s.y)"));
        }

        [Fact]
        public async void NestedClass()
        {
            Assert.Equal(9, await Invoke(
                $"var r = new {_namespaceName}.MyClass.NestedClass().Method();",
                "r"
            ));
        }

        [Fact]
        public async void Indexer()
        {
            Assert.Equal(10, await Invoke(
                $"x[1] = 10; var r = x[1];",
            "r"
            ));
        }

        /*
        [Fact]
        public async void AsyncMethod()
        {
            Assert.Equal(60, await Invoke(
                $"int r = await x.AsyncMethod(30, 30);",
                "r"));
        }
        */
    }
}
