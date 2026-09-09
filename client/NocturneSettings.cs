using System.Collections.Generic;
using BepInEx.Configuration;

namespace Nocturne.Client;

/// <summary>One effect's F12 controls: on/off, plus duration and strength where they mean anything.</summary>
internal sealed class EffectBinding
{
    private readonly ConfigEntry<bool> _enabled;
    private readonly ConfigEntry<float>? _duration;
    private readonly ConfigEntry<float>? _strength;

    internal NocturneEffectSpec Spec { get; }

    internal bool Enabled => _enabled.Value;
    internal double Duration => _duration?.Value ?? Spec.DefaultDuration;
    internal double Strength => _strength?.Value ?? Spec.DefaultStrength;

    internal EffectBinding(ConfigFile config, string section, NocturneEffectSpec spec, int order)
    {
        Spec = spec;

        _enabled = config.Bind(
            section,
            $"{spec.Label} · 사용",
            spec.DefaultEnabled,
            new ConfigDescription(spec.Help, null, new ConfigurationManagerAttributes { Order = order }));

        if (spec.HasDuration)
        {
            _duration = config.Bind(
                section,
                $"{spec.Label} · 지속시간(초)",
                (float)spec.DefaultDuration,
                new ConfigDescription(
                    $"효과가 유지되는 시간. 기본 {spec.DefaultDuration:0.#}초.",
                    new AcceptableValueRange<float>(1f, 1800f),
                    new ConfigurationManagerAttributes { Order = order - 1 }));
        }

        if (spec.HasStrength)
        {
            _strength = config.Bind(
                section,
                $"{spec.Label} · 강도",
                (float)spec.DefaultStrength,
                new ConfigDescription(
                    StrengthHelp(spec),
                    new AcceptableValueRange<float>(0f, 1000f),
                    new ConfigurationManagerAttributes { Order = order - 2 }));
        }
    }

    private static string StrengthHelp(NocturneEffectSpec spec) => spec.Effect switch
    {
        NocturneEffect.SideEffectPain =>
            $"주사 후 통증이 시작되기까지의 지연 시간(초). 기본 {spec.DefaultStrength:0.#}초.",
        NocturneEffect.DamageResist =>
            $"받는 피해 감소 비율. 0.15 이면 15% 감소. 기본 {spec.DefaultStrength:0.##}.",
        _ => $"효과의 크기. 기본 {spec.DefaultStrength:0.##}.",
    };
}

/// <summary>Every knob the F12 window exposes, in Korean.</summary>
internal sealed class NocturneSettings
{
    private const string GeneralSection = "0. 일반";
    private const string NocturneSection = "1. SJ-0 «녹턴» 효과";
    private const string VanillaSection = "2. 기존 주사기 일괄 조정";

    internal ConfigEntry<bool> Enabled { get; }
    internal ConfigEntry<bool> VerboseLogging { get; }

    internal IReadOnlyList<EffectBinding> Effects { get; }

    internal ConfigEntry<bool> TuneVanilla { get; }
    internal ConfigEntry<float> VanillaBuffDuration { get; }
    internal ConfigEntry<float> VanillaDebuffDuration { get; }
    internal ConfigEntry<float> VanillaStrength { get; }

    internal NocturneSettings(ConfigFile config)
    {
        Enabled = config.Bind(
            GeneralSection,
            "플러그인 사용",
            true,
            new ConfigDescription(
                "끄면 녹턴과 기존 주사기 모두 서버가 보낸 원래 수치로 즉시 되돌아갑니다.",
                null,
                new ConfigurationManagerAttributes { Order = 100 }));

        VerboseLogging = config.Bind(
            GeneralSection,
            "자세한 로그",
            false,
            new ConfigDescription(
                "수치를 다시 적용할 때마다 BepInEx 로그에 한 줄씩 남깁니다.",
                null,
                new ConfigurationManagerAttributes { Order = 90, IsAdvanced = true }));

        var effects = new List<EffectBinding>();
        var order = 100 * NocturneBuffs.All.Count;
        foreach (var spec in NocturneBuffs.All)
        {
            effects.Add(new EffectBinding(config, NocturneSection, spec, order));
            order -= 100;
        }

        Effects = effects;

        TuneVanilla = config.Bind(
            VanillaSection,
            "기존 주사기 조정 사용",
            false,
            new ConfigDescription(
                "바닐라 주사기(SJ1/6/9/12, 프로피탈, 자구스틴, 아드레날린 등) 전부에 아래 배수를 겁니다. "
                + "녹턴에는 적용되지 않습니다.",
                null,
                new ConfigurationManagerAttributes { Order = 40 }));

        VanillaBuffDuration = config.Bind(
            VanillaSection,
            "긍정 효과 지속시간 배수",
            1f,
            new ConfigDescription(
                "이로운 효과가 유지되는 시간. 2면 두 배로 오래 갑니다.",
                new AcceptableValueRange<float>(0.1f, 5f),
                new ConfigurationManagerAttributes { Order = 30 }));

        VanillaDebuffDuration = config.Bind(
            VanillaSection,
            "부작용 지속시간 배수",
            1f,
            new ConfigDescription(
                "해로운 효과가 유지되는 시간. 0.5면 부작용이 절반만 갑니다. "
                + "긍정/부작용 구분은 게임이 자체적으로 판정합니다.",
                new AcceptableValueRange<float>(0.1f, 5f),
                new ConfigurationManagerAttributes { Order = 20 }));

        VanillaStrength = config.Bind(
            VanillaSection,
            "효과 강도 배수",
            1f,
            new ConfigDescription(
                "긍정·부작용 양쪽의 크기에 함께 걸립니다. 부작용까지 같이 세지니 주의하세요.",
                new AcceptableValueRange<float>(0.1f, 5f),
                new ConfigurationManagerAttributes { Order = 10 }));
    }
}
