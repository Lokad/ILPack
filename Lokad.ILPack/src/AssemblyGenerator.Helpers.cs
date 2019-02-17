using System;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using CfgAssemblyHashAlgorithm = System.Configuration.Assemblies.AssemblyHashAlgorithm;

namespace Lokad.ILPack
{
    public partial class AssemblyGenerator
    {
        private AssemblyFlags _assemblyNameFlagsConvert(AssemblyNameFlags flags)
        {
            switch (flags)
            {
                // we not support only AssemblyNameFlags.None flag
                // also i'm not sure about AssemblyNameFlags.EnableJITcompileOptimizer flag
                case AssemblyNameFlags.None:
                    return 0; // Possible wrong
                default:
                    return (AssemblyFlags) flags;
            }
        }

        private AssemblyHashAlgorithm _assemblyHashAlgorithmConvert(CfgAssemblyHashAlgorithm alg)
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