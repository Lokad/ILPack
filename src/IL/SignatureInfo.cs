using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Lokad.ILPack.Metadata;

namespace Lokad.ILPack.IL
{
    public sealed class SignatureInfo
    {
        private readonly struct TypeWithModifiers
        {
            private readonly LinkedList<Type> _optionalModifiers;

            private readonly LinkedList<Type> _requiredModifiers;

            private readonly Type _type;

            private TypeWithModifiers(
                Type type)
                : this(
                    null,
                    null,
                    type)
            {
            }

            private TypeWithModifiers(
                TypeWithModifiers prototype,
                Type type)
                : this(
                    prototype._optionalModifiers,
                    prototype._requiredModifiers,
                    type)
            {
            }

            private TypeWithModifiers(
                LinkedList<Type> optionalModifiers,
                LinkedList<Type> requiredModifiers,
                Type type)
            {
                _optionalModifiers = optionalModifiers;
                _requiredModifiers = requiredModifiers;
                _type = type;
            }

            public Type Type => _type?.GetElementType() ?? _type;

            public bool IsByRef => _type.IsByRef;
            public IEnumerable<Type> RequiredModifiers =>
                _requiredModifiers ?? Enumerable.Empty<Type>();

            public IEnumerable<Type> OptionalModifiers =>
                _optionalModifiers ?? Enumerable.Empty<Type>();

            public bool HasModifiers =>
                _optionalModifiers != null || _requiredModifiers != null;

            public static implicit operator TypeWithModifiers(
                Type type) => new TypeWithModifiers(type);

            public TypeWithModifiers MakeArrayType() =>
                new TypeWithModifiers(this, _type.MakeArrayType());

            public TypeWithModifiers MakeByRefType() =>
                new TypeWithModifiers(this, _type.MakeByRefType());

            public TypeWithModifiers MakePointerType() =>
                new TypeWithModifiers(this, _type.MakePointerType());

            public TypeWithModifiers MakeArrayType(int rank) =>
                new TypeWithModifiers(this, _type.MakeArrayType(rank));

            public TypeWithModifiers MakeGenericType(
                Type[] typeArguments) =>
                new TypeWithModifiers(this, _type.MakeGenericType(typeArguments));

            public TypeWithModifiers AddModifier(
                Type modifier,
                Boolean isOptional) =>
                isOptional
                    ? new TypeWithModifiers(
                        AddLastSafe(_optionalModifiers, modifier), _requiredModifiers, _type)
                    : new TypeWithModifiers(
                        _optionalModifiers, AddLastSafe(_requiredModifiers, modifier), _type);

            private static LinkedList<Type> AddLastSafe(LinkedList<Type> list, Type item)
            {
                list ??= new LinkedList<Type>();
                list.AddLast(item);
                return list;
            }
        }

        private sealed class SignatureReader
        {
            private readonly Type[] _methodArguments;

            private readonly Type[] _typeArguments;

            private readonly Module _module;

            private BlobReader _reader;

            public SignatureReader(
                Module module,
                byte[] signature,
                Type [] typeArguments,
                Type [] methodArguments)
            {
                _methodArguments = methodArguments;
                _typeArguments = typeArguments;
                _reader = GetReader(signature);
                _module = module;
            }

            public SignatureCallingConvention ReadCallingConvention() =>
                _reader.ReadSignatureHeader().CallingConvention;

            public IEnumerable<TypeWithModifiers> ReadReturnTypeAndParameters() =>
                ReadTypes(_reader.ReadCompressedInteger() + 1);

            private static unsafe BlobReader GetReader(
                byte[] signature)
            {
                fixed (byte* pSignature = signature)
                {
                    return new BlobReader(pSignature, signature.Length);
                }
            }

