using BepInEx;

namespace Nocturne.Client;

/// <summary>
/// F12 front end for SJ-0 «Nocturne».
///
/// No Harmony patches: the plugin only writes public fields on a settings object the game already
/// has loaded, so there is nothing to bind against and nothing to rebind on a game update.
/// </summary>
[BepInPlugin(PluginGuid, "Nocturne", "1.0.0")]
public class NocturnePlugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.rf.nocturne.client";

    private StimTuner _tuner = null!;

    private void Awake()
    {
        var settings = new NocturneSettings(Config);
        _tuner = new StimTuner(Logger, settings);

        // One handler covers every entry, so each F12 edit schedules a re-apply on the next frame.
        // It also fires for a Config.Reload(), which is what picks up hand edits to the .cfg.
        Config.SettingChanged += (_, _) => _tuner.MarkDirty();

        Logger.LogInfo("Nocturne loaded. F12로 조절하세요.");
    }

    private void Update()
    {
        _tuner.Tick();
    }
}
