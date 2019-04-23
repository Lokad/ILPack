using System.Reflection;
using System.Reflection.Metadata;

namespace Lokad.ILPack.Metadata
{
    internal partial class AssemblyMetadata
    {
        public PropertyDefinitionMetadata ReservePropertyDefinition(PropertyInfo property,
            PropertyDefinitionHandle propertyHandle, MethodDefinitionHandle getMethodHandle,
            MethodDefinitionHandle setMethodHandle)
        {
            var metadata = new PropertyDefinitionMetadata(property, propertyHandle, getMethodHandle, setMethodHandle);
            _propertyHandles.Add(property, metadata);
            return metadata;
        }

        public bool TryGetPropertyMetadata(PropertyInfo property, out PropertyDefinitionMetadata metadata)
        {
            return _propertyHandles.TryGetValue(property, out metadata);
        }
    }
}