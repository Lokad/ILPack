﻿using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Lokad.ILPack.Metadata
{
    internal partial class AssemblyMetadata
    {
        public EntityHandle GetMethodHandle(MethodInfo method)
        {
            if (TryGetMethodDefinition(method, out var metadata))
            {
                return metadata.Handle;
            }

            if (IsReferencedType(method.DeclaringType))
            {
                return ResolveMethodReference(method);
            }

            throw new ArgumentException($"Method cannot be found: {MetadataHelper.GetFriendlyName(method)}",
                nameof(method));
        }

        public BlobHandle GetMethodSignature(MethodInfo methodInfo)
        {
            if (methodInfo.DeclaringType.IsConstructedGenericType)
            {
                // When calling methods on constructed generic types, the type is the constructed 
                // type name, but the method info is the method from the open type definition. eg:
                // 
                // callvirt instance void class System.Action`1<int32>::Invoke(!0)
                //                        ^^^^^^^^^^^^^^^^^^^^^^^^^^^^          ^
                //                            constructed type here             |
                //                                                              |
                //                    non constructed parameter type here ------+
                //
                // ie: NOT this:
                //
                // callvirt instance void class System.Action`1<int32>::Invoke(int32)
                //                                                             ^^^^^
                //                                                             wrong
                //
                // There doesn't seem to be a reflection API method to get the definition method.
                // Note: MethodInfo.GetGenericMethodDefinition won't work here because this is a
                // non-generic method in a generic type (as opposed to a generic method)
                // 
                // Luckily both the original and the constructed type's method have the same meta
                // data token so we just go to the original generic definition and find the 
                // method with the same token.
                //
                // TODO: What about generic method definitions in a generic type???
                System.Diagnostics.Debug.Assert(!methodInfo.IsGenericMethod);

                var definition = methodInfo.DeclaringType.GetGenericTypeDefinition();
                methodInfo = definition.GetMethods().Single(x => x.MetadataToken == methodInfo.MetadataToken);
            }

            // Get parameters
            var parameters = methodInfo.GetParameters();

            // Create method signature encoder
            var enc = new BlobEncoder(new BlobBuilder())
                .MethodSignature(
                    MetadataHelper.ConvertCallingConvention(methodInfo.CallingConvention),
                    genericParameterCount: methodInfo.GetGenericArguments().Length,
                    isInstanceMethod: !methodInfo.IsStatic);

            // Add return type and parameters
            enc.Parameters(
                    parameters.Length,
                    (retEnc) => retEnc.FromSystemType(methodInfo.ReturnType, this),
                    (parEnc) =>
                    {
                        foreach (var par in parameters)
                        {
                            if (par.ParameterType.IsByRef)
                            {
                                parEnc.AddParameter().Type(true).FromSystemType(par.ParameterType.GetElementType(), this);
                            }
                            else
                            {
                                parEnc.AddParameter().Type(false).FromSystemType(par.ParameterType, this);
                            }
                        }
                    }
                );

            // Get blob
            return GetOrAddBlob(enc.Builder);
        }

        private EntityHandle ResolveMethodReference(MethodInfo method)
        {
            if (!IsReferencedType(method.DeclaringType))
            {
                throw new ArgumentException(
                    $"Method of a reference type is expected: {MetadataHelper.GetFriendlyName(method)}",
                    nameof(method));
            }

            // Constructed generic method?
            if (method.IsConstructedGenericMethod)
            {
                // Already created?
                if (_methodSpecHandles.TryGetValue(method, out var handle))
                {
                    return handle;
                }

                // Get the definition handle
                var definition = method.GetGenericMethodDefinition();
                var definitionHandle = ResolveMethodReference(definition);

                // Create method spec encoder
                var enc = new BlobEncoder(new BlobBuilder()).MethodSpecificationSignature(definition.GetGenericArguments().Length);
                foreach (var a in method.GetGenericArguments())
                {
                    enc.AddArgument().FromSystemType(a, this);
                }

                // Add method spec
                var spec = Builder.AddMethodSpecification(definitionHandle, GetOrAddBlob(enc.Builder));
                _methodSpecHandles.Add(method, spec);
                return spec;
            }
            else
            {
                // Already created?
                if (_methodRefHandles.TryGetValue(method, out var handle))
                {
                    return handle;
                }

                var typeRef = ResolveTypeReference(method.DeclaringType);
                var methodRef = Builder.AddMemberReference(typeRef, GetOrAddString(method.Name),
                    GetMethodSignature(method));
                _methodRefHandles.Add(method, methodRef);
                return methodRef;
            }
        }

        public bool TryGetMethodDefinition(MethodInfo methodInfo, out MethodBaseDefinitionMetadata metadata)
        {
            return _methodDefHandles.TryGetValue(methodInfo, out metadata);
        }

        public MethodBaseDefinitionMetadata ReserveMethodDefinition(MethodInfo method, MethodDefinitionHandle handle)
        {
            var metadata = new MethodBaseDefinitionMetadata(method, handle);
            _methodDefHandles.Add(method, metadata);
            return metadata;
        }
    }
}