using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Lokad.ILPack.IL
{
    internal class MethodBodyReader
    {
        private readonly ByteBuffer _il;
        private readonly IList<LocalVariableInfo> _locals;

        private readonly MethodBase _method;
        private readonly Type[] _methodArguments;
        private readonly Module _module;
        private readonly ParameterInfo[] _parameters;
        private readonly Type[] _typeArguments;

        private MethodBodyReader(MethodBase method)
        {
            _method = method;

            var body = method.GetMethodBody();
            if (body == null)
            {
                throw new ArgumentException();
            }

            var bytes = body.GetILAsByteArray();
            if (bytes == null)
            {
                throw new ArgumentException();
            }

            if (!(method is ConstructorInfo))
            {
                _methodArguments = method.GetGenericArguments();
            }

            if (method.DeclaringType != null)
            {
                _typeArguments = method.DeclaringType.GetGenericArguments();
            }

            _parameters = method.GetParameters();
            _locals = body.LocalVariables;
            _module = method.Module;
            _il = new ByteBuffer(bytes);
        }

        private List<Instruction> ReadInstructions()
        {
            var instructions = new List<Instruction>();
            var reader = (Func<byte>) (() => _il.ReadByte());

            while (_il._position < _il._buffer.Length)
            {
                var instruction = new Instruction(_il._position, reader.ReadOpCode());
                ReadOperand(instruction);
                instructions.Add(instruction);
            }

            return instructions;
        }

        private void ReadOperand(Instruction instruction)
        {
            switch (instruction.OpCode.OperandType)
            {
                case OperandType.InlineNone:
                    break;

                case OperandType.InlineSwitch:
                    var length = _il.ReadInt32();
                    var offsets = new int[length];
                    for (var i = 0; i < length; i++)
                    {
                        offsets[i] = _il.ReadInt32();
                    }

                    instruction.Operand = offsets;
                    break;

                case OperandType.ShortInlineBrTarget:
                    instruction.Operand = (sbyte) _il.ReadByte();
                    break;

                case OperandType.InlineBrTarget:
                    instruction.Operand = _il.ReadInt32();
                    break;

                case OperandType.ShortInlineI:
                    if (instruction.OpCode == OpCodes.Ldc_I4_S)
                    {
                        instruction.Operand = (sbyte) _il.ReadByte();
                    }
                    else
                    {
                        instruction.Operand = _il.ReadByte();
                    }

                    break;

                case OperandType.InlineI:
                    instruction.Operand = _il.ReadInt32();
                    break;

                case OperandType.ShortInlineR:
                    instruction.Operand = _il.ReadSingle();
                    break;

                case OperandType.InlineR:
                    instruction.Operand = _il.ReadDouble();
                    break;

                case OperandType.InlineI8:
                    instruction.Operand = _il.ReadInt64();
                    break;

                case OperandType.InlineSig:
                    instruction.Operand = _module.ResolveSignature(_il.ReadInt32());
                    break;

                case OperandType.InlineString:
                    instruction.Operand = _module.ResolveString(_il.ReadInt32());
                    break;

                case OperandType.InlineTok:
                    instruction.Operand = _module.ResolveMember(_il.ReadInt32(), _typeArguments, _methodArguments);
                    break;
                case OperandType.InlineType:
                    instruction.Operand = _module.ResolveType(_il.ReadInt32(), _typeArguments, _methodArguments);
                    break;

                case OperandType.InlineMethod:
                    instruction.Operand = _module.ResolveMethod(_il.ReadInt32(), _typeArguments, _methodArguments);
                    break;

                case OperandType.InlineField:
                    instruction.Operand = _module.ResolveField(_il.ReadInt32(), _typeArguments, _methodArguments);
                    break;

                case OperandType.ShortInlineVar:
                    instruction.Operand = GetVariable(instruction, _il.ReadByte());
                    break;

                case OperandType.InlineVar:
                    instruction.Operand = GetVariable(instruction, _il.ReadInt16());
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        private object GetVariable(Instruction instruction, int index)
        {
            if (TargetsLocalVariable(instruction.OpCode))
            {
                return GetLocalVariable(index);
            }

            return GetParameter(index);
        }

        private static bool TargetsLocalVariable(OpCode opcode)
        {
            return opcode.Name.Contains("loc");
        }

        private LocalVariableInfo GetLocalVariable(int index)
        {
            return _locals[index];
        }

        private ParameterInfo GetParameter(int index)
        {
            if (!_method.IsStatic)
            {
                index--;
            }

            return _parameters[index];
        }

        public static List<Instruction> GetInstructions(MethodBase method)
        {
            var reader = new MethodBodyReader(method);
            return reader.ReadInstructions();
        }

        private class ByteBuffer
        {
            internal readonly byte[] _buffer;
            internal int _position;

            public ByteBuffer(byte[] buffer)
            {
                _buffer = buffer;
            }

            public byte ReadByte()
            {
                CheckCanRead(1);
                return _buffer[_position++];
            }

            public byte[] ReadBytes(int length)
            {
                CheckCanRead(length);
                var value = new byte[length];
                Buffer.BlockCopy(_buffer, _position, value, 0, length);
                _position += length;
                return value;
            }

            public short ReadInt16()
            {
                CheckCanRead(2);
                var value = (short) (_buffer[_position] |
                                     (_buffer[_position + 1] << 8));
                _position += 2;
                return value;
            }

            public int ReadInt32()
            {
                CheckCanRead(4);
                var value = _buffer[_position] |
                            (_buffer[_position + 1] << 8) |
                            (_buffer[_position + 2] << 16) |
                            (_buffer[_position + 3] << 24);
                _position += 4;
                return value;
            }

            public long ReadInt64()
            {
                CheckCanRead(8);
                var low = (uint) (_buffer[_position] |
                                  (_buffer[_position + 1] << 8) |
                                  (_buffer[_position + 2] << 16) |
                                  (_buffer[_position + 3] << 24));

                var high = (uint) (_buffer[_position + 4] |
                                   (_buffer[_position + 5] << 8) |
                                   (_buffer[_position + 6] << 16) |
                                   (_buffer[_position + 7] << 24));

                var value = ((long) high << 32) | low;
                _position += 8;
                return value;
            }

            public float ReadSingle()
            {
                if (!BitConverter.IsLittleEndian)
                {
                    var bytes = ReadBytes(4);
                    Array.Reverse(bytes);
                    return BitConverter.ToSingle(bytes, 0);
                }

                CheckCanRead(4);
                var value = BitConverter.ToSingle(_buffer, _position);
                _position += 4;
                return value;
            }

            public double ReadDouble()
            {
                if (!BitConverter.IsLittleEndian)
                {
                    var bytes = ReadBytes(8);
                    Array.Reverse(bytes);
                    return BitConverter.ToDouble(bytes, 0);
                }

                CheckCanRead(8);
                var value = BitConverter.ToDouble(_buffer, _position);
                _position += 8;
                return value;
            }

            private void CheckCanRead(int count)
            {
                if (_position + count > _buffer.Length)
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}