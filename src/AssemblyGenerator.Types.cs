﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using Lokad.ILPack.Metadata;

namespace Lokad.ILPack
{
    public partial class AssemblyGenerator
    {
        private void CreateTypes(IEnumerable<Type> types, List<DelayedWrite> genericParams)
        {
            var offsets = new TypeDefinitionMetadataOffset()
            {
                TypeIndex = _metadata.Builder.GetRowCount(TableIndex.TypeDef),
                FieldIndex = _metadata.Builder.GetRowCount(TableIndex.Field),
                PropertyIndex = _metadata.Builder.GetRowCount(TableIndex.PropertyMap),
                MethodIndex = _metadata.Builder.GetRowCount(TableIndex.MethodDef),
                EventIndex = _metadata.Builder.GetRowCount(TableIndex.EventMap)
            };

            // Reserve types
            foreach (var type in types)
            {
                ReserveType(type, ref offsets);
            }

            // Create types
            foreach (var type in types)
            {
                CreateType(type, genericParams);
            }
        }

        private void ReserveType(Type type, ref TypeDefinitionMetadataOffset offset)
        {
            var typeHandle = MetadataTokens.TypeDefinitionHandle(++offset.TypeIndex);
            _metadata.ReserveTypeDefinition(type, typeHandle);

            foreach (var field in type.GetFields(AllFields))
            {
                var handle = MetadataTokens.FieldDefinitionHandle(++offset.FieldIndex);
                _metadata.ReserveFieldDefinition(field, handle);
            }

            foreach (var property in type.GetProperties(AllProperties))
            {
                // We don't need to handle backing field. Because, it's handled as a regular field.
                // Also, we don't need to handle getter or setter. Because, they are handled as regular methods.
                var propertyHandle = MetadataTokens.PropertyDefinitionHandle(++offset.PropertyIndex);
                _metadata.ReservePropertyDefinition(property, propertyHandle);
            }

            foreach (var ev in type.GetEvents(AllEvents))
            {
                var eventHandle = MetadataTokens.EventDefinitionHandle(++offset.EventIndex);
                _metadata.ReserveEventDefinition(ev, eventHandle);
            }

            foreach (var ctor in type.GetConstructors(AllMethods))
            {
                var handle = MetadataTokens.MethodDefinitionHandle(++offset.MethodIndex);
                _metadata.ReserveConstructorDefinition(ctor, handle);
            }

            foreach (var method in type.GetMethods(AllMethods))
            {
                var handle = MetadataTokens.MethodDefinitionHandle(++offset.MethodIndex);
                _metadata.ReserveMethodDefinition(method, handle);
            }
        }

        private void CreateType(Type type, List<DelayedWrite> genericParams)
        {
            // Check reserved and not already emitted
            if (!_metadata.TryGetTypeDefinition(type, out var metadata))
            {
                ThrowMetadataIsNotReserved("Type", type);
            }
            EnsureMetadataWasNotEmitted(metadata, type);

            // Add the type definition
            var baseTypeHandle = type.BaseType != null ? _metadata.GetTypeHandle(type.BaseType) : default;
            var handle = _metadata.Builder.AddTypeDefinition(
                type.Attributes,
                type.DeclaringType == null ? _metadata.GetOrAddString(ApplyNameChange(type.Namespace)) : default(StringHandle),
                _metadata.GetOrAddString(Unescape(type.Name)),
                baseTypeHandle,
                MetadataTokens.FieldDefinitionHandle(_metadata.Builder.GetRowCount(TableIndex.Field) + 1),
                MetadataTokens.MethodDefinitionHandle(_metadata.Builder.GetRowCount(TableIndex.MethodDef) + 1));

            // Verify and mark emitted
            VerifyEmittedHandle(metadata, handle);
            metadata.MarkAsEmitted();

            // Setup pack and size attributes (if explicit layout)
            if (type.IsExplicitLayout)
            {
                _metadata.Builder.AddTypeLayout(
                    handle,
                    (ushort)type.StructLayoutAttribute.Pack,
                    (uint)type.StructLayoutAttribute.Size
                    );
            }

            // Add implemented interfaces (not for enums though - eg: IComparable etc...)
            if (!type.IsEnum)
            {
                DeclareInterfacesAndCreateInterfaceMap(type, handle);
            }

            // Setup enclosing type
            if (type.DeclaringType != null)
            {
                _metadata.Builder.AddNestedType(handle, (TypeDefinitionHandle)_metadata.GetTypeHandle(type.DeclaringType));
            }

            // Create attributes
            CreateCustomAttributes(handle, type.GetCustomAttributesData());

            // Handle generics type
            if (type.IsGenericType)
            {
                if (type.IsGenericTypeDefinition)
                {
                    var genericType = type.GetGenericTypeDefinition();
                    var typeInfo = genericType.GetTypeInfo();

                    int index = 0;
                    foreach(var arg in typeInfo.GenericTypeParameters)
                    {
                        var attr = arg.GenericParameterAttributes;

                        genericParams.Add(new DelayedWrite(CodedIndex.TypeOrMethodDef(handle), () =>
                        {
                            var gpHandle = _metadata.Builder.AddGenericParameter(handle, attr, _metadata.GetOrAddString(arg.Name), index++);

                            foreach (var constraint in arg.GetGenericParameterConstraints())
                            {
                                _metadata.Builder.AddGenericParameterConstraint(gpHandle,
                                          _metadata.GetTypeHandle(constraint));
                            }
                        }));
                    }
                }
            }

            // Create members...
            CreateFields(type.GetFields(AllFields));
            CreatePropertiesForType(type.GetProperties(AllProperties));
            CreateEventsForType(type.GetEvents(AllEvents));
            CreateConstructors(type.GetConstructors(AllMethods));
            CreateMethods(type.GetMethods(AllMethods), genericParams);
        }

