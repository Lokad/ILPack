using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

namespace Lokad.ILPack
{
    public partial class AssemblyGenerator
    {
        private readonly BindingFlags AllFields =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.DeclaredOnly | BindingFlags.CreateInstance |
            BindingFlags.Instance;

        private BlobHandle GetFieldSignature(FieldInfo fieldInfo)
        {
            var type = fieldInfo.FieldType;
            return GetBlob(
                BuildSignature(x =>
                    x.FieldSignature()
                        .FromSystemType(type, this)));
        }

        private FieldDefinitionHandle CreateFields(FieldInfo[] fields)
        {
            if (fields.Length == 0)
            {
                return default(FieldDefinitionHandle);
            }

            var handles = new FieldDefinitionHandle[fields.Length];
            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];

                if (_fieldHandles.TryGetValue(field, out var fieldDef))
                {
                    handles[i] = fieldDef;
                    continue;
                }

                fieldDef = _metadataBuilder.AddFieldDefinition(
                    field.Attributes,
                    GetString(field.Name),
                    GetFieldSignature(field));

                _fieldHandles.Add(field, fieldDef);

                handles[i] = fieldDef;

                CreateCustomAttributes(fieldDef, field.GetCustomAttributesData());
            }

            return handles.First();
        }
    }
}