using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace Lokad.ILPack.Metadata
{
    internal partial class AssemblyMetadata
    {
        private const string SystemRuntimeAssemblyName = "System.Runtime";
        private const string MscorlibAssemblyName = "mscorlib";

        private byte[] _coreLibToken;

        private AssemblyReferenceHandle GetCoreLibAssembly()
        {
            return _assemblyRefHandles.First().Value;
        }

        private AssemblyReferenceHandle GetReferencedAssemblyForType(Type type)
        {
            if (type.Name == "System.RuntimeType")
            {
                return GetCoreLibAssembly();
            }

            var asm = type.Assembly.GetName();

            var token = asm.GetPublicKeyToken();
            if (token.SequenceEqual(_coreLibToken))
            {
                return GetCoreLibAssembly();
            }

            var uniqueName = asm.ToString();
            if (_assemblyRefHandles.ContainsKey(uniqueName))
            {
                return _assemblyRefHandles[uniqueName];
            }

            throw new Exception($"Referenced Assembly not found! ({asm.FullName})");
        }

        private void AddReferencedAssembly(string referenceName, AssemblyName assemblyName)
        {
            var uniqueName = assemblyName.ToString();
            if (_assemblyRefHandles.ContainsKey(uniqueName))
            {
                return;
            }

            var token = assemblyName.GetPublicKeyToken();
            if (token != null && _coreLibToken != null && token.SequenceEqual(_coreLibToken))
            {
                return;
            }

            var key = assemblyName.GetPublicKey();
            var hashOrToken = token ?? key;
            var handle = Builder.AddAssemblyReference(
                GetOrAddString(referenceName),
                assemblyName.Version,
                GetOrAddString(assemblyName.CultureName),
                Builder.GetOrAddBlob(hashOrToken),
                MetadataHelper.ConvertReferencedAssemblyNameFlags(assemblyName.Flags),
                default); // Null is allowed

            _assemblyRefHandles.Add(uniqueName, handle);
        }

        private static bool IsDotNetCore()
        {
            var desc = RuntimeInformation.FrameworkDescription;
            if (string.IsNullOrEmpty(desc))
            {
                return false;
            }

            return desc.StartsWith(".NET Core ");
        }

        private void CreateReferencedAssemblies(AssemblyName[] assemblies)
        {
            // We reference different core library assembly for .NET Core and .NET Framework.
            // We reference "System.Runtime" for .NET Core and "mscorlib" for .NET Framework.
            // We also define a mapping, so that "System.Private.CoreLib" which is added as first
            // assembly for dynamically generated assemblies in .NET Core maps to "System.Runtime".

            if (IsDotNetCore())
            {
                var systemRuntime = Assembly.Load(SystemRuntimeAssemblyName).GetName();
                AddReferencedAssembly(SystemRuntimeAssemblyName, systemRuntime);
            }
            else
            {
                var mscorlib = Assembly.Load("mscorlib").GetName();
                AddReferencedAssembly(MscorlibAssemblyName, mscorlib);
            }

            // Since AddReferencedAssembly checks for mapping,
            // we set _corLibToken after "mscorlib"/"System.Runtime"
            var coreLib = typeof(object).GetTypeInfo().Assembly.GetName();
            _coreLibToken = coreLib.GetPublicKeyToken();

            foreach (var asm in assemblies)
            {
                AddReferencedAssembly(asm.ToString(), asm);
            }
        }
    }
}