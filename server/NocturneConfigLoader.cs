using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Server;
using Path = System.IO.Path;

namespace Nocturne.Server;

/// <summary>
/// Reads <c>config/config.jsonc</c> once and hands the same instance to both load steps, which run
/// in different phases of startup.
/// </summary>
[Injectable(InjectionType.Singleton)]
public class NocturneConfigLoader(ISptLogger<NocturneConfigLoader> logger, ModHelper modHelper)
{
    private NocturneConfig? _config;

    public NocturneConfig Value => _config ??= Load();

    private NocturneConfig Load()
    {
        var configDir = Path.Combine(
            modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly()),
            "config"
        );

        try
        {
            return modHelper.GetJsonDataFromFile<NocturneConfig>(configDir, "config.jsonc") ?? new NocturneConfig();
        }
        catch (Exception ex)
        {
            logger.Error($"[Nocturne] could not read config.jsonc from {configDir}; falling back to defaults.", ex);
            return new NocturneConfig();
        }
    }
}
