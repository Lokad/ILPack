using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

namespace Lokad.ILPack
{
    public partial class AssemblyGenerator
    {
        private readonly byte[] _coreLibToken = {0x7c, 0xec, 0x85, 0xd7, 0xbe, 0xa7, 0x79, 0x8e};

        // Saved assembly references handles
        private readonly Dictionary<string, AssemblyReferenceHandle> _assemblyReferenceHandles =
            new Dictionary<string, AssemblyReferenceHandle>();

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

            if (token.SequenceEqual(_coreLibToken))
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

        private void CreateReferencedAssemblies(AssemblyName[] assemblies)
        {
            foreach (var asm in assemblies)
            {
                var uniqueName = asm.ToString();
                if (_assemblyReferenceHandles.ContainsKey(uniqueName))
                {
                    continue;
                }

                var token = asm.GetPublicKeyToken();
                var key = asm.GetPublicKey();

                var hashOrToken = token == null ? GetBlob(key) : GetBlob(token);


                var handle = _metadataBuilder.AddAssemblyReference(
                    GetString(asm.Name),
                    asm.Version,
                    GetString(asm.CultureName),
                    hashOrToken,
                    _assemblyNameFlagsConvert(asm.Flags),
                    default(BlobHandle)); // Null is allowed

                _assemblyReferenceHandles.Add(uniqueName, handle);
            }
        }
    }
}