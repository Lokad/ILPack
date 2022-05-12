using System;
using System.Collections.Generic;
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
        /// <summary>
        /// Metadata writes pushed to the builder needs to be ordered.
        /// This structure is used to accumulate all the write operations
        /// and execute them in the right order afterward.
        /// </summary>
        private struct DelayedWrite
        {
            public int Index;

            public Action Write;

            public DelayedWrite(int index, Action write)
            {
                Index = index;
                Write = write;
            }
        }

        private DebugDirectoryBuilder _debugDirectoryBuilder;
        private AssemblyMetadata _metadata;

        private void Initialize(Assembly assembly, IEnumerable<Assembly> referencedDynamicAssemblies)
        {
            _metadata = new AssemblyMetadata(assembly, referencedDynamicAssemblies);
            _debugDirectoryBuilder = new DebugDirectoryBuilder();
        }

        /// <summary>
        /// Called by the unit tests to rename the assembly and the namespaces 
        /// in the rewritten assembly.  This allows the unit tests to load
        /// both RewrittenOriginal.dll and RewrittenClone.dll.
        /// </summary>
        /// <param name="oldName">Name to replace</param>
        /// <param name="newName">What to replace it with</param>
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
            // Types can have a null namespace. eg: some compiler generated classes like C#'s  
            // "<PrivateImplementationDetails>" used for static array initializers
            if (str == null)
                return null;

            if (_oldName == null)
                return str;
            else
                return str.Replace(_oldName, _newName);
        }

        /// <summary> Serialize an assembly that don't depend on other dynamic assembly </summary>
        /// <returns> Serialized bytes </returns>
        public byte[] GenerateAssemblyBytes(Assembly assembly) =>
            GenerateAssemblyBytes(assembly, Array.Empty<Assembly>());

        /// <summary> Serialize an assembly to a byte array </summary>
        /// <param name="assembly"> Assembly to be serialized </param>
        /// <param name="referencedDynamicAssembly">
        /// List of other assembly that have types referenced by <see cref="assembly"/>
        /// and that are dynamic assembly. The .net assembly loader can't find those
        /// otherwize
        /// </param>
        /// <returns> The serialized assembly. </returns>
        public byte[] GenerateAssemblyBytes(Assembly assembly, IEnumerable<Assembly> referencedDynamicAssembly)
        {
            Initialize(assembly, referencedDynamicAssembly);

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
            _metadata.Builder.AddTypeDefinition(
                default,
                default,
                _metadata.GetOrAddString("<Module>"),
                default,
                MetadataTokens.FieldDefinitionHandle(1),
                MetadataTokens.MethodDefinitionHandle(1));

            CreateModules(_metadata.SourceAssembly.GetModules());

            CreateCustomAttributes(assemblyHandle, assembly.GetCustomAttributesData());

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
            var header = new PEHeaderBuilder(
                imageCharacteristics: Characteristics.ExecutableImage |
                                           (entryPoint.IsNil ? Characteristics.Dll : 0));

            var peBuilder = new ManagedPEBuilder(
                header: header,
                metadataRootBuilder: metadataRootBuilder,
                ilStream: _metadata.ILBuilder,
                mappedFieldData: _metadata.MappedFieldDataBuilder,
                debugDirectoryBuilder: _debugDirectoryBuilder,
                entryPoint: entryPoint);

            var peImageBuilder = new BlobBuilder();
            peBuilder.Serialize(peImageBuilder);

            return peImageBuilder.ToArray();
        }


        /// <summary> Write to a file an assembly that don't depend on other dynamic assembly </summary>
        public void GenerateAssembly(Assembly assembly, string path) =>
            GenerateAssembly(assembly, Array.Empty<Assembly>(), path);

        /// <summary> Serialize an assembly to a file </summary>
        /// <param name="assembly"> Assembly to be serialized </param>
        /// <param name="referencedDynamicAssembly">
        /// List of other assembly that have types referenced by <see cref="assembly"/>
        /// and that are dynamic assembly. The .net assembly loader can't find those
        /// otherwize
        /// </param>
        /// <param name="path"> Output file path </param>
        public void GenerateAssembly(Assembly assembly, IEnumerable<Assembly> referencedDynamicAssembly, string path)
        {
            var bytes = GenerateAssemblyBytes(assembly, referencedDynamicAssembly);
            File.WriteAllBytes(path, bytes);
        }
    }
}