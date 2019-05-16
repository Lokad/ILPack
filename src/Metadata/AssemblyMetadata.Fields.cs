using System;
using System.Reflection;
using System.Reflection.Metadata;

namespace Lokad.ILPack.Metadata
{
    internal partial class AssemblyMetadata
    {
        public EntityHandle GetFieldHandle(FieldInfo field)
        {
            if (TryGetFieldDefinition(field, out var metadata))
            {
                return metadata.Handle;
            }

            if (IsReferencedType(field.DeclaringType))
            {
                return ResolveFieldReference(field);
            }

            throw new ArgumentException($"Field cannot be found: {MetadataHelper.GetFriendlyName(field)}",
                nameof(field));
        }

        public BlobHandle GetFieldSignature(FieldInfo fieldInfo)
        {
            var type = fieldInfo.FieldType;
            return GetOrAddBlob(MetadataHelper.BuildSignature(x =>
                x.FieldSignature().FromSystemType(type, this)));
        }

        public FieldDefinitionMetadata ReserveFieldDefinition(FieldInfo field, FieldDefinitionHandle handle)
        {
            var metadata = new FieldDefinitionMetadata(field, handle);
            _fieldDefHandles.Add(field, metadata);
            return metadata;
        }

        private EntityHandle ResolveFieldReference(FieldInfo field)
        {
            if (!IsReferencedType(field.DeclaringType))
            {
                throw new ArgumentException(
                    $"Field of a reference type is expected: {MetadataHelper.GetFriendlyName(field)}", nameof(field));
            }


            if (_fieldRefHandles.TryGetValue(field, out var handle))
            {
                return handle;
            }

            var typeRef = ResolveTypeReference(field.DeclaringType);
            var fieldRef = Builder.AddMemberReference(typeRef, GetOrAddString(field.Name), GetFieldSignature(field));
            _fieldRefHandles.Add(field, fieldRef);
            return fieldRef;
        }

        public bool TryGetFieldDefinition(FieldInfo field, out FieldDefinitionMetadata metadata)
        {
            return _fieldDefHandles.TryGetValue(field, out metadata);
        }
    }
}