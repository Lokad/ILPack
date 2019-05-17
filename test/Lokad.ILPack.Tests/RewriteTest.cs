using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Xunit;

namespace Lokad.ILPack.Tests
{
    public class RewriteTest
    {
        static RewriteTest()
        {
            // Get the original assembly
            _originalAssembly = typeof(RewriteOriginal.MyClass).Assembly;

            // Put clone in a subdirectory
            var outDir = System.IO.Path.GetFullPath(".\\cloned\\");
            System.IO.Directory.CreateDirectory(outDir);

            // Rewrite it
            var generator = new AssemblyGenerator();
            var targetPath = System.IO.Path.GetFullPath(".\\cloned\\RewriteOriginal.dll");
            generator.GenerateAssembly(_originalAssembly, targetPath);

            // Load it
            _clonedAssembly = Assembly.LoadFrom(targetPath);
        }

        static Assembly _originalAssembly;
        static Assembly _clonedAssembly;

        [Fact]
        void RewrittenDidLoad()
        {
            Assert.NotNull(_clonedAssembly);
        }

        [Fact]
        void AssemblyNamesMatch()
        {
            Assert.Equal(_originalAssembly.FullName, _clonedAssembly.FullName);
        }

        public static IEnumerable<object[]> EnumModules
        {
            get
            {
                var original = _originalAssembly.GetModules().OrderBy(x=>x.FullyQualifiedName).ToArray();
                var cloned = _clonedAssembly.GetModules().OrderBy(x=>x.FullyQualifiedName).ToArray();
                Assert.Equal(original.Length, cloned.Length);
                for (int i = 0; i < original.Length; i++)
                {
                    yield return new object[] { original[i], cloned[i] };
                }
            }
        }

        [Theory]
        [MemberData(nameof(EnumModules))]
        void ModuleNamesMatch(Module original, Module clone)
        {
            Assert.Equal(original.Name, clone.Name);
        }

        public static IEnumerable<object[]> EnumTypes
        {
            get
            {
                var original = _originalAssembly.GetTypes().OrderBy(x => x.FullName).ToArray();
                var cloned = _clonedAssembly.GetTypes().OrderBy(x => x.FullName).ToArray();
                Assert.Equal(original.Length, cloned.Length);
                for (int i = 0; i < original.Length; i++)
                {
                    yield return new object[] { original[i], cloned[i] };
                }
            }
        }

        [Theory]
        [MemberData(nameof(EnumTypes))]
        void TypesMatch(Type original, Type clone)
        {
            Assert.Equal(original.Name, clone.Name);
        }

        // Get all methods in an assembly
        static IEnumerable<MethodInfo> AllMethodsInAssembly(Assembly a)
        {
            foreach (var t in _originalAssembly.GetTypes().OrderBy(x => x.FullName).ToArray())
            {
                foreach (var m in t.GetMethods())
                {
                    yield return m;
                }
            }
        }
        public static IEnumerable<object[]> EnumMethods
        {
            get
            {
                var original = AllMethodsInAssembly(_originalAssembly).ToArray();
                var cloned = AllMethodsInAssembly(_clonedAssembly).ToArray();
                Assert.Equal(original.Length, cloned.Length);
                for (int i = 0; i < original.Length; i++)
                {
                    yield return new object[] { original[i], cloned[i] };
                }
            }
        }

        [Theory]
        [MemberData(nameof(EnumMethods))]
        public void MethodsMatch(MethodInfo original, MethodInfo clone)
        {
            Assert.Equal(original.Name, clone.Name);
            Assert.Equal(original.GetParameters().Length, clone.GetParameters().Length);
        }


        // Get all properties in an assembly
        static IEnumerable<PropertyInfo> AllPropertiesInAssembly(Assembly a)
        {
            foreach (var t in _originalAssembly.GetTypes().OrderBy(x => x.FullName).ToArray())
            {
                foreach (var p in t.GetProperties())
                {
                    yield return p;
                }
            }
        }
        public static IEnumerable<object[]> EnumProperties
        {
            get
            {
                var original = AllPropertiesInAssembly(_originalAssembly).ToArray();
                var cloned = AllPropertiesInAssembly(_clonedAssembly).ToArray();
                Assert.Equal(original.Length, cloned.Length);
                for (int i = 0; i < original.Length; i++)
                {
                    yield return new object[] { original[i], cloned[i] };
                }
            }
        }

        [Theory]
        [MemberData(nameof(EnumProperties))]
        public void PropertiesMatch(PropertyInfo original, PropertyInfo clone)
        {
            Assert.Equal(original.Name, clone.Name);
            Assert.Equal(original.PropertyType.Name, clone.PropertyType.Name);
        }


        // Get all events in an assembly
        static IEnumerable<EventInfo> AllEventsInAssembly(Assembly a)
        {
            foreach (var t in _originalAssembly.GetTypes().OrderBy(x => x.FullName).ToArray())
            {
                foreach (var e in t.GetEvents())
                {
                    yield return e;
                }
            }
        }
        public static IEnumerable<object[]> EnumEvents
        {
            get
            {
                var original = AllEventsInAssembly(_originalAssembly).ToArray();
                var cloned = AllEventsInAssembly(_clonedAssembly).ToArray();
                Assert.Equal(original.Length, cloned.Length);
                for (int i = 0; i < original.Length; i++)
                {
                    yield return new object[] { original[i], cloned[i] };
                }
            }
        }

        [Theory]
        [MemberData(nameof(EnumEvents))]
        public void EventsMatch(EventInfo original, EventInfo clone)
        {
            Assert.Equal(original.Name, clone.Name);
        }


    }
}
