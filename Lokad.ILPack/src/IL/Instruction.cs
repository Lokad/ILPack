using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Text;

namespace Lokad.ILPack.IL
{
    [DebuggerDisplay("{OpCode} {Operand}")]
    public sealed class Instruction
    {
        public int Offset { get; }

        public OpCode OpCode { get; }

        public object Operand { get; internal set; }

        internal Instruction(int offset, OpCode opcode)
        {
            Offset = offset;
            OpCode = opcode;
        }
    }
}
