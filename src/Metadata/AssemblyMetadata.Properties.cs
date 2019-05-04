using System.Reflection;
using System.Reflection.Metadata;

namespace Lokad.ILPack.Metadata
{
    internal partial class AssemblyMetadata
    {
        public PropertyDefinitionMetadata ReservePropertyDefinition(PropertyInfo property,
            PropertyDefinitionHandle propertyHandle)
        {
            var metadata = new PropertyDefinitionMetadata(property, propertyHandle);
            _propertyHandles.Add(property, metadata);
            return metadata;
        }

        public bool TryGetPropertyMetadata(PropertyInfo property, out PropertyDefinitionMetadata metadata)
        {
            return _propertyHandles.TryGetValue(property, out metadata);
        }
    }
}