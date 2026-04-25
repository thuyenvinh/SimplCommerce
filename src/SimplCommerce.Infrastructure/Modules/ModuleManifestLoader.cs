using System.Reflection;

namespace SimplCommerce.Infrastructure.Modules
{
    public static class ModuleManifestLoader
    {
        public static void LoadAllBundled()
        {
            if (GlobalConfiguration.Modules.Count > 0)
            {
                return;
            }

            var manifest = new ModuleConfigurationManager();
            foreach (var module in manifest.GetModules())
            {
                module.Assembly = Assembly.Load(new AssemblyName(module.Id));
                GlobalConfiguration.Modules.Add(module);
            }
        }
    }
}