            private TypeWithModifiers ReadType() =>
                // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
                _reader.ReadSignatureTypeCode() switch
                {
                    SignatureTypeCode.Array => ReadType().MakeArrayType(ReadArrayRank()),

                    SignatureTypeCode.ByReference => ReadType().MakeByRefType(),

                    SignatureTypeCode.Pointer => ReadType().MakePointerType(),

                    SignatureTypeCode.SZArray => ReadType().MakeArrayType(),

                    SignatureTypeCode.TypeHandle => ReadTypeFromToken(),

                    SignatureTypeCode.GenericMethodParameter =>
                        _methodArguments[_reader.ReadCompressedInteger()],

                    SignatureTypeCode.GenericTypeParameter =>
                        _typeArguments[_reader.ReadCompressedInteger()],

                    SignatureTypeCode.GenericTypeInstance =>
                        ReadType().MakeGenericType(ReadTypesArray()),

                    SignatureTypeCode.UIntPtr => typeof(UIntPtr),
                    SignatureTypeCode.IntPtr => typeof(IntPtr),

                    SignatureTypeCode.UInt64 => typeof(ulong),
                    SignatureTypeCode.UInt32 => typeof(uint),
                    SignatureTypeCode.UInt16 => typeof(ushort),
                    SignatureTypeCode.Byte => typeof(byte),

                    SignatureTypeCode.Int64 => typeof(long),
                    SignatureTypeCode.Int32 => typeof(int),
                    SignatureTypeCode.Int16 => typeof(short),
                    SignatureTypeCode.SByte => typeof(sbyte),

                    SignatureTypeCode.Boolean => typeof(bool),
                    SignatureTypeCode.String => typeof(string),
                    SignatureTypeCode.Object => typeof(object),
                    SignatureTypeCode.Char => typeof(char),

                    SignatureTypeCode.Double => typeof(double),
                    SignatureTypeCode.Single => typeof(float),

                    SignatureTypeCode.Void => null,

                    SignatureTypeCode.OptionalModifier => ReadTypeWithModifier(
                        ReadTypeFromToken(), true, ReadType()),
                    SignatureTypeCode.RequiredModifier => ReadTypeWithModifier(
                        ReadTypeFromToken(), false, ReadType()),

                    //SignatureTypeCode.FunctionPointer => null, // TODO: read SignatureInfo recursively

                    _ => throw new InvalidOperationException(
                        $"This type code is not supported yet.")
                };

            private Type[] ReadTypesArray() =>
                ReadTypes(_reader.ReadCompressedInteger()).Select(_ => _.Type).ToArray();

            private IEnumerable<TypeWithModifiers> ReadTypes(
                int count)
            {
                for (var index = 0; index < count; ++index)
                {
                    yield return ReadType();
                }
            }

            private Type ReadTypeFromToken() =>
                _module.ResolveType(
                    MetadataTokens.GetToken(_reader.ReadTypeHandle()));

            private TypeWithModifiers ReadTypeWithModifier(
                Type modifier,
                bool isOptional,
                TypeWithModifiers type) =>
                type.AddModifier(modifier, isOptional);

            private int ReadArrayRank()
            {
                var rank = _reader.ReadCompressedInteger();
                for (var index = rank + 2; index > 0; --index)
                {
                    _reader.ReadCompressedInteger();
                }
                return rank;
            }
        }

        private readonly SignatureCallingConvention _signatureCallingConvention;

        private readonly List<TypeWithModifiers> _returnTypeAndParameters;

        private SignatureInfo(
            SignatureCallingConvention signatureCallingConvention,
            List<TypeWithModifiers> returnTypeAndParameters)
        {
            _signatureCallingConvention = signatureCallingConvention;
            _returnTypeAndParameters = returnTypeAndParameters;
        }

        public BlobBuilder GetBlobBuilder(
            IAssemblyMetadata metadata)
        {
            var methodSignatureEncoder = new BlobEncoder(new BlobBuilder())
                .MethodSignature(_signatureCallingConvention);

            // Add return type and parameters
            methodSignatureEncoder.Parameters(
                _returnTypeAndParameters.Count - 1, // First type is return type
                returnTypeEncoder =>
                {
                    var returnType = _returnTypeAndParameters[0].Type;
                    if (returnType is null)
                    {
                        returnTypeEncoder.Void();
                    }
                    else
                    {
                        returnTypeEncoder.Type().FromSystemType(returnType, metadata);
                    }
                },
                parametersEncoder =>
                {
                    // Skip the first element in list - it's a return type
                    for (var index = 1; index < _returnTypeAndParameters.Count; index++)
                    {
                        var parameter = _returnTypeAndParameters[index];
                        var parameterTypeEncoder = parametersEncoder.AddParameter();

                        if (parameter.HasModifiers)
                        {
                            metadata.AddCustomModifiers(parameterTypeEncoder.CustomModifiers(),
                                parameter.RequiredModifiers, parameter.OptionalModifiers);
                        }

                        parameterTypeEncoder.Type(parameter.IsByRef).FromSystemType(parameter.Type, metadata);
                    }
                }
            );

            return methodSignatureEncoder.Builder;
        }

        public static SignatureInfo Create(
            int metadataToken,
            Type [] typeArguments,
            Type [] methodArguments,
            Module module)
        {
            var reader = new SignatureReader(
                module, module.ResolveSignature(metadataToken),
                typeArguments, methodArguments);

            var callingConvention = reader.ReadCallingConvention();
            var returnTypeAndParameters = reader.ReadReturnTypeAndParameters().ToList();

            return new SignatureInfo(callingConvention, returnTypeAndParameters);
        }
    }
}
