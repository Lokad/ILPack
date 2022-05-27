﻿using System;
using System.Reflection;
using System.Reflection.Metadata;
using Lokad.ILPack.IL;

namespace Lokad.ILPack.Metadata
{
    public interface IAssemblyMetadata
    {
        BlobBuilder ILBuilder { get; }

        UserStringHandle GetOrAddUserString(string value);
        EntityHandle GetTypeHandle(Type type, Boolean inMethodBodyWritingContext);
        EntityHandle GetFieldHandle(FieldInfo field, Boolean inMethodBodyWritingContext);
        EntityHandle GetConstructorHandle(ConstructorInfo ctor, Boolean inMethodBodyWritingContext);
        EntityHandle GetMethodHandle(MethodInfo method, Boolean inMethodBodyWritingContext);
        EntityHandle GetSignatureHandle(SignatureInfo signature);
    }
}
