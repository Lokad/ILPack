using System;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Lokad.ILPack
{
    public partial class AssemblyGenerator
    {
        private void CreateTypes(Type[] types)
        {
            foreach (var type in types)
            {
                GetOrCreateType(type);
            }
        }

        private EntityHandle GetResolutionScopeForType(Type type)
        {
            return GetReferencedAssemblyForType(type);
        }

        internal EntityHandle CreateReferencedType(Type type)
        {
            if (_typeHandles.ContainsKey(type.GUID))
            {
                return _typeHandles[type.GUID];
            }

            var scope = GetResolutionScopeForType(type);
            var refType = _metadataBuilder.AddTypeReference(
                scope,
                GetString(type.Namespace),
                GetString(type.Name));

            _typeHandles.Add(type.GUID, refType);

            CreateConstructorForReferencedType(type);
            CreateCustomAttributes(refType, type.GetCustomAttributesData());

            return refType;
        }

        internal bool IsReferencedType(Type type)
        {
            // todo, also maybe in Module, ModuleRef, AssemblyRef and TypeRef
            // ECMA-335 page 273-274
            return type.Assembly != _currentAssembly;
        }

        internal EntityHandle GetOrCreateType(Type type)
        {
            if (_typeHandles.ContainsKey(type.GUID))
            {
                return _typeHandles[type.GUID];
            }

            var baseType = default(EntityHandle);

            if (IsReferencedType(type))
            {
                return CreateReferencedType(type);
            }

            if (type.BaseType != null)
            {
                var bsType = type.BaseType;
                if (bsType.Assembly != _currentAssembly)
                {
                    var bsTypeRef = CreateReferencedType(bsType);
                    _typeHandles[bsType.GUID] = bsTypeRef;
                    baseType = bsTypeRef;
                }
                else
                {
                    baseType = GetOrCreateType(bsType);
                }
            }

            var fields = CreateFields(type.GetFields(AllFields));
            var propsHandle = CreatePropertiesForType(type.GetProperties(AllProperties));
            var methods = CreateMethods(type.GetMethods(AllMethods));

            CreateConstructors(type.GetConstructors());

            var def = _metadataBuilder.AddTypeDefinition(
                type.Attributes,
                GetString(type.Namespace),
                GetString(type.Name),
                baseType,
                fields.IsNil ? MetadataTokens.FieldDefinitionHandle(1) : fields,
                methods.IsNil ? MetadataTokens.MethodDefinitionHandle(1) : methods);

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
                            _metadataBuilder.AddGenericParameter(def, attr, GetString(parm.Name), i);

                        foreach (var constraint in parm.GetGenericParameterConstraints())
                        {
                            _metadataBuilder.AddGenericParameterConstraint(genericParameterHandle,
                                GetOrCreateType(constraint));
                        }
                    }
                }
            }

            _typeHandles[type.GUID] = def;

            if (propsHandle != default(PropertyDefinitionHandle))
            {
                _metadataBuilder.AddPropertyMap(def, propsHandle);
            }

            CreateCustomAttributes(def, type.GetCustomAttributesData());

            return def;
        }
    }
}