        private static string Unescape(string str)
        {
            int idx = str.IndexOf('\\');
            if (idx < 0)
                return str;
            StringBuilder sb = new StringBuilder();
            int prevIdx = 0;
            while (idx >= 0 && idx < str.Length - 1)
            {
                sb.Append(str.Substring(prevIdx, idx - prevIdx));
                prevIdx = idx + 1;
                idx = str.IndexOf("\\", idx + 2);
            }
            sb.Append(str.Substring(prevIdx));
            return sb.ToString();
        }

        private void DeclareInterfacesAndCreateInterfaceMap(Type type, TypeDefinitionHandle handle)
        {
            // Only add those interfaces that are not already implemented by the base type.
            // This prevents the generation of assembly dependencies not covered by the
            // GetReferencedAssemblies method.
            HashSet<Type> interfaces
               = new HashSet<Type>((type.BaseType is null
                   ? type.GetInterfaces()
                   : type.GetInterfaces().Except(type.BaseType.GetInterfaces()))
                   .OrderBy(t => CodedIndex.TypeDefOrRefOrSpec(_metadata.GetTypeHandle(t))));

            foreach (var ifc in interfaces)
            {
                _metadata.Builder.AddInterfaceImplementation(handle, _metadata.GetTypeHandle(ifc));
            }

            // If the type is an interface there may be no interface map.
            // TODO: This isn't necessarily true for Default interface methods.
            if (type.IsInterface)
            {
                return;
            }

            // Build the interface map.
            // All interfaces need to be considered (not just the ones added by the type) because
            // the type may override interface methods of interfaces implemented by the base type.
            // It is also possible for a type to use a base type method to implement an interface
            // method of an interface added by the type. For these reasons it is not sufficient
            // to only use the interfaces declared by a type or look at the declaring type of a
            // method implementing an interface method.
            foreach (var ifcm in from ifc in type.GetInterfaces() select type.GetInterfaceMap(ifc))
            {
                bool implementedByType = interfaces.Contains(ifcm.InterfaceType);

                for (int i = 0; i < ifcm.InterfaceMethods.Length; ++i)
                {
                    MethodInfo targetMethod = ifcm.TargetMethods[i];
                    MethodInfo ifcMethod = ifcm.InterfaceMethods[i];

                    // Declare a method override either when the interface implementation or
                    // the interface method implementation is declared by the type.
                    if (implementedByType || targetMethod.DeclaringType.Equals(type))
                    {
                        // Mark the target method as implementing the interface method.
                        // (This is the equivalent of .Override in msil)
                        _metadata.Builder.AddMethodImplementation(
                           (TypeDefinitionHandle)_metadata.GetTypeHandle(targetMethod.DeclaringType),
                           _metadata.GetMethodHandle(targetMethod),
                           _metadata.GetMethodHandle(ifcMethod));
                    }
                }
            }
        }
    }
}
