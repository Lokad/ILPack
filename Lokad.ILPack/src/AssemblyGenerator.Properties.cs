using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

namespace Lokad.ILPack
{
    public partial class AssemblyGenerator
    {
        private readonly BindingFlags AllProperties =
            BindingFlags.NonPublic | BindingFlags.Public |
            BindingFlags.Instance | BindingFlags.Static;

        private BlobHandle GetPropertySignature(PropertyInfo propertyInfo)
        {
            var parameters = propertyInfo.GetIndexParameters();
            var countParameters = parameters.Length;
            var retType = propertyInfo.PropertyType;

            var blob = BuildSignature(x => x.PropertySignature()
                .Parameters(
                    countParameters,
                    r => r.FromSystemType(retType, this),
                    p =>
                    {
                        foreach (var par in parameters)
                        {
                            var parEncoder = p.AddParameter();
                            parEncoder.Type().FromSystemType(par.ParameterType, this);
                        }
                    }));
            return GetBlob(blob);
        }

        public PropertyDefinitionHandle CreatePropertiesForType(PropertyInfo[] properties)
        {
            if (properties.Length == 0)
            {
                return default(PropertyDefinitionHandle);
            }

            var handles = new PropertyDefinitionHandle[properties.Length];
            for (var i = 0; i < properties.Length; i++)
            {
                var property = properties[i];

                if (_propertyHandles.TryGetValue(property, out var propertyDef))
                {
                    handles[i] = propertyDef;
                    continue;
                }

                propertyDef = _metadataBuilder.AddProperty(
                    property.Attributes,
                    GetString(property.Name),
                    GetPropertySignature(property));

                _propertyHandles.Add(property, propertyDef);

                handles[i] = propertyDef;

                var getMethod = property.GetGetMethod(true);
                if (getMethod != null)
                {
                    _metadataBuilder.AddMethodSemantics(
                        propertyDef,
                        MethodSemanticsAttributes.Getter,
                        GetOrCreateMethod(getMethod));
                }

                var setMethod = property.GetSetMethod(true);
                if (setMethod != null)
                {
                    _metadataBuilder.AddMethodSemantics(
                        propertyDef,
                        MethodSemanticsAttributes.Setter,
                        GetOrCreateMethod(setMethod));
                }
            }

            return handles.First();
        }
    }
}