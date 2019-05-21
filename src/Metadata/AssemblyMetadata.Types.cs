﻿using System;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

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

            if (IsGenericTypeSpec(type))
            {
                return ResolveGenericTypeSpec(type);
            }

            if (IsReferencedType(type))
            {
                return ResolveTypeReference(type);
            }

            throw new ArgumentException($"Type cannot be found: {MetadataHelper.GetFriendlyName(type)}", nameof(type));
        }

        public bool IsReferencedType(Type type)
        {
            // todo, also maybe in Module, ModuleRef, AssemblyRef and TypeRef
            // ECMA-335 page 273-274
            return type.Assembly != SourceAssembly;
        }

        private EntityHandle ResolveTypeReference(Type type)
        {
            if (IsGenericTypeSpec(type))
            {
                return ResolveGenericTypeSpec(type);
            }

            if (!IsReferencedType(type))
            {
                throw new ArgumentException($"Reference type is expected: {MetadataHelper.GetFriendlyName(type)}",
                    nameof(type));
            }

            if (_typeRefHandles.TryGetValue(type, out var typeRef))
            {
                return typeRef;
            }

            var scope = GetReferencedAssemblyForType(type);
            var typeHandle = Builder.AddTypeReference(
                scope,
                GetOrAddString(type.Namespace),
                GetOrAddString(type.Name));

            _typeRefHandles.Add(type, typeHandle);

            return typeHandle;
        }


        public bool IsGenericTypeSpec(Type type)
        {
            return type.IsGenericMethodParameter || type.IsGenericParameter || (type.IsGenericType && !type.IsGenericTypeDefinition);
        }

        private EntityHandle ResolveGenericTypeSpec(Type type)
        {
            if (!IsGenericTypeSpec(type))
            {
                throw new ArgumentException($"Generic type spec is expected: {MetadataHelper.GetFriendlyName(type)}",
                    nameof(type));
            }

            if (_typeSpecHandles.TryGetValue(type, out var typeSpec))
            {
                return typeSpec;
            }

            var typeSpecEncoder = new BlobEncoder(new BlobBuilder()).TypeSpecificationSignature();
            typeSpecEncoder.FromSystemType(type, this);
            var typeSpecHandle = Builder.AddTypeSpecification(GetOrAddBlob(typeSpecEncoder.Builder));

            _typeSpecHandles.Add(type, typeSpecHandle);

            return typeSpecHandle;

        }

        public TypeDefinitionMetadata ReserveTypeDefinition(Type type, TypeDefinitionHandle handle)
        {
            var metadata = new TypeDefinitionMetadata(type, handle);
            _typeDefHandles.Add(type, metadata);
            return metadata;
        }

        public bool TryGetTypeDefinition(Type type, out TypeDefinitionMetadata metadata)
        {
            return _typeDefHandles.TryGetValue(type, out metadata);
        }
    }
}