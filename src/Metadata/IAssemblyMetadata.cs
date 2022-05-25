using System;
using System.Reflection;
using System.Reflection.Metadata;
using Lokad.ILPack.IL;

namespace Lokad.ILPack.Metadata
{
    public interface IAssemblyMetadata
    {
        BlobBuilder ILBuilder { get; }

        UserStringHandle GetOrAddUserString(string value);
        EntityHandle GetTypeHandle(Type type, Boolean inMethodBodyWritingContext = false);
        EntityHandle GetFieldHandle(FieldInfo field, Boolean inMethodBodyWritingContext = false);
        EntityHandle GetConstructorHandle(ConstructorInfo ctor, Boolean inMethodBodyWritingContext = false);
        EntityHandle GetMethodHandle(MethodInfo method, Boolean inMethodBodyWritingContext = false);
        EntityHandle GetSignatureHandle(SignatureInfo signature);
    }
}
