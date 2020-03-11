using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;

namespace Lokad.ILPack.Metadata
{
    internal partial class AssemblyMetadata
    {
        private AssemblyReferenceHandle GetReferencedAssemblyForType(Type type)
        {
            // To look up nested types in the forwarding map, we need the outermost type
            while (type.DeclaringType != null)
                type = type.DeclaringType;

            // Look up the reverse forwarding map
            if (!_reverseForwardingMap.TryGetValue($"{type.Namespace}.{type.Name}", out var asm))
            {
                asm = type.Assembly.GetName();
            }

            // Get the assembly reference
            var uniqueName = asm.ToString();
            if (_assemblyRefHandles.ContainsKey(uniqueName))
            {
                return _assemblyRefHandles[uniqueName];
            }

            throw new InvalidOperationException($"Referenced assembly cannot be found: {asm.FullName}");

        }

        private void AddReferencedAssembly(string referenceName, AssemblyName assemblyName)
        {
            var uniqueName = assemblyName.ToString();
            if (_assemblyRefHandles.ContainsKey(uniqueName))
            {
                return;
            }

            var token = assemblyName.GetPublicKeyToken();
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

        // A map of type name to the original assembly reference that forwarded it
        Dictionary<string, AssemblyName> _reverseForwardingMap = new Dictionary<string, AssemblyName>();

        // Build a map of forwarded types to the original assembly reference that forwarded it.
        void AddAssemblyToReverseForwardingMap(AssemblyName asmName)
        {
            // Load the assembly
            var asm = 
                _referencedDynamics.TryGetValue(asmName.FullName, out var dynamic)
                    ? dynamic
                    : Assembly.Load(asmName);

            // Get it's metadata
            MetadataReader mdr = null;
            try
            {
                unsafe
                {
                    if (asm.TryGetRawMetadata(out var blob, out var length))
                    {
                        mdr = new MetadataReader(blob, length);
                    }
                }
            }
            catch { /* Don't care */ }

            // Look up all its exported types and add them to the map
            if (mdr != null)
            {
                foreach (var eth in mdr.ExportedTypes)
                {
                    var et = mdr.GetExportedType(eth);
                    if (et.IsForwarder)
                    {
                        var name = mdr.GetString(et.Name);
                        var ns = mdr.GetString(et.Namespace);
                        var key = $"{ns}.{name}";
                        if (!_reverseForwardingMap.ContainsKey(key))
                            _reverseForwardingMap.Add(key, asmName);
                    }
                }
            }
        }

        private void CreateReferencedAssemblies(IEnumerable<AssemblyName> assemblies)
        {
            foreach (var asm in assemblies)
            {
                AddAssemblyToReverseForwardingMap(asm);
                AddReferencedAssembly(asm.Name, asm);
            }
        }
    }
}