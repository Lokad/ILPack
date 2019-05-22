using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;

namespace Lokad.ILPack
{
    public partial class AssemblyGenerator
    {
        private const BindingFlags AllEvents =
            BindingFlags.NonPublic | BindingFlags.Public |
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        private void CreateEvent(EventInfo ev, bool addToEventMap)
        {
            if (!_metadata.TryGetEventMetadata(ev, out var metadata))
            {
                ThrowMetadataIsNotReserved("Event", ev);
            }

            EnsureMetadataWasNotEmitted(metadata, ev);

            var eventHandle = _metadata.Builder.AddEvent(
                ev.Attributes, 
                _metadata.GetOrAddString(ev.Name), 
                _metadata.GetTypeHandle(ev.EventHandlerType)
                );

            // If this is the first event for this type then add to the event map
            // (Without this ildasm doesn't show the .event block and intellisense doesn't show any properties)
            if (addToEventMap)
            {
                if (!_metadata.TryGetTypeDefinition(ev.DeclaringType, out var typeHandle))
                {
                    ThrowMetadataIsNotReserved("Type", ev.DeclaringType);
                }
                _metadata.Builder.AddEventMap((TypeDefinitionHandle)typeHandle.Handle, eventHandle);
            }

            VerifyEmittedHandle(metadata, eventHandle);
            metadata.MarkAsEmitted();

            if (ev.AddMethod != null)
            {
                if (!_metadata.TryGetMethodDefinition(ev.AddMethod, out var addMethodMetadata))
                {
                    ThrowMetadataIsNotReserved("Event add method", ev);
                }

                _metadata.Builder.AddMethodSemantics(
                    eventHandle,
                    MethodSemanticsAttributes.Adder,
                    addMethodMetadata.Handle);
            }

            if (ev.RemoveMethod != null)
            {
                if (!_metadata.TryGetMethodDefinition(ev.RemoveMethod, out var removeMethodMetadata))
                {
                    ThrowMetadataIsNotReserved("Event remove method", ev);
                }

                _metadata.Builder.AddMethodSemantics(
                    eventHandle,
                    MethodSemanticsAttributes.Remover,
                    removeMethodMetadata.Handle);
            }
        }

        private void CreateEventsForType(IEnumerable<EventInfo> events)
        {
            bool first = true;
            foreach (var ev in events)
            {
                CreateEvent(ev, first);
                first = false;
            }
        }
    }
}