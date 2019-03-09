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

            if (type == typeof(bool))
            {
                return PrimitiveTypeCode.Boolean;
            }

            if (type == typeof(byte))
            {
                return PrimitiveTypeCode.Byte;
            }

            if (type == typeof(char))
            {
                return PrimitiveTypeCode.Char;
            }

            if (type == typeof(double))
            {
                return PrimitiveTypeCode.Double;
            }

            if (type == typeof(short))
            {
                return PrimitiveTypeCode.Int16;
            }

            if (type == typeof(int))
            {
                return PrimitiveTypeCode.Int32;
            }

            if (type == typeof(long))
            {
                return PrimitiveTypeCode.Int64;
            }

            if (type == typeof(IntPtr))
            {
                return PrimitiveTypeCode.IntPtr;
            }

            if (type == typeof(object))
            {
                return PrimitiveTypeCode.Object; // this strange, because Object is not primitive type
            }

            if (type == typeof(sbyte))
            {
                return PrimitiveTypeCode.SByte;
            }

            if (type == typeof(float))
            {
                return PrimitiveTypeCode.Single;
            }

            if (type == typeof(string))
            {
                return PrimitiveTypeCode.String; // this strange, because String is not primitive type
            }

            if (type == typeof(ushort))
            {
                return PrimitiveTypeCode.UInt16;
            }

            if (type == typeof(uint))
            {
                return PrimitiveTypeCode.UInt32;
            }

            if (type == typeof(ulong))
            {
                return PrimitiveTypeCode.UInt64;
            }

            if (type == typeof(UIntPtr))
            {
                return PrimitiveTypeCode.UIntPtr;
            }

            if (type == typeof(void))
            {
                return PrimitiveTypeCode.Void;
            }

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
            else if (type == typeof(string))
            {
                typeEncoder.String();
            }
            else if (type == typeof(object))
            {
                typeEncoder.Object();
            }
            else if (type == typeof(void))
            {
                throw new ArgumentException(
                    "Void type is not allowed in SignatureTypeEncoder. Please, use FromSystemType from ReturnTypeEncoder.");
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
                var genericTypeDef = type.GetGenericTypeDefinition();
                var typeHandler = generator.GetOrCreateType(genericTypeDef);
                var genericArguments = type.GetGenericArguments();

                var inst = typeEncoder.GenericInstantiation(typeHandler, genericArguments.Length, false);
                foreach (var ga in genericArguments)
                {
                    if (ga.IsGenericParameter)
                    {
                        inst.AddArgument().GenericTypeParameter(ga.GenericParameterPosition);
                    }
                    else
                    {
                        inst.AddArgument().FromSystemType(ga, generator);
                    }
                }
            }
            else
            {
                var typeHandler = generator.GetOrCreateType(type);
                typeEncoder.Type(typeHandler, type.IsValueType);
            }
        }
    }
}