using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;

namespace Lokad.ILPack.Metadata
{
    internal partial class AssemblyMetadata : IAssemblyMetadata
    {
        private const BindingFlags AllMethods =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.DeclaredOnly | BindingFlags.CreateInstance | BindingFlags.Instance;

        private readonly Dictionary<string, AssemblyReferenceHandle> _assemblyRefHandles;
        private readonly Dictionary<ConstructorInfo, MethodBaseDefinitionMetadata> _ctorDefHandles;
        private readonly Dictionary<ConstructorInfo, MemberReferenceHandle> _ctorRefHandles;
        private readonly Dictionary<FieldInfo, FieldDefinitionMetadata> _fieldDefHandles;
        private readonly Dictionary<FieldInfo, MemberReferenceHandle> _fieldRefHandles;
        private readonly Dictionary<MethodInfo, MethodBaseDefinitionMetadata> _methodDefHandles;
        private readonly Dictionary<MethodInfo, MemberReferenceHandle> _methodRefHandles;
        private readonly Dictionary<MethodInfo, MethodSpecificationHandle> _methodSpecHandles;
        private readonly Dictionary<ParameterInfo, ParameterHandle> _parameterHandles;
        private readonly Dictionary<PropertyInfo, PropertyDefinitionMetadata> _propertyHandles;
        private readonly Dictionary<EventInfo, EventDefinitionMetadata> _eventHandles;
        private readonly Dictionary<Type, TypeDefinitionMetadata> _typeDefHandles;
        private readonly Dictionary<Type, TypeReferenceHandle> _typeRefHandles;
        private readonly Dictionary<Type, TypeSpecificationHandle> _typeSpecHandles;

        public AssemblyMetadata(Assembly sourceAssembly)
        {
            SourceAssembly = sourceAssembly;
            Builder = new MetadataBuilder();
            ILBuilder = new BlobBuilder();
            MappedFieldDataBuilder = new BlobBuilder();

            _assemblyRefHandles = new Dictionary<string, AssemblyReferenceHandle>();
            _ctorDefHandles = new Dictionary<ConstructorInfo, MethodBaseDefinitionMetadata>();
            _ctorRefHandles = new Dictionary<ConstructorInfo, MemberReferenceHandle>();
            _fieldDefHandles = new Dictionary<FieldInfo, FieldDefinitionMetadata>();
            _fieldRefHandles = new Dictionary<FieldInfo, MemberReferenceHandle>();
            _methodDefHandles = new Dictionary<MethodInfo, MethodBaseDefinitionMetadata>();
            _methodRefHandles = new Dictionary<MethodInfo, MemberReferenceHandle>();
            _methodSpecHandles = new Dictionary<MethodInfo, MethodSpecificationHandle>();
            _parameterHandles = new Dictionary<ParameterInfo, ParameterHandle>();
            _propertyHandles = new Dictionary<PropertyInfo, PropertyDefinitionMetadata>();
            _eventHandles = new Dictionary<EventInfo, EventDefinitionMetadata>();
            _typeDefHandles = new Dictionary<Type, TypeDefinitionMetadata>();
            _typeRefHandles = new Dictionary<Type, TypeReferenceHandle>();
            _typeSpecHandles = new Dictionary<Type, TypeSpecificationHandle>();

            var assemblies = new HashSet<AssemblyName>(SourceAssembly.GetReferencedAssemblies())
            {
                // HACK: [vermorel] 2019-07-25. 'GetReferencedAssemblies()' does not capture all assemblies,
                // only those that are explicitly referenced. However, when using an optional argument in
                // a method, the 'OptionalAttribute' from  'System.Core.Private' is used by not returned
                // by the method. Thus, we end-up adding the assembly manual.
                Assembly.GetAssembly(typeof(OptionalAttribute)).GetName() // System.Core.Private
            };

            CreateReferencedAssemblies(assemblies);
        }


        public Assembly SourceAssembly { get; }
        public MetadataBuilder Builder { get; }
        public BlobBuilder ILBuilder { get; }
        public BlobBuilder MappedFieldDataBuilder { get; }

        public UserStringHandle GetOrAddUserString(string value)
        {
            return value != null ? Builder.GetOrAddUserString(value) : default;
        }

        public BlobHandle GetOrAddBlob(byte[] value)
        {
            return value != null ? Builder.GetOrAddBlob(value) : default;
        }

        public BlobHandle GetOrAddBlob(BlobBuilder value)
        {
            return value != null ? Builder.GetOrAddBlob(value) : default;
        }

        public GuidHandle GetOrAddGuid(Guid value)
        {
            return Builder.GetOrAddGuid(value);
        }

        public StringHandle GetOrAddString(string value)
        {
            return value != null ? Builder.GetOrAddString(value) : default;
        }
    }
}
