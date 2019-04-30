using System.Reflection;
using System.Reflection.Metadata;

namespace Lokad.ILPack
{
    public partial class AssemblyGenerator
    {
        /// <summary>
        ///     Creates parameter metadata of a method parameters.
        /// </summary>
        /// <param name="parameters">Method parameters</param>
        /// <returns>
        ///     Metadata handle of first parameter if number of parameters is greater than zero,
        ///     null metadata otherwise.
        /// </returns>
        private ParameterHandle CreateParameters(ParameterInfo[] parameters)
        {
            if (parameters.Length == 0)
            {
                return default;
            }

            ParameterHandle? firstHandle = null;
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];

                if (_metadata.TryGetParameterHandle(parameter, out var parameterDef))
                {
                    if (firstHandle == null)
                    {
                        firstHandle = parameterDef;
                    }

                    continue;
                }

                parameterDef =
                    _metadata.Builder.AddParameter(parameter.Attributes, _metadata.GetOrAddString(parameter.Name), i);

                _metadata.AddParameterHandle(parameter, parameterDef);

                if (firstHandle == null)
                {
                    firstHandle = parameterDef;
                }

                CreateCustomAttributes(parameterDef, parameter.GetCustomAttributesData());
            }

            return firstHandle ?? default;
        }
    }
}