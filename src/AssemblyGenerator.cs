using System;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Lokad.ILPack.Metadata;

namespace Lokad.ILPack
{
    public partial class AssemblyGenerator
    {
        private DebugDirectoryBuilder _debugDirectoryBuilder;
        private AssemblyMetadata _metadata;

        private void Initialize(Assembly assembly)
        {
            _metadata = new AssemblyMetadata(assembly);
            _debugDirectoryBuilder = new DebugDirectoryBuilder();
        }

        // Called by the unit tests to rename the assembly and the namespaces
        // in the rewritten assembly.  This allows the unit tests to load
        // both RewrittenOriginal.dll and RewrittenClone.dll.
        internal void RenameForTesting(string oldName, string newName)
        {
            _oldName = oldName;
            _newName = newName;
        }

        string _oldName;
        string _newName;

        // Apply name changes to assembly and namespace names
        internal string ApplyNameChange(string str)
        {
            if (_oldName == null)
                return str;
            else
                return str.Replace(_oldName, _newName);
        }

        public byte[] GenerateAssemblyBytes(Assembly assembly)
        {
            Initialize(assembly);

            if (_metadata.SourceAssembly.EntryPoint != null)
            {
                // See "<Module>" type definition below.
                throw new NotSupportedException("Entry point is not supported.");
            }

            var name = _metadata.SourceAssembly.GetName();

            var assemblyPublicKey = name.GetPublicKey();
            var assemblyHandle = _metadata.Builder.AddAssembly(
                _metadata.GetOrAddString(ApplyNameChange(name.Name)),
                name.Version,
                _metadata.GetOrAddString(name.CultureName),
                assemblyPublicKey.Length > 0 ? _metadata.GetOrAddBlob(name.GetPublicKey()) : default,
                ConvertGeneratedAssemblyNameFlags(name),
                ConvertAssemblyHashAlgorithm(name.HashAlgorithm));

            // Add "<Module>" type definition *before* any type definition.
            //
            // TODO: [osman] methodList argument should be as following:
            //
            //   methodList: entryPoint.IsNil ? MetadataTokens.MethodDefinitionHandle(1) : entryPoint
            //
            // But, in order to work above code, we need to serialize
            // entry point *without* serializing any type definition.
            // This is not needed for libraries since they don't have any entry point.
            _metadata.Builder.AddTypeDefinition(
                default,
                default,
                _metadata.GetOrAddString("<Module>"),
                default,
                MetadataTokens.FieldDefinitionHandle(1),
                MetadataTokens.MethodDefinitionHandle(1));

            CreateModules(_metadata.SourceAssembly.GetModules());

            MethodDefinitionHandle entryPoint = default;
            if (_metadata.SourceAssembly.EntryPoint != null &&
                _metadata.TryGetMethodDefinition(_metadata.SourceAssembly.EntryPoint, out var entryPointMetadata))
            {
                entryPoint = entryPointMetadata.Handle;
            }

            var metadataRootBuilder = new MetadataRootBuilder(_metadata.Builder);

            // Without Characteristics.ExecutableImage flag, .NET runtime refuses
            // to load an assembly even it's a DLL. PEHeaderBuilder.CreateLibraryHeader
            // does not set this flag. So, we set it explicitly.
            var header = new PEHeaderBuilder(imageCharacteristics: Characteristics.ExecutableImage |
                                                                   (entryPoint.IsNil ? Characteristics.Dll : 0));

            var peBuilder = new ManagedPEBuilder(
                header,
                metadataRootBuilder,
                _metadata.ILBuilder,
                debugDirectoryBuilder: _debugDirectoryBuilder,
                entryPoint: entryPoint);

            var peImageBuilder = new BlobBuilder();
            peBuilder.Serialize(peImageBuilder);

            return peImageBuilder.ToArray();
        }

        public void GenerateAssembly(Assembly assembly, string path)
        {
            var bytes = GenerateAssemblyBytes(assembly);
            File.WriteAllBytes(path, bytes);
        }
    }
}