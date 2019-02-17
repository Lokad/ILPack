using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using Lokad.ILPack.IL;

namespace Lokad.ILPack
{
    public partial class AssemblyGenerator
    {
        BindingFlags AllMethods =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.DeclaredOnly | BindingFlags.CreateInstance |
            BindingFlags.Instance;

        private BlobHandle GetMethodSignature(MethodInfo methodInfo)
        {
            var retType = methodInfo.ReturnType;
            var parameters = methodInfo.GetParameters();
            var countParameters = parameters.Length;

            var blob = BuildSignature(x => x.MethodSignature(
                    convention: ConvertCallingConvention(methodInfo.CallingConvention),
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
            return GetBlob(blob);
        }

        private MethodDefinitionHandle GetMethodDefinitionHandle(MethodInfo methodInfo)
        {
            if (methodInfo == null)
                return default(MethodDefinitionHandle);

            return _methodsHandles[methodInfo];
        }

        private MethodDefinitionHandle GetOrCreateMethod(MethodInfo methodInfo)
        {
            if (_methodsHandles.ContainsKey(methodInfo))
                return _methodsHandles[methodInfo];

            var offset = _ilBuilder.Count; // take an offset
            var body = methodInfo.GetMethodBody();
            // If body exists, we write it in IL body stream
            if (body != null)
            {
                // TODO: [vermorel] BUG, this is NOT going to work
                //var methodBody = body.GetILAsByteArray();
                //_ilBuilder.WriteBytes(methodBody);

                var il = methodInfo.GetInstructions();
                MethodBodyWriter.Write(_ilBuilder, il,
                    GetString,
                    _typeHandles,
                    _ctorRefHandles,
                    _fieldHandles,
                    _methodsHandles);
            }

            var signature = GetMethodSignature(methodInfo);
            var parameters = CreateParameters(methodInfo.GetParameters());

            var handle = _metadataBuilder.AddMethodDefinition(
                methodInfo.Attributes,
                methodInfo.MethodImplementationFlags,
                GetString(methodInfo.Name),
                signature,
                offset,
                parameters);


            if (body != null && body.LocalVariables.Count > 0)
            {
                _metadataBuilder.AddStandaloneSignature
                (GetBlob(
                    BuildSignature(x =>
                    {
                        var sig = x.LocalVariableSignature(body.LocalVariables.Count);
                        foreach (var vrb in body.LocalVariables)
                        {
                            sig.AddVariable().Type(
                                    vrb.LocalType.IsByRef,
                                    vrb.IsPinned)
                                .FromSystemType(vrb.LocalType, this);
                        }
                    })));
            }

            /*
             FieldList and MethodList described in ECMA 335, page 270
             */

            _methodsHandles.Add(methodInfo, handle);

            CreateCustomAttributes(handle, methodInfo.GetCustomAttributesData());
            return handle;
        }

        private MethodDefinitionHandle CreateMethods(MethodInfo[] methods)
        {
            if (methods.Length == 0)
                return default(MethodDefinitionHandle);

            var handles = new MethodDefinitionHandle[methods.Length];
            for (var i = 0; i < methods.Length; i++)
            {
                var method = methods[i];
                handles[i] = GetOrCreateMethod(method);
            }

            return handles.First();
        }
    }
}