using System;
using System.Reflection;
using Lokad.ILPack.Metadata;
using CfgAssemblyHashAlgorithm = System.Configuration.Assemblies.AssemblyHashAlgorithm;

namespace Lokad.ILPack
{
    public partial class AssemblyGenerator
    {
        private static AssemblyFlags ConvertGeneratedAssemblyNameFlags(AssemblyName asmName)
        {
            var result = MetadataHelper.ConvertAssemblyNameFlags(asmName.Flags);

            // If there is no public key, unset PublicKey flag.
            if (result.HasFlag(AssemblyFlags.PublicKey) && asmName.GetPublicKey().Length == 0)
            {
                result &= ~AssemblyFlags.PublicKey;
            }

            return result;
        }

        private static AssemblyHashAlgorithm ConvertAssemblyHashAlgorithm(CfgAssemblyHashAlgorithm alg)
        {
            return (AssemblyHashAlgorithm) alg;
        }

        private static void EnsureMetadataWasNotEmitted<TEntity, THandle>(DefinitionMetadata<TEntity, THandle> metadata,
            TEntity entity)
        {
            if (metadata.IsEmitted)
            {
                var friendlyName = MetadataHelper.GetFriendlyName(entity);
                throw new InvalidOperationException($"Entity metadata was already emitted before: {friendlyName}");
            }
        }

        private static void ThrowMetadataIsNotReserved<TEntity>(string entityFriendlyName, TEntity entity)
        {
            var friendlyName = MetadataHelper.GetFriendlyName(entity);
            throw new InvalidOperationException(
                $"{entityFriendlyName} metadata should be reserved before emitting metadata: {friendlyName}");
        }

        private static void VerifyEmittedHandle<TEntity, THandle>(DefinitionMetadata<TEntity, THandle> metadata,
            THandle handle)
        {
            if (!metadata.Handle.Equals(handle))
            {
                throw new InvalidOperationException("Reserved and emitted metadata handles are different.");
            }
        }
    }
}