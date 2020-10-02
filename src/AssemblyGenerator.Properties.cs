using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using Lokad.ILPack.Metadata;

namespace Lokad.ILPack
{
    public partial class AssemblyGenerator
    {
        private const BindingFlags AllProperties =
            BindingFlags.NonPublic | BindingFlags.Public |
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        private BlobHandle GetPropertySignature(PropertyInfo propertyInfo)
        {
            var parameters = propertyInfo.GetIndexParameters();
            var countParameters = parameters.Length;
            var retType = propertyInfo.PropertyType;

            // Work out if this is an instance property
            var eitherAccessor = propertyInfo.GetMethod ?? propertyInfo.SetMethod;
            System.Diagnostics.Debug.Assert(eitherAccessor != null);
            var isInstanceProperty = eitherAccessor.CallingConvention.HasFlag(CallingConventions.HasThis);

            var blob = MetadataHelper.BuildSignature(x => x.PropertySignature(isInstanceProperty)
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

        private void CreateProperty(PropertyInfo property, bool addToPropertyMap)
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

            // If this is the first property for this type then add to the property map
            // (Without this ildasm doesn't show the .property block and intellisense doesn't show any properties)
            if (addToPropertyMap)
            {
                if (!_metadata.TryGetTypeDefinition(property.DeclaringType, out var typeHandle))
                {
                    ThrowMetadataIsNotReserved("Type", property.DeclaringType);
                }
                _metadata.Builder.AddPropertyMap((TypeDefinitionHandle)typeHandle.Handle, propertyHandle);
            }

            VerifyEmittedHandle(metadata, propertyHandle);
            metadata.MarkAsEmitted();
            this.CreateCustomAttributes(propertyHandle, property.GetCustomAttributesData());
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
            bool first = true;
            foreach (var property in properties)
            {
                CreateProperty(property, first);
                first = false;
            }
        }
    }
}