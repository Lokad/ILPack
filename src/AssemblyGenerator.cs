using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace Lokad.ILPack
{
    public partial class AssemblyGenerator
    {
        private Dictionary<string, AssemblyReferenceHandle> _assemblyReferenceHandles;
        private Dictionary<ConstructorInfo, MethodDefinitionHandle> _ctorDefHandles;
        private Dictionary<ConstructorInfo, MemberReferenceHandle> _ctorRefHandles;
        private Assembly _currentAssembly;
        private DebugDirectoryBuilder _debugDirectoryBuilder;
        private Dictionary<FieldInfo, FieldDefinitionHandle> _fieldHandles;
        private BlobBuilder _ilBuilder;
        private MetadataBuilder _metadataBuilder;
        private Dictionary<MethodInfo, MethodDefinitionHandle> _methodsHandles;
        private Dictionary<ParameterInfo, ParameterHandle> _parameterHandles;
        private Dictionary<PropertyInfo, PropertyDefinitionHandle> _propertyHandles;
        private Dictionary<Guid, EntityHandle> _typeHandles;

        private void Initialize(Assembly assembly)
        {
            _currentAssembly = assembly;
            _debugDirectoryBuilder = new DebugDirectoryBuilder();
            _ilBuilder = new BlobBuilder();
            _metadataBuilder = new MetadataBuilder();

            _typeHandles = new Dictionary<Guid, EntityHandle>();
            _ctorRefHandles = new Dictionary<ConstructorInfo, MemberReferenceHandle>();
            _ctorDefHandles = new Dictionary<ConstructorInfo, MethodDefinitionHandle>();
            _fieldHandles = new Dictionary<FieldInfo, FieldDefinitionHandle>();
            _methodsHandles = new Dictionary<MethodInfo, MethodDefinitionHandle>();
            _propertyHandles = new Dictionary<PropertyInfo, PropertyDefinitionHandle>();
            _parameterHandles = new Dictionary<ParameterInfo, ParameterHandle>();
            _assemblyReferenceHandles = new Dictionary<string, AssemblyReferenceHandle>();
        }

        public byte[] GenerateAssemblyBytes(Assembly assembly)
        {
            Initialize(assembly);

            if (_currentAssembly.EntryPoint != null)
            {
                // See "<Module>" type definition below.
                throw new NotSupportedException("Entry point is not supported.");
            }

            var name = _currentAssembly.GetName();

            var assemblyPublicKey = name.GetPublicKey();
            var assemblyHandle = _metadataBuilder.AddAssembly(
                GetString(name.Name),
                name.Version,
                GetString(name.CultureName),
                assemblyPublicKey.Length > 0 ? GetBlob(name.GetPublicKey()) : default(BlobHandle),
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
            _metadataBuilder.AddTypeDefinition(
                default(TypeAttributes),
                default(StringHandle),
                GetString("<Module>"),
                default(EntityHandle),
                MetadataTokens.FieldDefinitionHandle(1),
                MetadataTokens.MethodDefinitionHandle(1));

            CreateReferencedAssemblies(_currentAssembly.GetReferencedAssemblies());
            CreateCustomAttributes(assemblyHandle, _currentAssembly.GetCustomAttributesData());

            CreateModules(_currentAssembly.GetModules());
            CreateTypes(_currentAssembly.GetTypes());

            var entryPoint = GetMethodDefinitionHandle(_currentAssembly.EntryPoint);

            var metadataRootBuilder = new MetadataRootBuilder(_metadataBuilder);

            // Without Characteristics.ExecutableImage flag, .NET runtime refuses
            // to load an assembly even it's a DLL. PEHeaderBuilder.CreateLibraryHeader
            // does not set this flag. So, we set it explicitly.
            var header = new PEHeaderBuilder(imageCharacteristics: Characteristics.ExecutableImage |
                                                                   (entryPoint.IsNil ? Characteristics.Dll : 0));

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

        public void GenerateAssembly(Assembly assembly, string path)
        {
            var bytes = GenerateAssemblyBytes(assembly);
            File.WriteAllBytes(path, bytes);
        }
    }
}