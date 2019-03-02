using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

namespace Lokad.ILPack
{
    // TODO: [vermorel] missing dictionary for 'CustomAttributeHandle'

    public partial class AssemblyGenerator
    {
        public void CreateCustomAttributes(EntityHandle parent, IEnumerable<CustomAttributeData> attributes)
        {
            // TODO: implement support for custom attributes
            return;

            foreach (var attr in attributes)
            {
                var type = attr.AttributeType;
                GetOrCreateType(type); // create type

                var args = attr.ConstructorArguments;
                var text = string.Join(",", args.Select(x => $"\"{x.Value}"));

                /*
                var namedArgs = attr.NamedArguments;
                var namedText = string.Join(", ", namedArgs
                    .Where(x => !string.IsNullOrEmpty(x.TypedValue.Value.ToString()))
                    .Select(x =>
                {
                    return $"{x.MemberName}=\"{x.TypedValue.Value}\"";
                }));

                if (!string.IsNullOrEmpty(namedText))
                    text += $", {namedText}";
                */
                var ctor = GetTypeConstructor(type);
                if (ctor != null)
                {
                    //Console.WriteLine(text);
                    _metadataBuilder.AddCustomAttribute(parent, ctor.Value, GetCustomAttributeValueFromString(text));
                }
            }
        }
    }
}