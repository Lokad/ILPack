using System.Reflection;
using System.Reflection.Metadata;

namespace Lokad.ILPack.Metadata
{
    internal partial class AssemblyMetadata
    {
        public EventDefinitionMetadata ReserveEventDefinition(EventInfo ev,
            EventDefinitionHandle eventHandle)
        {
            var metadata = new EventDefinitionMetadata(ev, eventHandle);
            _eventHandles.Add(ev, metadata);
            return metadata;
        }

        public bool TryGetEventMetadata(EventInfo ev, out EventDefinitionMetadata metadata)
        {
            return _eventHandles.TryGetValue(ev, out metadata);
        }
    }
}