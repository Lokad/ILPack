using System;
using System.Reflection;
using System.Reflection.Metadata;

namespace Lokad.ILPack.Metadata
{
    public interface IAssemblyMetadata
    {
        BlobBuilder ILBuilder { get; }

        UserStringHandle GetOrAddUserString(string value);
        EntityHandle GetTypeHandle(Type type);
        EntityHandle GetFieldHandle(FieldInfo field);
        EntityHandle GetConstructorHandle(ConstructorInfo ctor);
        EntityHandle GetMethodHandle(MethodInfo method);
    }
}
