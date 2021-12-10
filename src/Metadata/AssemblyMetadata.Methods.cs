using System;
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

        public BlobHandle GetMethodOrConstructorSignature(MethodBase methodBase)
        {
            // Method or Constructor? (must be one or the other)
            System.Diagnostics.Debug.Assert(methodBase is MethodInfo || methodBase is ConstructorInfo);

            if (methodBase.DeclaringType.IsConstructedGenericType)
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
                System.Diagnostics.Debug.Assert(!methodBase.IsGenericMethod);

                var definition = methodBase.DeclaringType.GetGenericTypeDefinition();
                if (methodBase is MethodInfo)
                    methodBase = definition.GetMethods(AllMethods).Single(x => x.MetadataToken == methodBase.MetadataToken);
                else
                    methodBase = definition.GetConstructors(AllMethods).Single(x => x.MetadataToken == methodBase.MetadataToken);
            }

            // Get parameters
            var parameters = methodBase.GetParameters();

            // Create method signature encoder
            var enc = new BlobEncoder(new BlobBuilder())
                .MethodSignature(
                    MetadataHelper.ConvertCallingConvention(methodBase.CallingConvention),
                    genericParameterCount: (methodBase is MethodInfo) ? ((MethodInfo)methodBase).GetGenericArguments().Length : 0,
                    isInstanceMethod: !methodBase.IsStatic);

            // Add return type and parameters
            enc.Parameters(
                    parameters.Length,
                    (retEnc) =>
                    {
                        if (methodBase is MethodInfo)
                        {
                            retEnc.FromSystemType(((MethodInfo)methodBase).ReturnType, this);
                        }
                        else
                        {
                            retEnc.Void();
                        }
                    },
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
            if (method.IsConstructedGenericMethod())
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
                    GetMethodOrConstructorSignature(method));
                _methodRefHandles.Add(method, methodRef);
                return methodRef;
            }
        }

        public bool TryGetMethodDefinition(MethodInfo methodInfo, out MethodBaseDefinitionMetadata metadata)
        {
            if (methodInfo.Module.Assembly == SourceAssembly
                // && (methodInfo.DeclaringType.IsConstructedGenericType || methodInfo.IsConstructedGenericMethod())
                && _unconstructedMethodDefs.TryGetValue(methodInfo.MetadataToken, out var baseMethod)
            )
            {
                methodInfo = baseMethod;
            }

            return _methodDefHandles.TryGetValue(methodInfo, out metadata);
        }

        public MethodBaseDefinitionMetadata ReserveMethodDefinition(MethodInfo method, MethodDefinitionHandle handle)
        {
            var metadata = new MethodBaseDefinitionMetadata(method, handle);
            _methodDefHandles.Add(method, metadata);
            _unconstructedMethodDefs.Add(method.MetadataToken, method);
            return metadata;
        }
    }
}