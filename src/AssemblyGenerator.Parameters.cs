using System;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Lokad.ILPack
{
    public partial class AssemblyGenerator
    {
        int _nextParameterRowId = 1;

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
            var firstHandle = MetadataTokens.ParameterHandle(_nextParameterRowId);
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];

                if (_metadata.TryGetParameterHandle(parameter, out var parameterDef))
                {
                    throw new InvalidOperationException("Duplicate emit of parameter");
                }

                parameterDef = _metadata.Builder.AddParameter(
                    parameter.Attributes, 
                    _metadata.GetOrAddString(parameter.Name), 
                    i+1         // As per EMCA335 II.22.33 sequence numbers are one based for parameters
                                // and zero refers to the return value.  Without this parameter attributes
                                // get applied to the wrong parameter.
                );

                // If the parameter has a default value add the value to the constants table.
                // Use the parameter handle as the parent of the value in the constants table
                // as specified in ECMA335 II.22.9.
                if (parameter.HasDefaultValue)
                    _metadata.Builder.AddConstant(parameterDef, parameter.RawDefaultValue);

                System.Diagnostics.Debug.Assert(parameterDef == MetadataTokens.ParameterHandle(_nextParameterRowId));

                _metadata.AddParameterHandle(parameter, parameterDef);
                CreateCustomAttributes(parameterDef, parameter.GetCustomAttributesData());

                _nextParameterRowId++;
            }

            return firstHandle;
        }
    }
}
