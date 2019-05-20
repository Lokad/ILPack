using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
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

            var body = method.GetMethodBody();

            var localVariablesSignature = default(StandaloneSignatureHandle);

            if (body != null && body.LocalVariables.Count > 0)
            {
                localVariablesSignature = _metadata.Builder.AddStandaloneSignature(_metadata.GetOrAddBlob(
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

            var offset = -1;

            // If body exists, we write it in IL body stream
            if (body != null && !method.IsAbstract)
            {
                offset = _metadata.ILBuilder.Count; // take an offset

                var methodBodyWriter = new MethodBodyStreamWriter(_metadata);

                // offset can be aligned during serialization. So, override the correct offset.
                offset = methodBodyWriter.AddMethodBody(method, localVariablesSignature);
            }

            var parameters = CreateParameters(method.GetParameters());

            var handle = _metadata.Builder.AddMethodDefinition(
                method.Attributes,
                method.MethodImplementationFlags,
                _metadata.GetOrAddString(method.Name),
                _metadata.GetMethodSignature(method),
                offset,
                parameters);

            // Add generic parameters
            if (method.IsGenericMethodDefinition)
            {
                int index = 0;
                foreach (var ga in method.GetGenericArguments())
                {
                    // Add the argument
                    var gaHandle = _metadata.Builder.AddGenericParameter(handle, ga.GenericParameterAttributes, _metadata.GetOrAddString(ga.Name), index++);

                    // Add it's constraints
                    foreach (var constraint in ga.GetGenericParameterConstraints())
                    {
                        _metadata.Builder.AddGenericParameterConstraint(gaHandle, _metadata.GetTypeHandle(constraint));
                    }
                }
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