using System.Collections.Generic;
using System.Reflection;

namespace Lokad.ILPack.IL
{
    /// <summary>IL parsing utility.</summary>
    public static class MethodBaseExtensions
    {
        public static IReadOnlyList<Instruction> GetInstructions(this MethodBase self)
        {
            return MethodBodyReader.GetInstructions(self).AsReadOnly();
        }
    }
}