using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Lokad.ILPack.IL;
using Lokad.ILPack.Metadata;

namespace Lokad.ILPack
{
    public partial class AssemblyGenerator
    {
        private const BindingFlags AllMethods = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic |
                                                BindingFlags.DeclaredOnly | BindingFlags.CreateInstance |
                                                BindingFlags.Instance;

        private void CreateMethod(MethodInfo method, List<DelayedWrite> genericParams)
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
                        x.LocalVariableSignature(body.LocalVariables.Count).AddRange(body.LocalVariables, _metadata);
                    })));
            }

            var offset = -1;

            // If body exists, we write it in IL body stream
            if (body != null && !method.IsAbstract)
            {
                var methodBodyWriter = new MethodBodyStreamWriter(_metadata);

                // offset can be aligned during serialization. So, override the correct offset.
                offset = methodBodyWriter.AddMethodBody(method, localVariablesSignature);
            }

            var parameters = CreateParameters(method.GetParameters());

            var handle = _metadata.Builder.AddMethodDefinition(
                method.Attributes,
                method.MethodImplementationFlags,
                _metadata.GetOrAddString(method.Name),
                _metadata.GetMethodOrConstructorSignature(method),
                offset,
                parameters);

            // The generation of interface method overrides has been moved to 
            // AssemblyGenerator.DeclareInterfacesAndCreateInterfaceMap in
            // AssemblyGenerator.Types.cs.

            // Add generic parameters
            if (method.IsGenericMethodDefinition)
            {
                int index = 0;
                foreach (var arg in method.GetGenericArguments())
                {
                    genericParams.Add(new DelayedWrite(CodedIndex.TypeOrMethodDef(handle), () =>
                    {
                        // Add the argument
                        var gaHandle = _metadata.Builder.AddGenericParameter(handle, arg.GenericParameterAttributes, _metadata.GetOrAddString(arg.Name), index++);

                        // Add it's constraints
                        foreach (var constraint in arg.GetGenericParameterConstraints())
                        {
                            _metadata.Builder.AddGenericParameterConstraint(gaHandle, _metadata.GetTypeHandle(constraint));
                        }
                    }));
                }
            }

            VerifyEmittedHandle(metadata, handle);
            metadata.MarkAsEmitted();

            CreateCustomAttributes(handle, method.GetCustomAttributesData());
        }

        private void CreateMethods(IEnumerable<MethodInfo> methods, List<DelayedWrite> genericParams)
        {
            foreach (var method in methods)
            {
                CreateMethod(method, genericParams);
            }
        }
    }
}