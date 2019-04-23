using System.Collections.Generic;
using System.Reflection;
using Lokad.ILPack.IL;

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

            var parameters = CreateParameters(ctor.GetParameters());
            var bodyOffset = _metadata.ILBuilder.Count;

            var body = ctor.GetMethodBody();
            if (body != null)
            {
                var methodBodyWriter = new MethodBodyStreamWriter(_metadata);

                // bodyOffset can be aligned during serialization. So, override the correct offset.
                bodyOffset = methodBodyWriter.AddMethodBody(ctor);
            }

            var handle = _metadata.Builder.AddMethodDefinition(
                ctor.Attributes,
                ctor.MethodImplementationFlags,
                _metadata.GetOrAddString(ctor.Name),
                _metadata.GetConstructorSignature(ctor),
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