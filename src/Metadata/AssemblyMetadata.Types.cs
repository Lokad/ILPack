using System;
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

            if (type.IsArray)
            {
                return ResolveArrayTypeSpec(type);
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
            // Arrays are always referenced types
            if (type.IsArray)
                return true;

            // todo, also maybe in Module, ModuleRef, AssemblyRef and TypeRef
            // ECMA-335 page 273-274
            return type.Assembly != SourceAssembly;
        }

        private EntityHandle ResolveTypeReference(Type type)
        {
            if (type.IsArray)
            {
                return ResolveArrayTypeSpec(type);
            }

            if (IsGenericTypeSpec(type))
            {
                return ResolveGenericTypeSpec(type);
            }

            if (IsReferencedType(type))
            {
                if (_typeRefHandles.TryGetValue(type, out var typeRef))
                {
                    return typeRef;
                }
            }
            else
            {
                if (_typeDefHandles.TryGetValue(type, out var typeDef))
                {
                    return type.ContainsGenericParameters ? ResolveGenericTypeSpec(type) : typeDef.Handle;
                }

                throw new ArgumentException($"Reference type is expected: {MetadataHelper.GetFriendlyName(type)}",
                    nameof(type));
            }


            // For nested types the scope is the declaring type, not the assembly.
            var typeHandle = Builder.AddTypeReference(
                GetScopeForType(type),
                GetNamespaceForType(type),
                GetOrAddString(type.Name));

            _typeRefHandles.Add(type, typeHandle);

            return typeHandle;
        }

        public bool IsGenericTypeSpec(Type type)
        {
            return type.IsGenericMethodParameter() || type.IsGenericParameter || (type.IsGenericType && !type.IsGenericTypeDefinition);
        }

        private EntityHandle ResolveArrayTypeSpec(Type type)
        {
            if (!type.IsArray)
            {
                throw new ArgumentException($"Array type is expected: {MetadataHelper.GetFriendlyName(type)}",
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


        private EntityHandle ResolveGenericTypeSpec(Type type)
        {
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

        private EntityHandle GetScopeForType(Type type)
        {
            // The scope for nested types is the declaring type, not the assembly.
            return type.IsNested
               ? ResolveTypeReference(type.DeclaringType)
               : GetReferencedAssemblyForType(type);
        }

        private StringHandle GetNamespaceForType(Type type)
        {
            // For nested types the namespace is the same as the namespace of the
            // declaring type. In this case the returned string handle is nil.
            return type.IsNested ? default : GetOrAddString(type.Namespace);
        }
    }
}