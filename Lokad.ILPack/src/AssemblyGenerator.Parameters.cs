using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

namespace Lokad.ILPack
{
    public partial class AssemblyGenerator
    {
        public ParameterHandle CreateParameters(ParameterInfo[] parameters)
        {
            if (parameters.Length == 0)
            {
                return default(ParameterHandle);
            }

            var handles = new ParameterHandle[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];

                if (_parameterHandles.TryGetValue(parameter, out var parameterDef))
                {
                    handles[i] = parameterDef;
                    continue;
                }

                parameterDef = _metadataBuilder.AddParameter(
                    parameter.Attributes,
                    GetString(parameter.Name),
                    i);

                _parameterHandles.Add(parameter, parameterDef);

                handles[i] = parameterDef;

                CreateCustomAttributes(parameterDef, parameter.GetCustomAttributesData());
            }

            return handles.First();
        }
    }
}