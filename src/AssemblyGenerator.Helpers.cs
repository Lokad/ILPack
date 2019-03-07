using System;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using CfgAssemblyHashAlgorithm = System.Configuration.Assemblies.AssemblyHashAlgorithm;

namespace Lokad.ILPack
{
    public partial class AssemblyGenerator
    {
        private AssemblyFlags ConvertAssemblyNameFlags(AssemblyNameFlags flags)
        {
            var result = (AssemblyFlags) 0;

            if (flags.HasFlag(AssemblyNameFlags.PublicKey))
            {
                result |= AssemblyFlags.PublicKey;
            }

            if (flags.HasFlag(AssemblyNameFlags.Retargetable))
            {
                result |= AssemblyFlags.Retargetable;
            }

            // No, it's not a typo. Microsoft decided to put "exact opposite of the meaning" for this flag.
            if (flags.HasFlag(AssemblyNameFlags.EnableJITcompileOptimizer))
            {
                result |= AssemblyFlags.DisableJitCompileOptimizer;
            }

            if (flags.HasFlag(AssemblyNameFlags.EnableJITcompileTracking))
            {
                result |= AssemblyFlags.EnableJitCompileTracking;
            }

            return result;
        }

        private AssemblyFlags ConvertGeneratedAssemblyNameFlags(AssemblyName asmName)
        {
            var result = ConvertAssemblyNameFlags(asmName.Flags);

            // If there is no public key, unset PublicKey flag.
            if (result.HasFlag(AssemblyFlags.PublicKey) && asmName.GetPublicKey().Length == 0)
            {
                result &= ~AssemblyFlags.PublicKey;
            }

            return result;
        }

        private AssemblyFlags ConvertReferencedAssemblyNameFlags(AssemblyNameFlags flags)
        {
            var result = ConvertAssemblyNameFlags(flags);

            // TODO: [osman] Referenced runtime assemblies don't have PublicKey flag.
            // How should we handle an assembly which doesn't have a strong name?
            result &= ~AssemblyFlags.PublicKey;

            return result;
        }

        private AssemblyHashAlgorithm ConvertAssemblyHashAlgorithm(CfgAssemblyHashAlgorithm alg)
        {
            return (AssemblyHashAlgorithm) alg;
        }

        private SignatureCallingConvention ConvertCallingConvention(CallingConventions callingConvention)
        {
            // TODO: incorrect / draft implementation
            // See https://stackoverflow.com/questions/54632913/how-to-convert-callingconventions-into-signaturecallingconvention

            //if (callingConvention.HasFlag(CallingConventions.Any))
            //    result |= SignatureCallingConvention.StdCall | SignatureCallingConvention.VarArgs;

            //if (callingConvention.HasFlag(CallingConventions.ExplicitThis))
            //    result = 0x40;

            //else if (callingConvention.HasFlag(CallingConventions.HasThis))
            //    result = 0x20;

            SignatureCallingConvention result = 0;

            //if(callingConvention.HasFlag(CallingConventions.HasThis))
            //    result |= SignatureCallingConvention.ThisCall;

            //if (callingConvention.HasFlag(CallingConventions.Standard))
            //    result |= SignatureCallingConvention.StdCall;

            if (callingConvention.HasFlag(CallingConventions.VarArgs))
            {
                result |= SignatureCallingConvention.VarArgs;
            }

            return result;

            /*
            var result = SignatureCallingConvention.Default;
            
            if (callingConvention.HasFlag(CallingConventions.Any))
                result |= SignatureCallingConvention.StdCall | SignatureCallingConvention.VarArgs;

            if (callingConvention.HasFlag(CallingConventions.ExplicitThis))
                throw new Exception("Unknown Calling Convention (ExplicitThis)");

            if (callingConvention.HasFlag(CallingConventions.HasThis))
                result |= SignatureCallingConvention.ThisCall;

            if (callingConvention.HasFlag(CallingConventions.Standard))
                result |= SignatureCallingConvention.StdCall;

            if (callingConvention.HasFlag(CallingConventions.VarArgs))
                result |= SignatureCallingConvention.VarArgs;

            return result;
            */
        }

        private BlobBuilder BuildSignature(Action<BlobEncoder> action)
        {
            var builder = new BlobBuilder();
            action(new BlobEncoder(builder));
            return builder;
        }

        private BlobBuilder BuildSignature(BlobBuilder builder, Action<BlobEncoder> action)
        {
            action(new BlobEncoder(builder));
            return builder;
        }

        private StringHandle GetString(string str)
        {
            return _metadataBuilder.GetOrAddString(str);
        }

        private BlobHandle GetBlob(byte[] bytes)
        {
            return _metadataBuilder.GetOrAddBlob(bytes);
        }

        private BlobHandle GetCustomAttributeValueFromString(string str)
        {
            if (str == null)
            {
                return default(BlobHandle);
            }

            var builder = new BlobBuilder();
            builder.WriteBytes(new byte[] {0x01, 0x00}); // "prolog"
            builder.WriteUTF8(str);

            return GetBlob(builder);
        }

        private BlobHandle GetBlob(BlobBuilder builder)
        {
            return _metadataBuilder.GetOrAddBlob(builder);
        }

        private GuidHandle GetGuid(Guid guid)
        {
            return _metadataBuilder.GetOrAddGuid(guid);
        }
    }
}