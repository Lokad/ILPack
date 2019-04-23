using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using Lokad.ILPack.Metadata;

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

            var blob = MetadataHelper.BuildSignature(x => x.PropertySignature()
                .Parameters(
                    countParameters,
                    r => r.FromSystemType(retType, _metadata),
                    p =>
                    {
                        foreach (var par in parameters)
                        {
                            var parEncoder = p.AddParameter();
                            parEncoder.Type().FromSystemType(par.ParameterType, _metadata);
                        }
                    }));
            return _metadata.GetOrAddBlob(blob);
        }

        private void CreateProperty(PropertyInfo property)
        {
            if (!_metadata.TryGetPropertyMetadata(property, out var metadata))
            {
                ThrowMetadataIsNotReserved("Property", property);
            }

            EnsureMetadataWasNotEmitted(metadata, property);

            var propertyHandle = _metadata.Builder.AddProperty(
                property.Attributes,
                _metadata.GetOrAddString(property.Name),
                GetPropertySignature(property));

            VerifyEmittedHandle(metadata, propertyHandle);
            metadata.MarkAsEmitted();

            if (property.GetMethod != null)
            {
                if (!_metadata.TryGetMethodDefinition(property.GetMethod, out var getMethodMetadata))
                {
                    ThrowMetadataIsNotReserved("Property getter method", property);
                }

                _metadata.Builder.AddMethodSemantics(
                    propertyHandle,
                    MethodSemanticsAttributes.Getter,
                    getMethodMetadata.Handle);
            }

            if (property.SetMethod != null)
            {
                if (!_metadata.TryGetMethodDefinition(property.SetMethod, out var setMethodMetadata))
                {
                    ThrowMetadataIsNotReserved("Property setter method", property);
                }

                _metadata.Builder.AddMethodSemantics(
                    propertyHandle,
                    MethodSemanticsAttributes.Setter,
                    setMethodMetadata.Handle);
            }
        }

        private void CreatePropertiesForType(IEnumerable<PropertyInfo> properties)
        {
            foreach (var property in properties)
            {
                CreateProperty(property);
            }
        }
    }
}