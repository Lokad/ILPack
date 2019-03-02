using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Lokad.ILPack.IL
{
    internal class MethodBodyStreamWriter
    {
        private readonly BlobBuilder _builder;
        private readonly IReadOnlyDictionary<ConstructorInfo, MemberReferenceHandle> _ctorRefHandles;
        private readonly IReadOnlyDictionary<FieldInfo, FieldDefinitionHandle> _fieldHandles;
        private readonly IReadOnlyDictionary<MethodInfo, MethodDefinitionHandle> _methodHandles;
        private readonly Func<string, StringHandle> _stringAccessor;
        private readonly IReadOnlyDictionary<Guid, EntityHandle> _typeHandles;

        public MethodBodyStreamWriter(BlobBuilder builder,
            Func<string, StringHandle> stringAccessor,
            IReadOnlyDictionary<Guid, EntityHandle> typeHandles,
            IReadOnlyDictionary<ConstructorInfo, MemberReferenceHandle> ctorRefHandles,
            IReadOnlyDictionary<FieldInfo, FieldDefinitionHandle> fieldHandles,
            IReadOnlyDictionary<MethodInfo, MethodDefinitionHandle> methodHandles)
        {
            _builder = builder;
            _stringAccessor = stringAccessor;
            _typeHandles = typeHandles;
            _ctorRefHandles = ctorRefHandles;
            _fieldHandles = fieldHandles;
            _methodHandles = methodHandles;
        }

        public int AddMethodBody(MethodBase methodBase)
        {
            var body = methodBase.GetMethodBody();
            if (body == null)
            {
                return _builder.Count;
            }

            var instructions = methodBase.GetInstructions();
            var maxStack = body.MaxStackSize;
            var codeSize = body.GetILAsByteArray().Length;
            var exceptionRegionCount = body.ExceptionHandlingClauses.Count;
            var attributes = body.InitLocals ? MethodBodyAttributes.InitLocals : MethodBodyAttributes.None;
            var localVariablesSignature = default(StandaloneSignatureHandle);
            var hasDynamicStackAllocation = instructions.Any(x => x.OpCode == OpCodes.Localloc);

            var offset = SerializeHeader(codeSize, maxStack, exceptionRegionCount, attributes, localVariablesSignature,
                hasDynamicStackAllocation);

            MethodBodyWriter.Write(_builder, instructions, _stringAccessor, _typeHandles, _ctorRefHandles,
                _fieldHandles, _methodHandles);
            return offset;
        }

        // Adapted from: https://github.com/dotnet/corefx/blob/772a2486f2dd29f3a0401427a26da23e845a6e59/src/System.Reflection.Metadata/src/System/Reflection/Metadata/Ecma335/Encoding/MethodBodyStreamEncoder.cs#L222-L272
        //
        private int SerializeHeader(int codeSize, int maxStack, int exceptionRegionCount,
            MethodBodyAttributes attributes, StandaloneSignatureHandle localVariablesSignature,
            bool hasDynamicStackAllocation)
        {
            const int TinyFormat = 2;
            const int FatFormat = 3;
            const int MoreSections = 8;
            const byte InitLocals = 0x10;

            var initLocals = (attributes & MethodBodyAttributes.InitLocals) != 0;

            var isTiny = codeSize < 64 &&
                         maxStack <= 8 &&
                         localVariablesSignature.IsNil && (!hasDynamicStackAllocation || !initLocals) &&
                         exceptionRegionCount == 0;

            int offset;
            if (isTiny)
            {
                offset = _builder.Count;
                _builder.WriteByte((byte) ((codeSize << 2) | TinyFormat));
            }
            else
            {
                _builder.Align(4);

                offset = _builder.Count;

                ushort flags = (3 << 12) | FatFormat;
                if (exceptionRegionCount > 0)
                {
                    flags |= MoreSections;
                }

                if (initLocals)
                {
                    flags |= InitLocals;
                }

                _builder.WriteUInt16((ushort) ((int) attributes | flags));
                _builder.WriteUInt16((ushort) maxStack);
                _builder.WriteInt32(codeSize);
                _builder.WriteInt32(
                    localVariablesSignature.IsNil ? 0 : MetadataTokens.GetToken(localVariablesSignature));
            }

            return offset;
        }
    }
}