using System.Collections.Generic;
using System.Reflection;
using Lokad.ILPack.IL;
using Lokad.ILPack.Metadata;

namespace Lokad.ILPack
{
    public partial class AssemblyGenerator
    {
        private const BindingFlags AllMethods = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic |
                                                BindingFlags.DeclaredOnly | BindingFlags.CreateInstance |
                                                BindingFlags.Instance;

        private void CreateMethod(MethodInfo method)
        {
            if (!_metadata.TryGetMethodDefinition(method, out var metadata))
            {
                ThrowMetadataIsNotReserved("Method", method);
            }

            EnsureMetadataWasNotEmitted(metadata, method);

            var offset = _metadata.ILBuilder.Count; // take an offset
            var body = method.GetMethodBody();
            // If body exists, we write it in IL body stream
            if (body != null)
            {
                var methodBodyWriter = new MethodBodyStreamWriter(_metadata);

                // offset can be aligned during serialization. So, override the correct offset.
                offset = methodBodyWriter.AddMethodBody(method);
            }

            var signature = _metadata.GetMethodSignature(method);
            var parameters = CreateParameters(method.GetParameters());

            var handle = _metadata.Builder.AddMethodDefinition(
                method.Attributes,
                method.MethodImplementationFlags,
                _metadata.GetOrAddString(method.Name),
                signature,
                offset,
                parameters);

            if (body != null && body.LocalVariables.Count > 0)
            {
                _metadata.Builder.AddStandaloneSignature
                (_metadata.GetOrAddBlob(
                    MetadataHelper.BuildSignature(x =>
                    {
                        var sig = x.LocalVariableSignature(body.LocalVariables.Count);
                        foreach (var vrb in body.LocalVariables)
                        {
                            sig.AddVariable().Type(
                                    vrb.LocalType.IsByRef,
                                    vrb.IsPinned)
                                .FromSystemType(vrb.LocalType, _metadata);
                        }
                    })));
            }

            VerifyEmittedHandle(metadata, handle);
            metadata.MarkAsEmitted();

            CreateCustomAttributes(handle, method.GetCustomAttributesData());
        }

        private void CreateMethods(IEnumerable<MethodInfo> methods)
        {
            foreach (var method in methods)
            {
                CreateMethod(method);
            }
        }
    }
}