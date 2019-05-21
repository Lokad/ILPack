using System;
using System.Reflection;
using System.Reflection.Metadata;

namespace Lokad.ILPack.Metadata
{
    internal partial class AssemblyMetadata
    {
        public EntityHandle GetConstructorHandle(ConstructorInfo ctor)
        {
            if (TryGetConstructorDefinition(ctor, out var metadata))
            {
                return metadata.Handle;
            }

            if (IsReferencedType(ctor.DeclaringType))
            {
                return ResolveConstructorReference(ctor);
            }

            throw new ArgumentException($"Constructor cannot be found: {MetadataHelper.GetFriendlyName(ctor)}",
                nameof(ctor));
        }

        private EntityHandle ResolveConstructorReference(ConstructorInfo method)
        {
            if (!IsReferencedType(method.DeclaringType))
            {
                throw new ArgumentException(
                    $"Method of a reference type is expected: {MetadataHelper.GetFriendlyName(method)}",
                    nameof(method));
            }

            // Already created?
            if (_ctorRefHandles.TryGetValue(method, out var handle))
            {
                return handle;
            }

            var typeRef = ResolveTypeReference(method.DeclaringType);
            var methodRef = Builder.AddMemberReference(typeRef, GetOrAddString(method.Name), GetMethodOrConstructorSignature(method));
            _ctorRefHandles.Add(method, methodRef);
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



