using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
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
            else if (method.Attributes.HasFlag(MethodAttributes.PinvokeImpl))
            {
                ProcessPInvokeMapData(
                    method,
                    out string libraryName,
                    out string entryName,
                    out MethodImportAttributes attrs);

                var libraryNameHandle = _metadata.GetOrAddString(libraryName);
                var moduleRefHandle = _metadata.Builder.AddModuleReference(libraryNameHandle);
                var entryNameHandle = _metadata.GetOrAddString(entryName);

                // Add the ImplMap entry for the P/Invoke
                _metadata.Builder.AddMethodImport(
                    handle,
                    attrs,
                    entryNameHandle,
                    moduleRefHandle);
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

        private void ProcessPInvokeMapData(
            MethodInfo method,
            out string libraryName,
            out string entryName,
            out MethodImportAttributes implAttr)
        {
            CustomAttributeData dllImportData = null;
            foreach (var custAttr in method.GetCustomAttributesData())
            {
                if (custAttr.AttributeType == typeof(DllImportAttribute))
                {
                    dllImportData = custAttr;
                    break;
                }
            }

            if (dllImportData == null)
            {
                throw new InvalidProgramException($"Missing P/Invoke map data for: {method.Name}");
            }

            // Initialize the outputs
            libraryName = (string)dllImportData.ConstructorArguments[0].Value;
            entryName = method.Name;
            implAttr = MethodImportAttributes.CallingConventionWinApi;

            foreach (var nargs in dllImportData.NamedArguments)
            {
                object argValue = nargs.TypedValue.Value;
                switch (nargs.MemberName)
                {
                    case nameof(DllImportAttribute.BestFitMapping):
                        implAttr |= ((bool)argValue)
                                ? MethodImportAttributes.BestFitMappingEnable
                                : MethodImportAttributes.BestFitMappingDisable;
                        break;
                    case nameof(DllImportAttribute.CallingConvention):
                        // Clear previous value.
                        implAttr &= ~MethodImportAttributes.CallingConventionMask;

                        implAttr |= (CallingConvention)argValue switch
                        {
                            CallingConvention.Winapi => MethodImportAttributes.CallingConventionWinApi,
                            CallingConvention.Cdecl => MethodImportAttributes.CallingConventionCDecl,
                            CallingConvention.StdCall => MethodImportAttributes.CallingConventionStdCall,
                            CallingConvention.ThisCall => MethodImportAttributes.CallingConventionThisCall,
                            CallingConvention.FastCall => MethodImportAttributes.CallingConventionFastCall,
                            _ => MethodImportAttributes.CallingConventionWinApi,
                        };
                        break;
                    case nameof(DllImportAttribute.CharSet):
                        // Clear previous value.
                        implAttr &= ~MethodImportAttributes.CharSetMask;

                        implAttr |= (CharSet)argValue switch
                        {
                            CharSet.Ansi => MethodImportAttributes.CharSetAnsi,
                            CharSet.Unicode => MethodImportAttributes.CharSetUnicode,
                            CharSet.Auto => MethodImportAttributes.CharSetAuto,
                            _ => MethodImportAttributes.None,
                        };
                        break;
                    case nameof(DllImportAttribute.EntryPoint):
                        entryName = (string)argValue;
                        break;
                    case nameof(DllImportAttribute.ExactSpelling):
                        implAttr |= ((bool)argValue)
                                ? MethodImportAttributes.ExactSpelling
                                : MethodImportAttributes.None;
                        break;
                    case nameof(DllImportAttribute.SetLastError):
                        implAttr |= ((bool)argValue)
                                ? MethodImportAttributes.SetLastError
                                : MethodImportAttributes.None;
                        break;
                    case nameof(DllImportAttribute.ThrowOnUnmappableChar):
                        implAttr |= ((bool)argValue)
                                ? MethodImportAttributes.ThrowOnUnmappableCharEnable
                                : MethodImportAttributes.ThrowOnUnmappableCharDisable;
                        break;
                }
            }
        }
    }
}