using System.Reflection;
using System.Reflection.Metadata;

namespace Lokad.ILPack.Metadata
{
    internal partial class AssemblyMetadata
    {
        public bool TryGetParameterHandle(ParameterInfo parameter, out ParameterHandle handle)
        {
            return _parameterHandles.TryGetValue(parameter, out handle);
        }

        public void AddParameterHandle(ParameterInfo parameter, ParameterHandle handle)
        {
            _parameterHandles.Add(parameter, handle);
        }
    }
}