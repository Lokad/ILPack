using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

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

            if (field.DeclaringType.IsExplicitLayout)
                _metadata.Builder.AddFieldLayout(handle, (int)Marshal.OffsetOf(field.DeclaringType, field.Name));

            VerifyEmittedHandle(metadata, handle);
            metadata.MarkAsEmitted();

            CreateCustomAttributes(handle, field.GetCustomAttributesData());

            if (field.IsStatic && (field.Attributes & FieldAttributes.HasFieldRVA) != 0)
            {
                // HasFieldRVA fields are used by C# for static array initializers 
                // (eg: x = new int { 1,2,3}).  The easiest way to understand these is to
                // look at an ildasm dump, but basically it creates a static field initialized
                // to point to a blob of data (via the RVA)
                // 
                // There's no reflection API to get the raw bytes from the RVA so we
                // author a little dynamic method to grab it.  This code basically mirrors
                // what C# generates for array initializers.

                // Allocate memory for the initialization data
                var bytes = new byte[Marshal.SizeOf(field.FieldType)];

                // Create a dynamic method
                var method = new DynamicMethod("getInitData", typeof(void), new System.Type[] {typeof(byte[]) }, true);
                var il = method.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);               // Load "bytes"
                il.Emit(OpCodes.Ldtoken, field);        // Token for the field
                il.Emit(OpCodes.Call, typeof(System.Runtime.CompilerServices.RuntimeHelpers).GetMethod("InitializeArray"));
                il.Emit(OpCodes.Ret);
                var getInitData = (Action<byte[]>)method.CreateDelegate(typeof(Action<byte[]>));

                // Call it
                getInitData(bytes);

                // Write the data to the mapped field data builder
                _metadata.Builder.AddFieldRelativeVirtualAddress(handle, _metadata.MappedFieldDataBuilder.Count);
                _metadata.MappedFieldDataBuilder.WriteBytes(bytes);
            }
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