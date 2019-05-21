﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Xunit;


namespace Lokad.ILPack.Tests
{
    public partial class RewriteTest
    {
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
        public async void AttributePlacedArrayValues()
        {
            Assert.Equal(new int[] { 10, 20, 30 } , await Invoke(
                $"var attr = typeof({_namespaceName}.MyClass).GetMethod(\"AttributeArrayTest\").GetCustomAttribute<MyAttribute>();",
                "attr.Values"
                ));
        }

        [Fact]
        public async void AttributeNamedValue()
        {
            Assert.Equal("ILPack", await Invoke(
                $"var attr = typeof({_namespaceName}.MyClass).GetMethod(\"AttributeArrayTest\").GetCustomAttribute<MyAttribute>();",
                "attr.Named"
                ));
        }

        [Fact]
        public async void AttributeNamedArrayValues()
        {
            Assert.Equal(new int[] { 40, 50, 60 }, await Invoke(
                $"var attr = typeof({_namespaceName}.MyClass).GetMethod(\"AttributeArrayTest\").GetCustomAttribute<MyAttribute>();",
                "attr.NamedArray"
                ));
        }
    }
}
