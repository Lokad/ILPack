﻿using System;
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
            // In a field signature the field type of a field declared by a generic class
            // is the field type of the generic type definition's field, not the field type
            // of the declaring type's field. ECMA335 II.23.2.4 doesn't specify how to layout
            // the signature of a field of a generic type. This solution was derived by
            // disassembling compiler generated code.
            var type = fieldInfo.DeclaringType.IsGenericType
                ? fieldInfo.DeclaringType.GetGenericTypeDefinition().GetField(fieldInfo.Name, fieldInfo.GetBindingFlags()).FieldType
                : fieldInfo.FieldType;

            return GetOrAddBlob(MetadataHelper.BuildSignature(x =>
                x.FieldSignature().FromSystemType(type, this)));
        }

        public FieldDefinitionMetadata ReserveFieldDefinition(FieldInfo field, FieldDefinitionHandle handle)
        {
            var metadata = new FieldDefinitionMetadata(field, handle);
            _fieldDefHandles.Add(field, metadata);
            _unconstructedFieldDefs.Add(field.MetadataToken, field);
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
            if (field.Module.Assembly == SourceAssembly 
                && field.DeclaringType?.IsConstructedGenericType == true
                && _unconstructedFieldDefs.TryGetValue(field.MetadataToken, out var baseField))
            {
                field = baseField;
            }
            return _fieldDefHandles.TryGetValue(field, out metadata);
        }
    }
}