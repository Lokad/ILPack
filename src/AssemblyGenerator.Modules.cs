using System.Collections.Generic;
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

                CreateCustomAttributes(moduleHandle, module.GetCustomAttributesData());
                CreateFields(module.GetFields());
                CreateTypes(module.GetTypes());
                CreateMethods(module.GetMethods(AllMethods));
            }
        }
    }
}