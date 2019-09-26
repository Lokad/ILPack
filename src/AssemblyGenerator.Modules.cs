using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Lokad.ILPack
{
    public partial class AssemblyGenerator
    {
        public void CreateModules(IEnumerable<Module> moduleInfo)
        {
            foreach (var module in moduleInfo)
            {
                var moduleHandle = _metadata.Builder.AddModule(
                    0, // reserved in ECMA
                    _metadata.GetOrAddString(module.Name),
                    _metadata.GetOrAddGuid(module.ModuleVersionId),
                    default, // reserved in ECMA
                    default); // reserved in ECMA

                var genericParams = new List<DelayedWrite>();

                CreateCustomAttributes(moduleHandle, module.GetCustomAttributesData());
                CreateFields(module.GetFields());
                CreateTypes(module.GetTypes(), genericParams);
                CreateMethods(module.GetMethods(AllMethods), genericParams);

                // Delaying those writes, because generic parameters must be sorted first.
                foreach (var dw in genericParams.OrderBy(tu => tu.Index))
                    dw.Write();
            }
        }
    }
}