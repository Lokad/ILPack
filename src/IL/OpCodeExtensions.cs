using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;

namespace Lokad.ILPack.IL
{
    public static class OpCodeExtensions
    {
        private static readonly OpCode[] OneByteOpCodes;
        private static readonly OpCode[] TwoBytesOpCodes;

        static OpCodeExtensions()
        {
            OneByteOpCodes = new OpCode[0xe1];
            TwoBytesOpCodes = new OpCode[0x1f];

            var fields = GetOpCodeFields();

            for (var i = 0; i < fields.Length; i++)
            {
                var opcode = (OpCode) fields[i].GetValue(null);
                if (opcode.OpCodeType == OpCodeType.Nternal)
                {
                    continue;
                }

                if (opcode.Size == 1)
                {
                    OneByteOpCodes[opcode.Value] = opcode;
                }
                else
                {
                    TwoBytesOpCodes[opcode.Value & 0xff] = opcode;
                }
            }
        }

        private static FieldInfo[] GetOpCodeFields()
        {
            return typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static);
        }

        public static OpCode ReadOpCode(this Func<byte> reader)
        {
            var op = reader();
            return op != 0xfe ? OneByteOpCodes[op] : TwoBytesOpCodes[reader()];
        }

        public static void WriteOpCode(this OpCode op, Action<byte> writer)
        {
            if (op.Size == 1)
            {
                writer((byte) op.Value);
            }
            else // Size == 2
            {
                writer(0xfe);
                writer((byte) (op.Value & 0xff));
            }
        }

        public static ILOpCode ToILOpCode(this OpCode op)
        {
            return (ILOpCode) op.Value;
        }
    }
}