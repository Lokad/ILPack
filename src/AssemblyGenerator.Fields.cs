using System.Collections.Generic;
using System.Reflection;

namespace Lokad.ILPack
{
    public partial class AssemblyGenerator
    {
        private const BindingFlags AllFields = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic |
                                               BindingFlags.DeclaredOnly | BindingFlags.CreateInstance |
                                               BindingFlags.Instance;

        private void CreateField(FieldInfo field)
        {
            if (!_metadata.TryGetFieldDefinition(field, out var metadata))
            {
                ThrowMetadataIsNotReserved("Field", field);
            }

            EnsureMetadataWasNotEmitted(metadata, field);

            var handle = _metadata.Builder.AddFieldDefinition(
                field.Attributes,
                _metadata.GetOrAddString(field.Name),
                _metadata.GetFieldSignature(field));

            if (field.Attributes.HasFlag(FieldAttributes.Literal))
                _metadata.Builder.AddConstant(handle, field.GetRawConstantValue());

            VerifyEmittedHandle(metadata, handle);
            metadata.MarkAsEmitted();

            CreateCustomAttributes(handle, field.GetCustomAttributesData());
        }

        private void CreateFields(IEnumerable<FieldInfo> fields)
        {
            foreach (var field in fields)
            {
                CreateField(field);
            }
        }
    }
}