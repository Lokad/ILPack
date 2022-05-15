using System;
using System.Reflection;
using System.Reflection.Metadata;

namespace Lokad.ILPack.Metadata
{
    internal partial class AssemblyMetadata
    {
        public EntityHandle GetConstructorHandle(ConstructorInfo ctor, Boolean inMethodBodyWritingContext)
        {
            if (ctor.DeclaringType?.IsConstructedGenericType == false &&
                TryGetConstructorDefinition(ctor, out var metadata))
            {
                return inMethodBodyWritingContext ? ResolveConstructorReference(ctor) : metadata.Handle;
            }

            if (IsReferencedType(ctor.DeclaringType) ||
                ctor.DeclaringType?.IsConstructedGenericType == true)
            {
                return ResolveConstructorReference(ctor);
            }

            throw new ArgumentException($"Constructor cannot be found: {MetadataHelper.GetFriendlyName(ctor)}",
                nameof(ctor));
        }

        private EntityHandle ResolveConstructorReference(ConstructorInfo ctor)
        {
            // Already created?
            if (_ctorRefHandles.TryGetValue(ctor, out var handle))
            {
                return handle;
            }

            var typeRef = ResolveTypeReference(ctor.DeclaringType);
            var methodRef = Builder.AddMemberReference(typeRef, GetOrAddString(ctor.Name),
                GetMethodOrConstructorSignature(ctor));
            _ctorRefHandles.Add(ctor, methodRef);
            return methodRef;
        }

        public bool TryGetConstructorDefinition(ConstructorInfo ctor, out MethodBaseDefinitionMetadata metadata)
        {
            return _ctorDefHandles.TryGetValue(ctor, out metadata);
        }

        public MethodBaseDefinitionMetadata ReserveConstructorDefinition(ConstructorInfo ctor, MethodDefinitionHandle handle)
        {
            var metadata = new MethodBaseDefinitionMetadata(ctor, handle);
            _ctorDefHandles.Add(ctor, metadata);
            return metadata;
        }
     }
}
