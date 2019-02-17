using System.Reflection;
using System.Reflection.Metadata;

namespace Lokad.ILPack
{
    public partial class AssemblyGenerator
    {
        public void CreateModules(Module[] moduleInfo)
        {
            foreach (var module in moduleInfo)
            {
                var moduleHandle = _metadataBuilder.AddModule(
                    0, // reserved in ECMA
                    GetString(module.Name),
                    GetGuid(module.ModuleVersionId),
                    default(GuidHandle), // reserved in ECMA
                    default(GuidHandle)); // reserved in ECMA

                CreateCustomAttributes(moduleHandle, module.GetCustomAttributesData());
                CreateFields(module.GetFields());
                CreateTypes(module.GetTypes());
                CreateMethods(module.GetMethods(AllMethods));
            }
        }
    }
}