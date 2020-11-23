using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using Lokad.ILPack.Metadata;

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

            throw new ArgumentException($"Type is unknown: {MetadataHelper.GetFriendlyName(type)}", nameof(type));
        }

        internal static BindingFlags GetBindingFlags(this FieldInfo fieldInfo)
        {
            BindingFlags result = fieldInfo.IsStatic
                ? BindingFlags.Static
                : BindingFlags.Instance;

            result |= FieldAttributes.Public == (fieldInfo.Attributes & FieldAttributes.FieldAccessMask)
                ? BindingFlags.Public
                : BindingFlags.NonPublic;
            return result;
        }

        internal static void FromSystemType(
            this ReturnTypeEncoder typeEncoder,
            Type type,
            IAssemblyMetadata metadata)
        {
            if (type == typeof(void))
            {
                typeEncoder.Void();
            }
            else if (type.IsByRef)
            {
                typeEncoder.Type(true).FromSystemType(type.GetElementType(), metadata);
            }
            else
            {
                typeEncoder.Type(false).FromSystemType(type, metadata);
            }
        }

        // Add a range of local variables to a local signature encoder
        internal static void AddRange(this LocalVariablesEncoder sig, IEnumerable<LocalVariableInfo> localVariables, IAssemblyMetadata metadata)
        {
            foreach (var v in localVariables)
            {
                Add(sig, v, metadata);
            }
        }

        // Add a local variable to a local variable signature encoder
        internal static void Add(this LocalVariablesEncoder sig, LocalVariableInfo localVariableInfo, IAssemblyMetadata metadata)
        {
            if (localVariableInfo.LocalType.IsByRef)
            {
                sig.AddVariable().Type(
                        true,
                        localVariableInfo.IsPinned)
                    .FromSystemType(localVariableInfo.LocalType.GetElementType(), metadata);
            }
            else
            {
                sig.AddVariable().Type(
                        false,
                        localVariableInfo.IsPinned)
                    .FromSystemType(localVariableInfo.LocalType, metadata);
            }
        }


        internal static void FromSystemType(this SignatureTypeEncoder typeEncoder, Type type,
            IAssemblyMetadata metadata)
        {
            if (type.IsByRef)
            {
                throw new ArgumentException("ByRef types should be handled by parameter encoder or return type encoder");
            }
            else if (type.IsPointer)
            {
                var elem_t = type.GetElementType();
                if (elem_t == typeof(void))
                {
                    typeEncoder.VoidPointer();
                }
                else
                {
                    typeEncoder.Pointer().FromSystemType(elem_t, metadata);
                }
            }
            else if (type.IsPrimitive)
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
                    "Void type is not allowed in SignatureTypeEncoder. Please, use FromSystemType from ReturnTypeEncoder.",
                    nameof(type));
            }
            else if (type.IsArray)
            {
                var elementType = type.GetElementType();

                if (type.GetArrayRank() == 1)
                {
                    typeEncoder.SZArray().FromSystemType(elementType, metadata);
                }
                else
                {
                    typeEncoder.Array(
                        x => x.FromSystemType(elementType, metadata),
                        x => x.Shape(
                            type.GetArrayRank(),
                            ImmutableArray.Create<int>(),
                            ImmutableArray.CreateRange<int>(Enumerable.Repeat(0, type.GetArrayRank())) // better matches metadata from C#
                            ));
                }
            }
            else if (type.IsGenericType)
            {
                var genericTypeDef = type.GetGenericTypeDefinition();
                var typeHandler = metadata.GetTypeHandle(genericTypeDef);
                var genericArguments = type.GetGenericArguments();

                var inst = typeEncoder.GenericInstantiation(typeHandler, genericArguments.Length, type.IsValueType);
                foreach (var ga in genericArguments)
                {
                    if (ga.IsGenericMethodParameter())
                    {
                        inst.AddArgument().GenericMethodTypeParameter(ga.GenericParameterPosition);
                    }
                    else if (ga.IsGenericParameter)
                    {
                        inst.AddArgument().GenericTypeParameter(ga.GenericParameterPosition);
                    }
                    else
                    {
                        inst.AddArgument().FromSystemType(ga, metadata);
                    }
                }
            }
            else if (type.IsGenericMethodParameter())
            {
                typeEncoder.GenericMethodTypeParameter(type.GenericParameterPosition);
            }
            else if (type.IsGenericParameter)
            {
                typeEncoder.GenericTypeParameter(type.GenericParameterPosition);
            }
            else
            {
                var typeHandler = metadata.GetTypeHandle(type);
                typeEncoder.Type(typeHandler, type.IsValueType);
            }
        }

#if NETSTANDARD || NET46
        internal static bool IsGenericMethodParameter(this Type type) => type.IsGenericParameter && type.DeclaringMethod != null;

        internal static bool IsConstructedGenericMethod(this MethodBase method) => method.IsGenericMethod && !method.IsGenericMethodDefinition;

        private static readonly Dictionary<byte[], GCHandle> s_peImages = new Dictionary<byte[], GCHandle>();

        internal static unsafe GCHandle GetPinnedPEImage(byte[] peImage)
        {
            lock(s_peImages)
            {
                if(!s_peImages.TryGetValue(peImage, out GCHandle pinned))
                {
                    s_peImages.Add(peImage, pinned = GCHandle.Alloc(peImage, GCHandleType.Pinned));
                }

                return pinned;
            }
        }

        internal static unsafe bool TryGetRawMetadata(this Assembly assembly, out byte* blob, out int length)
        {
            length = 0;
            blob = null;

            try
            {
                var ty = assembly.GetType();
                var pi = ty.GetMethod("GetRawBytes", BindingFlags.Instance | BindingFlags.NonPublic);
                byte[] peImage = null;
                if (pi == null) {
                    if (!String.IsNullOrEmpty(assembly.Location))
                        peImage = File.ReadAllBytes(assembly.Location);
                    else
                        return false;
                }
                else {
                    peImage = (byte[])pi.Invoke(assembly, null);
                }

                //var peImage = File.ReadAllBytes(assembly.Location);
                GCHandle pinned = GetPinnedPEImage(peImage);
                var headers = new PEHeaders(new MemoryStream(peImage));
                length = headers.MetadataSize;

                var source = pinned.AddrOfPinnedObject();
                var destination = new byte[length];

                Marshal.Copy(source + headers.MetadataStartOffset , destination, 0, length);

                var pinnedArray = GCHandle.Alloc(destination, GCHandleType.Pinned);
                var pointer = pinnedArray.AddrOfPinnedObject();
                blob = (byte*)pointer;

                return true;
            } catch (Exception ex)
            {
                return false;
            }
        }
#else
        internal static bool IsGenericMethodParameter( this Type type ) => type.IsGenericMethodParameter;

        internal static bool IsConstructedGenericMethod( this MethodBase method ) => method.IsConstructedGenericMethod;
#endif
    }
}