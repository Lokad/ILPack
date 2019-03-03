using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using Lokad.ILPack.IL;

namespace Lokad.ILPack
{
    public partial class AssemblyGenerator
    {
        internal BlobHandle GetConstructorSignature(ConstructorInfo constructorInfo)
        {
            var parameters = constructorInfo.GetParameters();
            var countParameters = parameters.Length;

            var blob = BuildSignature(x => x.MethodSignature(
                    ConvertCallingConvention(constructorInfo.CallingConvention),
                    isInstanceMethod: !constructorInfo.IsStatic)
                .Parameters(
                    countParameters,
                    r => r.Void(),
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

        internal EntityHandle? GetTypeConstructor(Type type)
        {
            return CreateConstructorForReferencedType(type);

            //_ctorHandles.TryGetValue(type, out var collection);
            //return collection?.First();
        }

        internal MemberReferenceHandle CreateConstructorForReferencedType(Type type)
        {
            var ctors = type.GetConstructors();
            var typeHandle = GetOrCreateType(type);

            if (ctors.Length == 0)
            {
                return default(MemberReferenceHandle);
            }

            var handles = new MemberReferenceHandle[ctors.Length];
            for (var i = 0; i < ctors.Length; i++)
            {
                var ctor = ctors[i];

                if (_ctorRefHandles.TryGetValue(ctor, out var ctorDef))
                {
                    handles[i] = ctorDef;
                    continue;
                }

                var signature = GetConstructorSignature(ctor);
                ctorDef = _metadataBuilder.AddMemberReference(
                    typeHandle, GetString(ctor.Name), signature);

                // HACK: [vermorel] recursive insertions can happen
                //if(!_ctorRefHandles.ContainsKey(ctor))
                _ctorRefHandles.Add(ctor, ctorDef);

                handles[i] = ctorDef;

                CreateCustomAttributes(ctorDef, ctor.GetCustomAttributesData());
            }

            return handles.First();
        }

        internal MethodDefinitionHandle CreateConstructor(ConstructorInfo constructorInfo)
        {
            if (_ctorDefHandles.TryGetValue(constructorInfo, out var constructorDef))
            {
                return constructorDef;
            }

            var parameters = CreateParameters(constructorInfo.GetParameters());
            var bodyOffset = _ilBuilder.Count;

            var body = constructorInfo.GetMethodBody();
            if (body != null)
            {
                var methodBodyWriter = new MethodBodyStreamWriter(_ilBuilder, GetString, _typeHandles, _ctorRefHandles,
                    _fieldHandles, _methodsHandles);

                // bodyOffset can be aligned during serialization. So, override the correct offset.
                bodyOffset = methodBodyWriter.AddMethodBody(constructorInfo);
            }

            var ctorDef = _metadataBuilder.AddMethodDefinition(
                constructorInfo.Attributes,
                constructorInfo.MethodImplementationFlags,
                GetString(constructorInfo.Name),
                GetConstructorSignature(constructorInfo),
                bodyOffset,
                parameters);

            _ctorDefHandles.Add(constructorInfo, ctorDef);

            return ctorDef;
        }

        internal MethodDefinitionHandle CreateConstructors(ConstructorInfo[] constructors)
        {
            if (constructors.Length == 0)
            {
                return default(MethodDefinitionHandle);
            }

            var handles = new MethodDefinitionHandle[constructors.Length];
            for (var i = 0; i < constructors.Length; i++)
            {
                var ctor = constructors[i];
                var ctorDef = CreateConstructor(ctor);
                handles[i] = ctorDef;
            }

            return handles.First();
        }
    }
}