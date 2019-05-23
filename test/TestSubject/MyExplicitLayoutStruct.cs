using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace TestSubject
{
    [StructLayout(LayoutKind.Explicit, Size=16, Pack = 1)]
    public struct MyExplicitLayoutStruct
    {
        [FieldOffset(0)] public byte al;
        [FieldOffset(1)] public byte ah;
        [FieldOffset(0)] public ushort ax;
    }
}
