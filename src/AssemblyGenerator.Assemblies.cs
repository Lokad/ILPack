using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

namespace Lokad.ILPack
{
    public partial class AssemblyGenerator
    {
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
            var key = asm.GetPublicKey();
            var fullName = asm.FullName;

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
            // Add "mscorlib" first by its explicit name.
            // Otherwise, it will be referenced as "System.Private.CoreLib"
            // Also, derive mscorlib public key token at runtime to support future changes.
            var mscorlib = typeof(object).GetTypeInfo().Assembly.GetName();
            _mscorlibToken = mscorlib.GetPublicKeyToken();
            AddReferencedAssembly("mscorlib", mscorlib);

            foreach (var asm in assemblies)
            {
                AddReferencedAssembly(asm.ToString(), asm);
            }
        }
    }
}