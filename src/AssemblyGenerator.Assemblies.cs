using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

namespace Lokad.ILPack
{
    public partial class AssemblyGenerator
    {
        private const string SystemRuntimeAssemblyName = "System.Runtime";

        // Saved assembly references handles
        private readonly Dictionary<string, AssemblyReferenceHandle> _assemblyReferenceHandles =
            new Dictionary<string, AssemblyReferenceHandle>();

        private byte[] _mscorlibToken;

        private AssemblyReferenceHandle GetCoreLibAssembly()
        {
            return _assemblyReferenceHandles.First().Value;
        }

        private AssemblyReferenceHandle GetReferencedAssemblyForType(Type type)
        {
            if (type.Name == "System.RuntimeType")
            {
                return GetCoreLibAssembly();
            }

            var asm = type.Assembly.GetName();

            var token = asm.GetPublicKeyToken();
            if (token.SequenceEqual(_mscorlibToken))
            {
                return GetCoreLibAssembly();
            }

            var uniqueName = asm.ToString();
            if (_assemblyReferenceHandles.ContainsKey(uniqueName))
            {
                return _assemblyReferenceHandles[uniqueName];
            }

            throw new Exception($"Referenced Assembly not found! ({asm.FullName})");
        }

        private void AddReferencedAssembly(string referenceName, AssemblyName assemblyName)
        {
            var uniqueName = assemblyName.ToString();
            if (_assemblyReferenceHandles.ContainsKey(uniqueName))
            {
                return;
            }

            var token = assemblyName.GetPublicKeyToken();
            if (token != null && _mscorlibToken != null && token.SequenceEqual(_mscorlibToken))
            {
                return;
            }

            var key = assemblyName.GetPublicKey();
            var hashOrToken = token ?? key;
            var handle = _metadataBuilder.AddAssemblyReference(
                GetString(referenceName),
                assemblyName.Version,
                GetString(assemblyName.CultureName),
                GetBlob(hashOrToken),
                ConvertReferencedAssemblyNameFlags(assemblyName.Flags),
                default(BlobHandle)); // Null is allowed

            _assemblyReferenceHandles.Add(uniqueName, handle);
        }

        private void CreateReferencedAssemblies(AssemblyName[] assemblies)
        {
            // Dynamically generated assemblies reference "System.Private.CoreLib" assembly first.
            // "System.Private.CoreLib" public key token is same as "mscorlib".
            // Since, .NET Core is fundamentally different from .NET Framework,
            // we don't reference "mscorlib" first. Instead, we'll reference "System.Runtime" assembly
            // which we extract it's full assembly name at runtime. Also, we'll provide a way to
            // map all "mscorlib" and "System.Private.CoreLib" references to "System.Runtime".
            var systemRuntime = Assembly.Load(SystemRuntimeAssemblyName).GetName();
            AddReferencedAssembly(SystemRuntimeAssemblyName, systemRuntime);

            // Since AddReferencedAssembly checks for mapping, we set _mscorlibToken after "System.Runtime"
            var mscorlib = typeof(object).GetTypeInfo().Assembly.GetName();
            _mscorlibToken = mscorlib.GetPublicKeyToken();

            foreach (var asm in assemblies)
            {
                AddReferencedAssembly(asm.ToString(), asm);
            }
        }
    }
}