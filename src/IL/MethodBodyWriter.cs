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
        private static int GetParameterPosition(ParameterInfo parameterInfo)
        {
            var method = parameterInfo.Member as MethodBase;
            if (method == null)
            {
                throw new ArgumentException("Declaring constructor or method cannot be null.", nameof(parameterInfo));
            }

            return parameterInfo.Position + (method.IsStatic ? 0 : 1);
        }

        public static void Write(IAssemblyMetadata metadata, IReadOnlyList<Instruction> il)
        {
            for (var i = 0; i < il.Count; i++)
            {
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
                            metadata.ILBuilder.WriteInt32(branchOffset);
                        }

                        break;

                    case OperandType.ShortInlineBrTarget:
                        var offset8 = (sbyte) il[i].Operand;
                        metadata.ILBuilder.WriteSByte(offset8);
                        break;

                    case OperandType.InlineBrTarget:
                        var offset32 = (int) il[i].Operand;
                        // offset convention in IL: zero is at next instruction
                        metadata.ILBuilder.WriteInt32(offset32);
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
                                    MetadataTokens.GetToken(metadata.GetConstructorHandle(constructorInfo, true)));
                                break;

                            case FieldInfo fieldInfo:
                                metadata.ILBuilder.WriteInt32(
                                    MetadataTokens.GetToken(metadata.GetFieldHandle(fieldInfo, true)));
                                break;

                            case MethodInfo methodInfo:
                                metadata.ILBuilder.WriteInt32(
                                    MetadataTokens.GetToken(metadata.GetMethodHandle(methodInfo, true)));
                                break;

                            default:
                                throw new NotSupportedException($"Unsupported inline operand: {il[i].Operand}");
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
                            metadata.ILBuilder.WriteByte((byte) GetParameterPosition(bParameterInfo));
                        }
                        else
                        {
                            throw new NotSupportedException($"Unsupported short inline variable: {il[i].Operand}");
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
                            metadata.ILBuilder.WriteUInt16((ushort) GetParameterPosition(sParameterInfo));
                        }
                        else
                        {
                            throw new NotSupportedException($"Unsupported inline variable: {il[i].Operand}");
                        }

                        break;

                    default:
                        throw new NotSupportedException($"Unsupported operand type: {opCode.OperandType}");
                }
            }
        }
    }
}