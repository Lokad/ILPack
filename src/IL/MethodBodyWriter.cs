using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata.Ecma335;
using Lokad.ILPack.Metadata;

namespace Lokad.ILPack.IL
{
    internal static class MethodBodyWriter
    {
        public static void Write(IAssemblyMetadata metadata, IReadOnlyList<Instruction> il)
        {
            var targetOffsets = new ArrayMapper<int>();
            //var offsetIndex = new Dictionary<int, int>();

            for (var i = 0; i < il.Count; i++)
            {
                //offsetIndex.Add(il[i].Offset, i);

                var opCode = il[i].OpCode;

                opCode.WriteOpCode(metadata.ILBuilder.WriteByte);

                switch (opCode.OperandType)
                {
                    case OperandType.InlineNone:
                        break;

                    case OperandType.InlineSwitch:
                        var branches = (int[]) il[i].Operand;
                        metadata.ILBuilder.WriteInt32(branches.Length);
                        for (var k = 0; k < branches.Length; k++)
                        {
                            var branchOffset = branches[k];
                            metadata.ILBuilder.WriteInt32(targetOffsets.Add(
                                branchOffset + il[i].Offset + opCode.Size + 4 * (branches.Length + 1)));
                        }

                        break;

                    case OperandType.ShortInlineBrTarget:
                        var offset8 = (sbyte) il[i].Operand;
                        // offset convention in IL: zero is at next instruction
                        metadata.ILBuilder.WriteSByte(
                            (sbyte) targetOffsets.Add(offset8 + il[i].Offset + opCode.Size + 1));
                        break;

                    case OperandType.InlineBrTarget:
                        var offset32 = (int) il[i].Operand;
                        // offset convention in IL: zero is at next instruction
                        metadata.ILBuilder.WriteInt32(targetOffsets.Add(offset32 + il[i].Offset + opCode.Size + 4));
                        break;

                    case OperandType.ShortInlineI:
                        if (opCode == OpCodes.Ldc_I4_S)
                        {
                            metadata.ILBuilder.WriteSByte((sbyte) il[i].Operand);
                        }
                        else
                        {
                            metadata.ILBuilder.WriteByte((byte) il[i].Operand);
                        }

                        break;

                    case OperandType.InlineI:
                        metadata.ILBuilder.WriteInt32((int) il[i].Operand);
                        break;

                    case OperandType.ShortInlineR:
                        metadata.ILBuilder.WriteSingle((float) il[i].Operand);
                        break;

                    case OperandType.InlineR:
                        metadata.ILBuilder.WriteDouble((double) il[i].Operand);
                        break;

                    case OperandType.InlineI8:
                        metadata.ILBuilder.WriteInt64((long) il[i].Operand);
                        break;

                    case OperandType.InlineSig:
                        metadata.ILBuilder.WriteBytes((byte[]) il[i].Operand);
                        break;

                    case OperandType.InlineString:
                        metadata.ILBuilder.WriteInt32(
                            MetadataTokens.GetToken(metadata.GetOrAddUserString((string) il[i].Operand)));
                        break;

                    case OperandType.InlineType:
                    case OperandType.InlineTok:
                    case OperandType.InlineMethod:
                    case OperandType.InlineField:
                        switch (il[i].Operand)
                        {
                            case Type type:
                                metadata.ILBuilder.WriteInt32(MetadataTokens.GetToken(metadata.GetTypeHandle(type)));
                                break;

                            case ConstructorInfo constructorInfo:
                                metadata.ILBuilder.WriteInt32(
                                    MetadataTokens.GetToken(metadata.GetConstructorHandle(constructorInfo)));
                                break;

                            case FieldInfo fieldInfo:
                                metadata.ILBuilder.WriteInt32(
                                    MetadataTokens.GetToken(metadata.GetFieldHandle(fieldInfo)));
                                break;

                            case MethodInfo methodInfo:
                                metadata.ILBuilder.WriteInt32(
                                    MetadataTokens.GetToken(metadata.GetMethodHandle(methodInfo)));
                                break;

                            default:
                                throw new NotSupportedException();
                        }

                        break;

                    case OperandType.ShortInlineVar:
                        var bLocalVariableInfo = il[i].Operand as LocalVariableInfo;
                        var bParameterInfo = il[i].Operand as ParameterInfo;

                        if (bLocalVariableInfo != null)
                        {
                            metadata.ILBuilder.WriteByte((byte) bLocalVariableInfo.LocalIndex);
                        }
                        else if (bParameterInfo != null)
                        {
                            metadata.ILBuilder.WriteByte((byte) bParameterInfo.Position);
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }

                        break;

                    case OperandType.InlineVar:
                        var sLocalVariableInfo = il[i].Operand as LocalVariableInfo;
                        var sParameterInfo = il[i].Operand as ParameterInfo;

                        if (sLocalVariableInfo != null)
                        {
                            metadata.ILBuilder.WriteUInt16((ushort) sLocalVariableInfo.LocalIndex);
                        }
                        else if (sParameterInfo != null)
                        {
                            metadata.ILBuilder.WriteUInt16((ushort) sParameterInfo.Position);
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }

                        break;

                    default:
                        throw new NotSupportedException();
                }
            }
        }
    }
}