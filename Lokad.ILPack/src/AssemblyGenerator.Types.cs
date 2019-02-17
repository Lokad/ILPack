using System;
using System.Reflection.Metadata;

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
                return _typeHandles[type.GUID];

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
                return _typeHandles[type.GUID];

            var baseType = default(EntityHandle);

            if (IsReferencedType(type))
                return CreateReferencedType(type);

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

            var propsHandle = CreatePropertiesForType(type.GetProperties(AllProperties));
            var methods = CreateMethods(type.GetMethods(AllMethods));

            CreateConstructors(type.GetConstructors());

            var fields = CreateFields(type.GetFields(AllFields));

            var def = _metadataBuilder.AddTypeDefinition(
                type.Attributes,
                GetString(type.Namespace),
                GetString(type.Name),
                baseType,
                fields,
                methods);

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