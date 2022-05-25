using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Lokad.ILPack.Metadata;

namespace Lokad.ILPack.IL
{
    internal class MethodBodyStreamWriter
    {
        private readonly IAssemblyMetadata _metadata;

        public MethodBodyStreamWriter(IAssemblyMetadata metadata)
        {
            _metadata = metadata;
        }

        public int AddMethodBody(MethodBase methodBase, StandaloneSignatureHandle localVariablesSignature = default)
        {
            var body = methodBase.GetMethodBody();
            if (body == null)
            {
                return _metadata.ILBuilder.Count;
            }

            var localVariables = body.LocalVariables.ToArray();
            var localEncoder = new BlobEncoder(new BlobBuilder()).LocalVariableSignature(localVariables.Length);
            localEncoder.AddRange(localVariables, _metadata);

            var instructions = methodBase.GetInstructions();
            var maxStack = body.MaxStackSize;
            var codeSize = body.GetILAsByteArray().Length;
            var exceptionRegionCount = body.ExceptionHandlingClauses.Count;
            var attributes = body.InitLocals ? MethodBodyAttributes.InitLocals : MethodBodyAttributes.None;
            var hasDynamicStackAllocation = instructions.Any(x => x.OpCode == OpCodes.Localloc);

            // Header
            var offset = SerializeHeader(codeSize, maxStack, exceptionRegionCount, attributes, localVariablesSignature, hasDynamicStackAllocation);

            // Instructions
            MethodBodyWriter.Write(_metadata, instructions);

            // Exceptions
            SerializeExceptionRegions(body);

            return offset;
        }

        private void SerializeExceptionRegions(MethodBody body)
        {
            // Get exception clauses, quit if none
            var clauses = body.ExceptionHandlingClauses;
            if (clauses.Count == 0)
                return;

            // Can we use a small exception table?
            var useSmallTable = HasSmallExceptionRegions(body.ExceptionHandlingClauses);

            // Align to 4 byte boundary
            var exre = ExceptionRegionEncoder.SerializeTableHeader(_metadata.ILBuilder, clauses.Count, useSmallTable);
            foreach (var ex in body.ExceptionHandlingClauses)
            {
                exre.Add((ExceptionRegionKind)ex.Flags, ex.TryOffset, ex.TryLength, ex.HandlerOffset, ex.HandlerLength,
                    ex.Flags == ExceptionHandlingClauseOptions.Clause ? _metadata.GetTypeHandle(ex.CatchType) : default,
                    ex.Flags == ExceptionHandlingClauseOptions.Filter ? ex.FilterOffset : 0);
            }
        }

        private bool HasSmallExceptionRegions(IList<ExceptionHandlingClause> clauses)
        {
            System.Diagnostics.Debug.Assert(clauses != null);
 
            if (!ExceptionRegionEncoder.IsSmallRegionCount(clauses.Count))
            {
                return false;
            }
 
            foreach (var clause in clauses)
            {
                if (!ExceptionRegionEncoder.IsSmallExceptionRegion(clause.TryOffset, clause.TryLength) ||
                    !ExceptionRegionEncoder.IsSmallExceptionRegion(clause.HandlerOffset, clause.HandlerLength))
                {
                    return false;
                }
            }
 
            return true;
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
                offset = _metadata.ILBuilder.Count;
                _metadata.ILBuilder.WriteByte((byte) ((codeSize << 2) | TinyFormat));
            }
            else
            {
                _metadata.ILBuilder.Align(4);

                offset = _metadata.ILBuilder.Count;

                ushort flags = (3 << 12) | FatFormat;
                if (exceptionRegionCount > 0)
                {
                    flags |= MoreSections;
                }

                if (initLocals)
                {
                    flags |= InitLocals;
                }

                _metadata.ILBuilder.WriteUInt16((ushort) ((int) attributes | flags));
                _metadata.ILBuilder.WriteUInt16((ushort) maxStack);
                _metadata.ILBuilder.WriteInt32(codeSize);
                _metadata.ILBuilder.WriteInt32(
                    localVariablesSignature.IsNil ? 0 : MetadataTokens.GetToken(localVariablesSignature));
            }

            return offset;
        }
    }
}
