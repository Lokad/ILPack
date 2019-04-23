﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Lokad.ILPack.Metadata;

namespace Lokad.ILPack
{
    public partial class AssemblyGenerator
    {
        private IEnumerable<Type> GetBaseTypes(Type type)
        {
            foreach (var inf in type.GetInterfaces())
            {
                if (_metadata.IsReferencedType(inf))
                {
                    continue;
                }

                yield return inf;

                foreach (var innerInf in GetBaseTypes(inf))
                {
                    yield return innerInf;
                }
            }

            var baseType = type.BaseType;
            if (baseType != null)
            {
                while (!_metadata.IsReferencedType(baseType))
                {
                    yield return baseType;
                    baseType = baseType.BaseType;
                }
            }
        }

        private void CreateTypes(IEnumerable<Type> types)
        {
            // Sort types by base types.
            var sortedTypes = types.TopologicalSort(GetBaseTypes).ToList();

            // First, reserve metadata for all types            
            ReserveTypes(sortedTypes);

            // Then, emit metadata
            foreach (var type in sortedTypes)
            {
                CreateFields(type.GetFields(AllFields));
                CreatePropertiesForType(type.GetProperties(AllProperties));
                CreateConstructors(type.GetConstructors(AllMethods));
                CreateMethods(type.GetMethods(AllMethods));

                if (!_metadata.TryGetTypeDefinition(type, out var metadata))
                {
                    throw new InvalidOperationException($"Type definition metadata cannot be found: {type}");
                }

                metadata.MarkAsEmitted();
            }
        }

        private void ReserveTypes(IEnumerable<Type> types)
        {
            var offset = new TypeDefinitionMetadataOffset
            {
                FieldIndex = _metadata.Builder.GetRowCount(TableIndex.Field),
                PropertyIndex = _metadata.Builder.GetRowCount(TableIndex.PropertyMap),
                MethodIndex = _metadata.Builder.GetRowCount(TableIndex.MethodDef)
            };

            foreach (var type in types)
            {
                var nextOffset = ReserveTypeDefinition(type, offset);
                offset = nextOffset;
            }
        }

        private TypeDefinitionMetadataOffset ReserveTypeDefinition(Type type, TypeDefinitionMetadataOffset offset)
        {
            var baseTypeHandle = type.BaseType != null ? _metadata.GetTypeHandle(type.BaseType) : default;

            var fieldRowCount = offset.FieldIndex;
            var propertyRowCount = offset.PropertyIndex;
            var methodRowCount = offset.MethodIndex;

            foreach (var field in type.GetFields(AllFields))
            {
                var handle = MetadataTokens.FieldDefinitionHandle(fieldRowCount + 1);
                _metadata.ReserveFieldDefinition(field, handle);
                ++fieldRowCount;
            }

            foreach (var property in type.GetProperties(AllProperties))
            {
                var propertyHandle = MetadataTokens.PropertyDefinitionHandle(propertyRowCount + 1);
                MethodDefinitionHandle getMethodHandle = default;
                MethodDefinitionHandle setMethodHandle = default;

                if (property.GetMethod != null)
                {
                    getMethodHandle = MetadataTokens.MethodDefinitionHandle(methodRowCount + 1);
                    ++methodRowCount;
                }

                if (property.SetMethod != null)
                {
                    setMethodHandle = MetadataTokens.MethodDefinitionHandle(methodRowCount + 1);
                    ++methodRowCount;
                }

                _metadata.ReservePropertyDefinition(property, propertyHandle, getMethodHandle, setMethodHandle);
                ++propertyRowCount;
            }

            foreach (var ctor in type.GetConstructors(AllMethods))
            {
                var handle = MetadataTokens.MethodDefinitionHandle(methodRowCount + 1);
                _metadata.ReserveConstructorDefinition(ctor, handle);
                ++methodRowCount;
            }

            foreach (var method in type.GetMethods(AllMethods))
            {
                var handle = MetadataTokens.MethodDefinitionHandle(methodRowCount + 1);
                _metadata.ReserveMethodDefinition(method, handle);
                ++methodRowCount;
            }

            var typeHandle = _metadata.Builder.AddTypeDefinition(
                type.Attributes,
                _metadata.GetOrAddString(type.Namespace),
                _metadata.GetOrAddString(type.Name),
                baseTypeHandle,
                MetadataTokens.FieldDefinitionHandle(offset.FieldIndex + 1),
                MetadataTokens.MethodDefinitionHandle(offset.MethodIndex + 1));

            // Add immediately to support self referencing generics
            _metadata.ReserveTypeDefinition(type, typeHandle, offset.FieldIndex, offset.PropertyIndex,
                offset.MethodIndex);

            // Handle generics type
            if (type.IsGenericType)
            {
                if (type.IsGenericTypeDefinition)
                {
                    var genericType = type.GetGenericTypeDefinition();
                    var typeInfo = genericType.GetTypeInfo();

                    for (var i = 0; i < typeInfo.GenericTypeParameters.Length; ++i)
                    {
                        var parm = typeInfo.GenericTypeParameters[i];
                        var attr = parm.GenericParameterAttributes;

                        var genericParameterHandle =
                            _metadata.Builder.AddGenericParameter(typeHandle, attr, _metadata.GetOrAddString(parm.Name),
                                i);

                        foreach (var constraint in parm.GetGenericParameterConstraints())
                        {
                            _metadata.Builder.AddGenericParameterConstraint(genericParameterHandle,
                                _metadata.GetTypeHandle(constraint));
                        }
                    }
                }
            }

            return new TypeDefinitionMetadataOffset
            {
                FieldIndex = fieldRowCount,
                PropertyIndex = propertyRowCount,
                MethodIndex = methodRowCount
            };
        }
    }
}