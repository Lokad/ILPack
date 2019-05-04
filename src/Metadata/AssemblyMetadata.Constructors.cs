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
                // Make sure type reference and all public constructors are resolved
                ResolveTypeReference(ctor.DeclaringType);

                if (_ctorRefHandles.TryGetValue(ctor, out var handle))
                {
                    return handle;
                }
            }

            throw new InvalidOperationException($"Constructor cannot be found: {ctor}");
        }

        internal BlobHandle GetConstructorSignature(ConstructorInfo constructorInfo)
        {
            var parameters = constructorInfo.GetParameters();
            var countParameters = parameters.Length;

            var blob = MetadataHelper.BuildSignature(x => x.MethodSignature(
                    MetadataHelper.ConvertCallingConvention(constructorInfo.CallingConvention),
                    isInstanceMethod: !constructorInfo.IsStatic)
                .Parameters(
                    countParameters,
                    r => r.Void(),
                    p =>
                    {
                        foreach (var par in parameters)
                        {
                            var parEncoder = p.AddParameter();
                            parEncoder.Type().FromSystemType(par.ParameterType, this);
                        }
                    }));
            return GetOrAddBlob(blob);
        }

        public MethodBaseDefinitionMetadata ReserveConstructorDefinition(ConstructorInfo ctor,
            MethodDefinitionHandle handle)
        {
            var metadata = new MethodBaseDefinitionMetadata(ctor, handle);
            _ctorDefHandles.Add(ctor, metadata);
            return metadata;
        }

        public bool TryGetConstructorDefinition(ConstructorInfo ctor, out MethodBaseDefinitionMetadata metadata)
        {
            return _ctorDefHandles.TryGetValue(ctor, out metadata);
        }
    }
}