using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Lokad.ILPack.IL
{
    internal static class MethodBodyWriter
    {
        public static void Write(
            BlobBuilder writer,
            IReadOnlyList<Instruction> il,
            Func<string, StringHandle> getString,
            IReadOnlyDictionary<Guid, EntityHandle> typeHandles,
            IReadOnlyDictionary<ConstructorInfo, MemberReferenceHandle> ctorRefHandles,
            IReadOnlyDictionary<FieldInfo, FieldDefinitionHandle> fieldHandles,
            IReadOnlyDictionary<MethodInfo, MethodDefinitionHandle> methodHandles)
        {
            var targetOffsets = new ArrayMapper<int>();
            //var offsetIndex = new Dictionary<int, int>();

            for (var i = 0; i < il.Count; i++)
            {
                //offsetIndex.Add(il[i].Offset, i);

                var opCode = il[i].OpCode;

                opCode.WriteOpCode(writer.WriteByte);

                switch (opCode.OperandType)
                {
                    case OperandType.InlineNone:
                        break;

                    case OperandType.InlineSwitch:
                        var branches = (int[]) il[i].Operand;
                        writer.WriteInt32(branches.Length);
                        for (var k = 0; k < branches.Length; k++)
                        {
                            var branchOffset = branches[k];
                            writer.WriteInt32(targetOffsets.Add(
                                branchOffset + il[i].Offset + opCode.Size + 4 * (branches.Length + 1)));
                        }

                        break;

                    case OperandType.ShortInlineBrTarget:
                        var offset8 = (sbyte) il[i].Operand;
                        // offset convention in IL: zero is at next instruction
                        writer.WriteSByte((sbyte) targetOffsets.Add(offset8 + il[i].Offset + opCode.Size + 1));
                        break;

                    case OperandType.InlineBrTarget:
                        var offset32 = (int) il[i].Operand;
                        // offset convention in IL: zero is at next instruction
                        writer.WriteInt32(targetOffsets.Add(offset32 + il[i].Offset + opCode.Size + 4));
                        break;

                    case OperandType.ShortInlineI:
                        if (opCode == OpCodes.Ldc_I4_S)
                        {
                            writer.WriteSByte((sbyte) il[i].Operand);
                        }
                        else
                        {
                            writer.WriteByte((byte) il[i].Operand);
                        }

                        break;

                    case OperandType.InlineI:
                        writer.WriteInt32((int) il[i].Operand);
                        break;

                    case OperandType.ShortInlineR:
                        writer.WriteSingle((float) il[i].Operand);
                        break;

                    case OperandType.InlineR:
                        writer.WriteDouble((double) il[i].Operand);
                        break;

                    case OperandType.InlineI8:
                        writer.WriteInt64((long) il[i].Operand);
                        break;

                    case OperandType.InlineSig:
                        writer.WriteBytes((byte[]) il[i].Operand);
                        break;

                    case OperandType.InlineString:
                        writer.WriteInt32(MetadataTokens.GetToken(getString((string) il[i].Operand)));
                        break;

                    case OperandType.InlineType:
                    case OperandType.InlineTok:
                    case OperandType.InlineMethod:
                    case OperandType.InlineField:
                        switch (il[i].Operand)
                        {
                            case Type type:
                                writer.WriteInt32(MetadataTokens.GetToken(typeHandles[type.GUID]));
                                break;

                            case ConstructorInfo constructorInfo:
                                writer.WriteInt32(MetadataTokens.GetToken(ctorRefHandles[constructorInfo]));
                                break;

                            case FieldInfo fieldInfo:
                                writer.WriteInt32(MetadataTokens.GetToken(fieldHandles[fieldInfo]));
                                break;

                            case MethodInfo methodInfo:
                                writer.WriteInt32(MetadataTokens.GetToken(methodHandles[methodInfo]));
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
                            writer.WriteByte((byte) bLocalVariableInfo.LocalIndex);
                        }
                        else if (bParameterInfo != null)
                        {
                            writer.WriteByte((byte) bParameterInfo.Position);
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
                            writer.WriteUInt16((ushort) sLocalVariableInfo.LocalIndex);
                        }
                        else if (sParameterInfo != null)
                        {
                            writer.WriteUInt16((ushort) sParameterInfo.Position);
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