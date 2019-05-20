using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Lokad.ILPack
{
    // TODO: [vermorel] missing dictionary for 'CustomAttributeHandle'

    public partial class AssemblyGenerator
    {
        public void CreateCustomAttributes(EntityHandle parent, IEnumerable<CustomAttributeData> attributes)
        {
            foreach (var attr in attributes)
            {
                // Get the attribute type and make sure handle created
                var attrType = attr.AttributeType;
                var attrTypeHandle = _metadata.GetTypeHandle(attrType); // create type

                // Get handle to the constructor
                var ctorHandle = _metadata.GetConstructorHandle(attr.Constructor);
                System.Diagnostics.Debug.Assert(!ctorHandle.IsNil);

                // Encode the attribute values
                var enc = new BlobEncoder(new BlobBuilder());
                enc.CustomAttributeSignature(
                    (fa) => EncodeFixedAttributes(fa, attr), 
                    (na) => EncodeNamedAttributes(na, attr)
                    );

                // Add attribute to the entity
                _metadata.Builder.AddCustomAttribute(parent, ctorHandle, _metadata.Builder.GetOrAddBlob(enc.Builder));
            }
        }

        static void EncodeFixedAttributes(FixedArgumentsEncoder fa, CustomAttributeData attr)
        {
            var args = attr.ConstructorArguments;
            foreach (var a in args)
            {
                // Add it
                if (a.Value is Type type)
                {
                    fa.AddArgument().Scalar().SystemType(type.FullName);
                }
                else
                {
                    // Check argument type supported (ie: simple scalar values)
                    PrimitiveTypeCodeFromSystemTypeCode(a.ArgumentType);
                    fa.AddArgument().Scalar().Constant(a.Value);
                }
            }
        }

        static void EncodeNamedAttributes(CustomAttributeNamedArgumentsEncoder na, CustomAttributeData attr)
        {
            var args = attr.NamedArguments;
            var enc = na.Count(args.Count);
            foreach (var a in args)
            {
                // Encode it
                enc.AddArgument(a.IsField, out var typeEnc, out var nameEnc, out var litEnc);

                nameEnc.Name(a.MemberName);

                if (a.TypedValue.Value is Type type)
                {
                    typeEnc.ScalarType().SystemType();
                    litEnc.Scalar().SystemType(type.FullName);
                }
                else
                {
                    // Work out the primitive type code
                    var primTypeCode = PrimitiveTypeCodeFromSystemTypeCode(a.TypedValue.ArgumentType);
                    typeEnc.ScalarType().PrimitiveType(primTypeCode);
                    litEnc.Scalar().Constant(a.TypedValue.Value);
                }
            }
        }

        static PrimitiveSerializationTypeCode PrimitiveTypeCodeFromSystemTypeCode(Type sysType)
        {
            switch (Type.GetTypeCode(sysType))
            {
                case TypeCode.Boolean: return PrimitiveSerializationTypeCode.Boolean;
                case TypeCode.Char: return PrimitiveSerializationTypeCode.Char;
                case TypeCode.SByte: return PrimitiveSerializationTypeCode.SByte;
                case TypeCode.Byte: return PrimitiveSerializationTypeCode.Byte;
                case TypeCode.Int16: return PrimitiveSerializationTypeCode.Int16;
                case TypeCode.UInt16: return PrimitiveSerializationTypeCode.UInt16;
                case TypeCode.Int32: return PrimitiveSerializationTypeCode.Int32;
                case TypeCode.UInt32: return PrimitiveSerializationTypeCode.UInt32;
                case TypeCode.Int64: return PrimitiveSerializationTypeCode.Int64;
                case TypeCode.UInt64: return PrimitiveSerializationTypeCode.UInt64;
                case TypeCode.Single: return PrimitiveSerializationTypeCode.Single;
                case TypeCode.Double: return PrimitiveSerializationTypeCode.Double;
                case TypeCode.String: return PrimitiveSerializationTypeCode.String;
            }

            throw new NotImplementedException($"Unsupported primitive type: {sysType}");
        }
    }
}