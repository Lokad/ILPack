﻿using System.Collections.Generic;
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

            var offset = -1;
            var body = method.GetMethodBody();
            // If body exists, we write it in IL body stream
            if (body != null && !method.IsAbstract)
            {
                offset = _metadata.ILBuilder.Count; // take an offset

                var methodBodyWriter = new MethodBodyStreamWriter(_metadata);

                // offset can be aligned during serialization. So, override the correct offset.
                offset = methodBodyWriter.AddMethodBody(method);
            }

            var signature = _metadata.GetMethodOrConstructorSignature(method);
            var parameters = CreateParameters(method.GetParameters());

            var handle = _metadata.Builder.AddMethodDefinition(
                method.Attributes,
                method.MethodImplementationFlags,
                _metadata.GetOrAddString(method.Name),
                signature,
                offset,
                parameters);

            // Explicit interface implementations need to be marked with method implementation
            // (This is the equivalent of .Override in msil)
            if (method.IsPrivate)
            {
                // Go through all the implemented interfaces and all their methods
                // looking for methods that this method implements and mark accordingly.

                // NB: This is not super efficient.  Should probably create a map somewhere
                //     for faster lookup, but this will do for now.

                var type = method.DeclaringType;
                foreach (var itf in type.GetInterfaces())
                {
                    var itfMap = type.GetInterfaceMap(itf);
                    for (int i = 0; i < itfMap.TargetMethods.Length; i++)
                    {
                        var m = itfMap.TargetMethods[i];
                        if (m == method)
                        {
                            var itfImpl = itfMap.InterfaceMethods[i];
                            _metadata.Builder.AddMethodImplementation((TypeDefinitionHandle)_metadata.GetTypeHandle(method.DeclaringType), handle, _metadata.GetMethodHandle(itfImpl));

                        }
                    }
                }
            }

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