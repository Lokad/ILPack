using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using Lokad.ILPack.IL;
using Lokad.ILPack.Metadata;

namespace Lokad.ILPack
{
    public partial class AssemblyGenerator
    {
        private void CreateConstructor(ConstructorInfo ctor)
        {
            if (!_metadata.TryGetConstructorDefinition(ctor, out var metadata))
            {
                ThrowMetadataIsNotReserved("Constructor", ctor);
            }

            EnsureMetadataWasNotEmitted(metadata, ctor);

            var body = ctor.GetMethodBody();

            var localVariablesSignature = default(StandaloneSignatureHandle);

            if (body != null && body.LocalVariables.Count > 0)
            {
                localVariablesSignature = _metadata.Builder.AddStandaloneSignature(_metadata.GetOrAddBlob(
                    MetadataHelper.BuildSignature(x =>
                    {
                        x.LocalVariableSignature(body.LocalVariables.Count).AddRange(body.LocalVariables, _metadata);
                    })));
            }

            var bodyOffset = body != null
                // bodyOffset can be aligned during serialization. So, use the correct offset.
                ? new MethodBodyStreamWriter(_metadata).AddMethodBody(ctor, localVariablesSignature)
                : -1;

            var parameters = CreateParameters(ctor.GetParameters());

            var handle = _metadata.Builder.AddMethodDefinition(
                ctor.Attributes,
                ctor.MethodImplementationFlags,
                _metadata.GetOrAddString(ctor.Name),
                _metadata.GetMethodOrConstructorSignature(ctor),
                bodyOffset,
                parameters);

            VerifyEmittedHandle(metadata, handle);
            metadata.MarkAsEmitted();
        }

        private void CreateConstructors(IEnumerable<ConstructorInfo> constructors)
        {
            foreach (var ctor in constructors)
            {
                CreateConstructor(ctor);
            }
        }
    }
}
