using System;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

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
                result |= SignatureCallingConvention.VarArgs; //-V3059
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

        public static void AppendTypeFriendlyName(StringBuilder output, Type type)
        {
            var depth = 0;
            while (type != null)
            {
                if (depth > 0)
                {
                    output.Append(" of ");
                }

                output.Append("\"");
                output.Append(string.IsNullOrEmpty(type.AssemblyQualifiedName)
                    ? type.ToString()
                    : type.AssemblyQualifiedName);
                output.Append("\"");

                type = type.DeclaringType;
                ++depth;
            }
        }

        public static string GetFriendlyName<TEntity>(TEntity entity)
        {
            if (entity == null) //-V3111
            {
                return "\"null\"";
            }

            var sb = new StringBuilder();
            MemberInfo member;

            // Note that Type class derives from MemberInfo.
            // So, we need to check it first.
            if (entity is Type type)
            {
                AppendTypeFriendlyName(sb, type);
            }
            else if ((member = entity as MemberInfo) != null)
            {
                sb.Append($"\"{member}\"");
                if (member.DeclaringType != null)
                {
                    sb.Append(" of ");
                    AppendTypeFriendlyName(sb, member.DeclaringType);
                }
            }
            else
            {
                sb.Append($"\"{entity}\"");
            }

            return sb.ToString();
        }
    }
}