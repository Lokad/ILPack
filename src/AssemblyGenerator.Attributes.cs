using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;

namespace Lokad.ILPack
{
    public partial class AssemblyGenerator
    {
        // TODO: This list is not exhaustive
        static readonly HashSet<Type> s_PseudoAttributes = new HashSet<Type>()
        {
            typeof(DllImportAttribute),
            typeof(ComImportAttribute),
            typeof(PreserveSigAttribute)
        };

        public void CreateCustomAttributes(EntityHandle parent, IEnumerable<CustomAttributeData> attributes)
        {
            foreach (var attr in attributes)
            {
                // Get the attribute type and make sure handle created
                var attrType = attr.AttributeType;

                // Check if the supplied attribute should be emitted or is encoded
                // directly in metadata.
                if (s_PseudoAttributes.Contains(attrType))
                {
                    continue;
                }

                var attrTypeHandle = _metadata.GetTypeHandle(attrType); // create type

                // Get handle to the constructor
                var ctorHandle = _metadata.GetConstructorHandle(attr.Constructor, false);
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

        static void EncodeLiteral(LiteralEncoder litEnc, CustomAttributeTypedArgument arg)
        {
            if (arg.Value is Type type)
            {
                // Type reference
                litEnc.Scalar().SystemType(type.FullName);
            }
            else if (arg.Value is ReadOnlyCollection<CustomAttributeTypedArgument> array)
            {
                // Array of values
                var subLitEnc = litEnc.Vector().Count(array.Count);
                foreach (var el in array)
                {
                    EncodeLiteral(subLitEnc.AddLiteral(), el);
                }
            }
            else if (arg.Value is null)
            {
                if (arg.ArgumentType.IsArray)
                {
                    litEnc.Scalar().NullArray();
                }
                else
                {
                    litEnc.Scalar().Constant(null);
                }
            }
            else
            {
                // Check argument type supported (ie: simple scalar values)
                PrimitiveTypeCodeFromSystemTypeCode(arg.Value.GetType());
                litEnc.Scalar().Constant(arg.Value);
            }
        }

        static void EncodeType(CustomAttributeElementTypeEncoder typeEnc, Type type)
        {
            if (type == typeof(Type))
            {
                typeEnc.SystemType();
            }
            else
            {
                // Work out the primitive type code
                var primTypeCode = PrimitiveTypeCodeFromSystemTypeCode(type);
                typeEnc.PrimitiveType(primTypeCode);
            }
        }

        static void EncodeType(NamedArgumentTypeEncoder typeEnc, Type type)
        {
            if (type == typeof(Type))
            {
                typeEnc.ScalarType().SystemType();
            }
            else if (type.IsArray)
            {
                EncodeType(typeEnc.SZArray().ElementType(), type.GetElementType());
            }
            else
            {
                // Work out the primitive type code
                var primTypeCode = PrimitiveTypeCodeFromSystemTypeCode(type);
                typeEnc.ScalarType().PrimitiveType(primTypeCode);
            }
        }

        static void EncodeFixedAttributes(FixedArgumentsEncoder fa, CustomAttributeData attr)
        {
            var args = attr.ConstructorArguments;
            foreach (var a in args)
            {
                EncodeLiteral(fa.AddArgument(), a);
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
                EncodeType(typeEnc, a.TypedValue.ArgumentType);
                nameEnc.Name(a.MemberName);
                EncodeLiteral(litEnc, a.TypedValue);
            }
        }

        static PrimitiveSerializationTypeCode PrimitiveTypeCodeFromSystemTypeCode(Type type)
        {
            switch (Type.GetTypeCode(type))
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

            throw new NotImplementedException($"Unsupported primitive type: {type}");
        }
    }
}