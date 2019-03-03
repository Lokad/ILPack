using System.Diagnostics;
using System.Reflection.Emit;

namespace Lokad.ILPack.IL
{
    [DebuggerDisplay("{OpCode} {Operand}")]
    public sealed class Instruction
    {
        internal Instruction(int offset, OpCode opcode)
        {
            Offset = offset;
            OpCode = opcode;
        }

        public int Offset { get; }

        public OpCode OpCode { get; }

        public object Operand { get; internal set; }
    }
}