using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace Lokad.ILPack
{
    public partial class AssemblyGenerator : IDisposable
    {
        private readonly Dictionary<ConstructorInfo, MethodDefinitionHandle> _ctorDefHandles;
        private readonly Dictionary<ConstructorInfo, MemberReferenceHandle> _ctorRefHandles;
        private readonly Dictionary<FieldInfo, FieldDefinitionHandle> _fieldHandles;
        private readonly Dictionary<MethodInfo, MethodDefinitionHandle> _methodsHandles;
        private readonly Dictionary<ParameterInfo, ParameterHandle> _parameterHandles;
        private readonly Dictionary<PropertyInfo, PropertyDefinitionHandle> _propertyHandles;

        private readonly Dictionary<Guid, EntityHandle> _typeHandles;
        private readonly Assembly _currentAssembly;

        private readonly DebugDirectoryBuilder _debugDirectoryBuilder;
        private readonly BlobBuilder _ilBuilder;
        private readonly MetadataBuilder _metadataBuilder;
        private readonly MemoryStream _peStream;

        public AssemblyGenerator(Assembly assembly)
        {
            _debugDirectoryBuilder = new DebugDirectoryBuilder();
            _peStream = new MemoryStream();
            _ilBuilder = new BlobBuilder();
            _metadataBuilder = new MetadataBuilder();
            _currentAssembly = assembly;

            _typeHandles = new Dictionary<Guid, EntityHandle>();
            _ctorRefHandles = new Dictionary<ConstructorInfo, MemberReferenceHandle>();
            _ctorDefHandles = new Dictionary<ConstructorInfo, MethodDefinitionHandle>();
            _fieldHandles = new Dictionary<FieldInfo, FieldDefinitionHandle>();
            _methodsHandles = new Dictionary<MethodInfo, MethodDefinitionHandle>();
            _propertyHandles = new Dictionary<PropertyInfo, PropertyDefinitionHandle>();
            _parameterHandles = new Dictionary<ParameterInfo, ParameterHandle>();
        }

        public void Dispose()
        {
            _peStream.Close();
        }

        public byte[] GenerateAssemblyBytes()
        {
            var name = _currentAssembly.GetName();

            var assemblyHandle = _metadataBuilder.AddAssembly(
                GetString(name.Name),
                name.Version,
                GetString(name.CultureName),
                GetBlob(name.GetPublicKey()),
                _assemblyNameFlagsConvert(name.Flags),
                _assemblyHashAlgorithmConvert(name.HashAlgorithm));

            CreateReferencedAssemblies(_currentAssembly.GetReferencedAssemblies());
            CreateCustomAttributes(assemblyHandle, _currentAssembly.GetCustomAttributesData());

            CreateModules(_currentAssembly.GetModules());
            CreateTypes(_currentAssembly.GetTypes());

            var entryPoint = GetMethodDefinitionHandle(_currentAssembly.EntryPoint);

            var metadataRootBuilder = new MetadataRootBuilder(_metadataBuilder);
            var header = PEHeaderBuilder.CreateLibraryHeader();

            var peBuilder = new ManagedPEBuilder(
                header,
                metadataRootBuilder,
                _ilBuilder,
                debugDirectoryBuilder: _debugDirectoryBuilder,
                entryPoint: entryPoint);

            var peImageBuilder = new BlobBuilder();
            peBuilder.Serialize(peImageBuilder);

            return peImageBuilder.ToArray();
        }

        public void GenerateAssembly(string path)
        {
            var bytes = GenerateAssemblyBytes();
            File.WriteAllBytes(path, bytes);
        }
    }
}