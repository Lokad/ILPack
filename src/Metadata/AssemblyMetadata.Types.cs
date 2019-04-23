using System;
using System.Reflection.Metadata;

namespace Lokad.ILPack.Metadata
{
    internal partial class AssemblyMetadata
    {
        public EntityHandle GetTypeHandle(Type type)
        {
            if (TryGetTypeDefinition(type, out var metadata))
            {
                return metadata.Handle;
            }

            if (IsReferencedType(type))
            {
                return ResolveTypeReference(type);
            }

            throw new InvalidOperationException($"Type cannot be found: {type}");
        }

        public bool IsReferencedType(Type type)
        {
            // todo, also maybe in Module, ModuleRef, AssemblyRef and TypeRef
            // ECMA-335 page 273-274
            return type.Assembly != SourceAssembly;
        }

        private EntityHandle GetResolutionScopeForType(Type type)
        {
            return GetReferencedAssemblyForType(type);
        }

        private EntityHandle ResolveTypeReference(Type type)
        {
            if (!IsReferencedType(type))
            {
                throw new ArgumentException("Reference type is expected.", nameof(type));
            }

            if (_typeRefHandles.TryGetValue(type.GUID, out var typeRef))
            {
                return typeRef;
            }

            var scope = GetResolutionScopeForType(type);
            var typeHandle = Builder.AddTypeReference(
                scope,
                GetOrAddString(type.Namespace),
                GetOrAddString(type.Name));

            _typeRefHandles.Add(type.GUID, typeHandle);

            // Create all public constructor references
            foreach (var ctor in type.GetConstructors())
            {
                var signature = GetConstructorSignature(ctor);
                var ctorRef = Builder.AddMemberReference(typeHandle, GetOrAddString(ctor.Name), signature);
                _ctorRefHandles.Add(ctor, ctorRef);
            }

            return typeHandle;
        }

        public TypeDefinitionMetadata ReserveTypeDefinition(Type type, TypeDefinitionHandle handle, int fieldIndex,
            int propertyIndex, int methodIndex)
        {
            var metadata = new TypeDefinitionMetadata(type, handle, fieldIndex, propertyIndex, methodIndex);
            _typeDefHandles.Add(type.GUID, metadata);
            return metadata;
        }

        public bool TryGetTypeDefinition(Type type, out TypeDefinitionMetadata metadata)
        {
            return _typeDefHandles.TryGetValue(type.GUID, out metadata);
        }
    }
}