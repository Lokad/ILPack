using System;
using System.Reflection;
using System.Reflection.Metadata;

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
            var retType = methodInfo.ReturnType;
            var parameters = methodInfo.GetParameters();
            var countParameters = parameters.Length;

            var blob = MetadataHelper.BuildSignature(x => x.MethodSignature(
                    MetadataHelper.ConvertCallingConvention(methodInfo.CallingConvention),
                    isInstanceMethod: !methodInfo.IsStatic)
                .Parameters(
                    countParameters,
                    r => r.FromSystemType(retType, this),
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

        private EntityHandle ResolveMethodReference(MethodInfo method)
        {
            if (!IsReferencedType(method.DeclaringType))
            {
                throw new ArgumentException(
                    $"Method of a reference type is expected: {MetadataHelper.GetFriendlyName(method)}",
                    nameof(method));
            }

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