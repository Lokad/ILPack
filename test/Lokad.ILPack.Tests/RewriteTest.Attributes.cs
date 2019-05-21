using System;
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

    }
}
