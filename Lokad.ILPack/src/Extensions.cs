using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Lokad.ILPack
{
    public static class Extensions
    {
        private static PrimitiveTypeCode GetPrimitiveTypeCode(Type type)
        {
            // What is PrimitiveTypeCode.TypedReference? It's not used for now

            if (type == typeof(Boolean))
                return PrimitiveTypeCode.Boolean;
            else if (type == typeof(Byte))
                return PrimitiveTypeCode.Byte;
            else if (type == typeof(Char))
                return PrimitiveTypeCode.Char;
            else if (type == typeof(Double))
                return PrimitiveTypeCode.Double;
            else if (type == typeof(Int16))
                return PrimitiveTypeCode.Int16;
            else if (type == typeof(Int32))
                return PrimitiveTypeCode.Int32;
            else if (type == typeof(Int64))
                return PrimitiveTypeCode.Int64;
            else if (type == typeof(IntPtr))
                return PrimitiveTypeCode.IntPtr;
            else if (type == typeof(Object))
                return PrimitiveTypeCode.Object; // this strange, because Object is not primitive type
            else if (type == typeof(SByte))
                return PrimitiveTypeCode.SByte;
            else if (type == typeof(Single))
                return PrimitiveTypeCode.Single;
            else if (type == typeof(String))
                return PrimitiveTypeCode.String; // this strange, because String is not primitive type
            else if (type == typeof(UInt16))
                return PrimitiveTypeCode.UInt16;
            else if (type == typeof(UInt32))
                return PrimitiveTypeCode.UInt32;
            else if (type == typeof(UInt64))
                return PrimitiveTypeCode.UInt64;
            else if (type == typeof(UIntPtr))
                return PrimitiveTypeCode.UIntPtr;
            else if (type == typeof(void))
                return PrimitiveTypeCode.Void;

            throw new ArgumentException($"Type {type.Name} is unknown");
        }

        internal static void FromSystemType(
            this ReturnTypeEncoder typeEncoder,
            Type type,
            AssemblyGenerator generator)
        {
            if (type == typeof(void))
            {
                typeEncoder.Void();
            }
            else
            {
                typeEncoder.Type().FromSystemType(type, generator);
            }
        }

        internal static void FromSystemType(
            this SignatureTypeEncoder typeEncoder,
            Type type,
            AssemblyGenerator generator)
        {
            if (type.IsPrimitive)
            {
                typeEncoder.PrimitiveType(GetPrimitiveTypeCode(type));
            }
            else if (type == typeof(String))
            {
                typeEncoder.String();
            }
            else if (type == typeof(Object))
            {
                typeEncoder.Object();
            }
            else if (type == typeof(void))
            {
                throw new ArgumentException("Void type is not allowed in SignatureTypeEncoder. Please, use FromSystemType from ReturnTypeEncoder.");
                //typeEncoder.VoidPointer();
            }
            else if (type.IsArray)
            {
                var elementType = type.GetElementType();
                var rank = type.GetArrayRank();

                typeEncoder.Array(
                    x => x.FromSystemType(elementType, generator),
                    x => x.Shape(
                        type.GetArrayRank(),
                        ImmutableArray.Create<int>(),
                        ImmutableArray.Create<int>()));
            }
            else if (type.IsGenericType)
            {
                throw new ArgumentException("Generic types not supported for now!");
            }
            else
            {

                var typeHandler = generator.GetOrCreateType(type);
                typeEncoder.Type(typeHandler, type.IsValueType);
            }
        }
    }
}
