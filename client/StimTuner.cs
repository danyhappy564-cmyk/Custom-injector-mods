using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.HealthSystem;
using Settings = EFT.HealthSystem.EffectsSettings.StimulatorSettings.StimulatorBuffSettings;

namespace Nocturne.Client;

/// <summary>
/// Writes the F12 values straight into the game's stimulator buff table.
///
/// This works because nothing caches the numbers: injecting a stim runs
/// <c>StimulatorSettings.GetPersonalBuffSettings</c>, which does
/// <c>Buffs[buffName][index].Clone()</c> at that moment. Editing the table therefore changes the
/// next injection - and the item's inspect tooltip, which reads the same table - with no restart.
/// </summary>
internal sealed class StimTuner
{
    /// <summary>A vanilla buff row plus the numbers it had before this plugin touched it.</summary>
    private sealed class VanillaRow
    {
        internal Settings Target = null!;
        internal float BaseDuration;
        internal float BaseValue;

        /// <summary>
        /// Captured up front: the game's own buff/debuff verdict (<c>BuffType.IsBuff(Value)</c>)
        /// depends on Value, and we scale Value, so reading it later would flip on its own.
        /// </summary>
        internal bool IsBuff;
    }

    private readonly ManualLogSource _log;
    private readonly NocturneSettings _settings;

    private readonly List<VanillaRow> _vanilla = new();
    private readonly Dictionary<string, EStimulatorBuffType> _buffTypes = new();

    private Dictionary<string, Settings[]>? _bound;
    private bool _dirty;
    private bool _warnedMissingKey;

    internal StimTuner(ManualLogSource log, NocturneSettings settings)
    {
        _log = log;
        _settings = settings;
    }

    /// <summary>Ask for a re-apply on the next tick. Cheap and safe to call repeatedly.</summary>
    internal void MarkDirty() => _dirty = true;

    internal void Tick()
    {
        if (!Singleton<GlobalConfiguration>.Instantiated)
        {
            // Back at the menu between profiles: forget the binding so the next config gets a
            // fresh baseline instead of one taken from a table that no longer exists.
            _bound = null;
            return;
        }

        var buffs = Singleton<GlobalConfiguration>.Instance?.Health?.Effects?.Stimulator?.Buffs;
        if (buffs == null || buffs.Count == 0)
        {
            return;
        }

        if (!ReferenceEquals(buffs, _bound))
        {
            CaptureVanilla(buffs);
            _bound = buffs;
            _dirty = true;
        }

        if (!_dirty)
        {
            return;
        }

        Apply(buffs);
        _dirty = false;
    }

    /// <summary>
    /// Snapshots the duration and value of every vanilla buff row. Nocturne's own rows are skipped
    /// because the plugin rebuilds those from scratch rather than scaling them.
    /// </summary>
    private void CaptureVanilla(Dictionary<string, Settings[]> buffs)
    {
        _vanilla.Clear();

        foreach (var pair in buffs)
        {
            if (pair.Key == NocturneBuffs.BuffsKey || pair.Value == null)
            {
                continue;
            }

            foreach (var row in pair.Value)
            {
                if (row == null)
                {
                    continue;
                }

                _vanilla.Add(new VanillaRow
                {
                    Target = row,
                    BaseDuration = row.Duration,
                    BaseValue = row.Value,
                    IsBuff = row.IsBuff,
                });
            }
        }

        _log.LogInfo($"Captured {_vanilla.Count} vanilla stimulator buff row(s) across {buffs.Count - 1} stim(s).");
    }

    private void Apply(Dictionary<string, Settings[]> buffs)
    {
        var on = _settings.Enabled.Value;

        ApplyNocturne(buffs, on);
        ApplyVanilla(on);

        if (_settings.VerboseLogging.Value)
        {
            _log.LogInfo(on
                ? $"Applied settings ({_vanilla.Count} vanilla row(s) rescaled)."
                : "Disabled - every stimulator is back to the values the server sent.");
        }
    }

    /// <summary>
    /// Rebuilds Nocturne's row array from the F12 values. Rebuilding rather than editing is what
    /// makes "효과 · 사용" an actual on/off: a disabled effect contributes no rows at all, so the
    /// game never rolls it and the tooltip never lists it.
    /// </summary>
    private void ApplyNocturne(Dictionary<string, Settings[]> buffs, bool on)
    {
        if (!buffs.ContainsKey(NocturneBuffs.BuffsKey))
        {
            if (!_warnedMissingKey)
            {
                _warnedMissingKey = true;
                _log.LogInfo(
                    $"No '{NocturneBuffs.BuffsKey}' entry in the buff table - the Nocturne server mod is not "
                    + "installed, so only the vanilla stim settings below do anything.");
            }

            return;
        }

        var rows = new List<Settings>();

        foreach (var binding in _settings.Effects)
        {
            // When the plugin is switched off, fall back to each effect's shipped defaults, which
            // are exactly what the server wrote at boot.
            var enabled = on ? binding.Enabled : binding.Spec.DefaultEnabled;
            if (!enabled)
            {
                continue;
            }

            var duration = on ? binding.Duration : binding.Spec.DefaultDuration;
            var strength = on ? binding.Strength : binding.Spec.DefaultStrength;

            foreach (var buff in binding.Spec.Build(duration, strength))
            {
                if (!TryBuffType(buff.BuffType, out var buffType))
                {
                    continue;
                }

                rows.Add(new Settings
                {
                    BuffType = buffType,
                    SkillName = buff.SkillName,
                    AbsoluteValue = buff.AbsoluteValue,
                    Chance = (float)buff.Chance,
                    Delay = (float)buff.Delay,
                    Duration = (float)buff.Duration,
                    Value = (float)buff.Value,
                });
            }
        }

        // Swapped in one assignment: an injection that reads Length and then indexes must never see
        // a half-built array.
        buffs[NocturneBuffs.BuffsKey] = rows.ToArray();
    }

    /// <summary>
    /// Rescales every vanilla row from its captured baseline. Always computing from the baseline
    /// rather than the current value is what keeps sliding back to 1.0 (or unticking the section)
    /// restore the original numbers exactly, and what stops repeated applies from compounding.
    /// </summary>
    private void ApplyVanilla(bool on)
    {
        var tune = on && _settings.TuneVanilla.Value;

        var buffDuration = tune ? _settings.VanillaBuffDuration.Value : 1f;
        var debuffDuration = tune ? _settings.VanillaDebuffDuration.Value : 1f;
        var strength = tune ? _settings.VanillaStrength.Value : 1f;

        foreach (var row in _vanilla)
        {
            row.Target.Duration = row.BaseDuration * (row.IsBuff ? buffDuration : debuffDuration);
            row.Target.Value = row.BaseValue * strength;
        }
    }

    private bool TryBuffType(string name, out EStimulatorBuffType buffType)
    {
        if (_buffTypes.TryGetValue(name, out buffType))
        {
            return true;
        }

        try
        {
            buffType = (EStimulatorBuffType)Enum.Parse(typeof(EStimulatorBuffType), name);
            _buffTypes[name] = buffType;
            return true;
        }
        catch (Exception)
        {
            // A game update that renamed a buff type should cost that one effect, not the mod.
            _log.LogWarning($"Unknown stimulator buff type '{name}' - skipping that effect.");
            buffType = default;
            return false;
        }
    }
}
