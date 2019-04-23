using System;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Lokad.ILPack.Metadata
{
    internal static class MetadataHelper
    {
        public static AssemblyFlags ConvertAssemblyNameFlags(AssemblyNameFlags flags)
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

        public static AssemblyFlags ConvertReferencedAssemblyNameFlags(AssemblyNameFlags flags)
        {
            var result = ConvertAssemblyNameFlags(flags);

            // TODO: [osman] Referenced runtime assemblies don't have PublicKey flag.
            // How should we handle an assembly which doesn't have a strong name?
            result &= ~AssemblyFlags.PublicKey;

            return result;
        }

        public static SignatureCallingConvention ConvertCallingConvention(CallingConventions callingConvention)
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

        public static BlobBuilder BuildSignature(Action<BlobEncoder> action)
        {
            var builder = new BlobBuilder();
            action(new BlobEncoder(builder));
            return builder;
        }

        public static BlobBuilder BuildSignature(BlobBuilder builder, Action<BlobEncoder> action)
        {
            action(new BlobEncoder(builder));
            return builder;
        }
    }
}