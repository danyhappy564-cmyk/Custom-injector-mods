using System;
using System.Collections.Generic;

namespace Nocturne;

/// <summary>
/// The tuneable effects on SJ-0 «Nocturne». One entry here becomes one group of F12 controls and
/// one or more entries in the game's stimulator buff table.
/// </summary>
public enum NocturneEffect
{
    HealthRegen,
    StaminaRegen,
    MaxStamina,
    SkillRate,
    WeightLimit,
    DamageResist,
    StopBleeding,
    ClearNegative,
    SideEffectHungerThirst,
    SideEffectPain,
}

/// <summary>
/// One row of the game's stimulator buff table.
/// <para>
/// <see cref="BuffType"/> is kept as a string rather than the client's
/// <c>EFT.HealthSystem.EStimulatorBuffType</c> because this file is compiled into the server mod
/// too, where that enum does not exist — and because the server writes these values into
/// <c>globals.config.Health.Effects.Stimulator.Buffs</c>, which stores the type as a string.
/// </para>
/// </summary>
public sealed class NocturneBuff
{
    public string BuffType = "";
    public string SkillName = "";
    public bool AbsoluteValue = true;
    public double Chance = 1;
    public double Delay;
    public double Duration;
    public double Value;
}

/// <summary>An effect as shown in F12: a label, defaults, and the buff rows it expands to.</summary>
public sealed class NocturneEffectSpec
{
    public NocturneEffect Effect;

    /// <summary>Korean label used for the F12 entry names.</summary>
    public string Label = "";

    /// <summary>Korean help text shown under the entry.</summary>
    public string Help = "";

    /// <summary>Whether the effect starts enabled. The two balance-breaking ones start off.</summary>
    public bool DefaultEnabled = true;

    /// <summary>Seconds. Zero means the effect is instantaneous and gets no duration slider.</summary>
    public double DefaultDuration;

    /// <summary>
    /// The strength shown in F12. Rows whose <see cref="NocturneBuff.Value"/> is non-zero are
    /// scaled from this; a spec with <see cref="HasStrength"/> false gets no strength slider.
    /// </summary>
    public double DefaultStrength;

    public bool HasStrength => Math.Abs(DefaultStrength) > double.Epsilon;
    public bool HasDuration => DefaultDuration > 0;

    /// <summary>
    /// Builds this effect's buff rows for the given duration and strength. Called by the server to
    /// seed the table with the defaults, and by the client every time an F12 value changes.
    /// </summary>
    public Func<double, double, List<NocturneBuff>> Build = (_, _) => new List<NocturneBuff>();
}

/// <summary>
/// The Nocturne effect table. Server and client both read it, so the item the server registers and
/// the buffs the client writes at runtime can never describe different things.
/// </summary>
public static class NocturneBuffs
{
    /// <summary>
    /// Key under <c>globals.config.Health.Effects.Stimulator.Buffs</c>. The item's
    /// <c>_props.StimulatorBuffs</c> points at this, exactly like <c>Buffs_BodyTemperature</c>
    /// does for SJ9.
    /// </summary>
    public const string BuffsKey = "Buffs_SJ0_Nocturne";

    /// <summary>Template id of the item this mod adds. Fixed so profiles survive a reinstall.</summary>
    public const string ItemId = "6d1c3a70b45e8f2419c07d31";

    /// <summary>SJ9 TGLabs — cloned for its stats layout and its black syringe model.</summary>
    public const string CloneSourceId = "5fca13ca637ee0341a484f46";

    /// <summary>Stimulator category (<c>_parent</c>), shared by every vanilla stim.</summary>
    public const string StimulatorParentId = "5448f3a64bdc2d60728b456a";

    /// <summary>Handbook category the vanilla stims sit in (Medical &gt; Stimulants).</summary>
    public const string HandbookCategoryId = "5b47574386f77428ca22b338";

    public static IReadOnlyList<NocturneEffectSpec> All { get; } = Build();

    private static List<NocturneEffectSpec> Build()
    {
        return new List<NocturneEffectSpec>
        {
            new()
            {
                Effect = NocturneEffect.HealthRegen,
                Label = "체력 재생",
                Help = "초당 회복되는 체력. 팔다리가 아니라 전신에 적용됩니다.",
                DefaultDuration = 60,
                DefaultStrength = 1.5,
                Build = (d, v) => One("HealthRate", d, v),
            },
            new()
            {
                Effect = NocturneEffect.StaminaRegen,
                Label = "스태미나 회복 속도",
                Help = "스태미나가 차오르는 속도에 더해지는 값.",
                DefaultDuration = 300,
                DefaultStrength = 5,
                Build = (d, v) => One("StaminaRate", d, v),
            },
            new()
            {
                Effect = NocturneEffect.MaxStamina,
                Label = "최대 스태미나",
                Help = "최대 스태미나에 더해지는 값.",
                DefaultDuration = 300,
                DefaultStrength = 50,
                Build = (d, v) => One("MaxStamina", d, v),
            },
            new()
            {
                Effect = NocturneEffect.SkillRate,
                Label = "근력·지구력 숙련",
                Help = "근력과 지구력 숙련도 획득량. 두 스킬에 같은 값이 들어갑니다.",
                DefaultDuration = 300,
                DefaultStrength = 25,
                Build = (d, v) => new List<NocturneBuff>
                {
                    Row("SkillRate", d, v, skill: "Strength"),
                    Row("SkillRate", d, v, skill: "Endurance"),
                },
            },
            new()
            {
                Effect = NocturneEffect.WeightLimit,
                Label = "중량 한계 증가",
                Help = "들 수 있는 무게. 밸런스를 크게 흔들어서 기본은 꺼져 있습니다.",
                DefaultEnabled = false,
                DefaultDuration = 300,
                DefaultStrength = 15,
                Build = (d, v) => One("WeightLimit", d, v),
            },
            new()
            {
                Effect = NocturneEffect.DamageResist,
                Label = "받는 피해 감소",
                Help = "받는 피해 배수를 낮춥니다. 0.15 이면 15% 감소. 기본은 꺼져 있습니다.",
                DefaultEnabled = false,
                DefaultDuration = 120,
                DefaultStrength = 0.15,
                // DamageModifier is a multiplier delta, so the reduction goes in as a negative.
                Build = (d, v) => One("DamageModifier", d, -Math.Abs(v), absolute: false),
            },
            new()
            {
                Effect = NocturneEffect.StopBleeding,
                Label = "출혈 전부 제거",
                Help = "경상·중상 출혈을 즉시 모두 멈춥니다. 지속시간이 없는 1회성 효과입니다.",
                Build = (_, _) => One("RemoveAllBloodLosses", 1, 0, absolute: false),
            },
            new()
            {
                Effect = NocturneEffect.ClearNegative,
                Label = "부정 효과 제거",
                Help = "통증·손떨림 같은 부정 효과를 즉시 제거합니다. 지속시간이 없는 1회성 효과입니다.",
                Build = (_, _) => One("RemoveNegativeEffects", 1, 0, absolute: false),
            },
            new()
            {
                Effect = NocturneEffect.SideEffectHungerThirst,
                Label = "부작용 · 허기와 탈수 가속",
                Help = "주사 1분 뒤부터 에너지와 수분이 빠르게 닳습니다. 밸런스용이라 기본은 켜져 있습니다.",
                DefaultDuration = 420,
                DefaultStrength = 1.2,
                Build = (d, v) => new List<NocturneBuff>
                {
                    Row("EnergyRate", d, -Math.Abs(v), delay: 60),
                    Row("HydrationRate", d, -Math.Abs(v), delay: 60),
                },
            },
            new()
            {
                Effect = NocturneEffect.SideEffectPain,
                Label = "부작용 · 지연 통증",
                Help = "약효가 끝날 무렵 통증이 옵니다. 강도 항목은 통증이 시작되기까지의 지연 시간(초)입니다.",
                DefaultDuration = 120,
                DefaultStrength = 300,
                Build = (d, v) => One("Pain", d, 0, absolute: false, delay: Math.Abs(v)),
            },
        };
    }

    private static List<NocturneBuff> One(
        string buffType, double duration, double value, bool absolute = true, double delay = 0) =>
        new() { Row(buffType, duration, value, absolute, delay) };

    private static NocturneBuff Row(
        string buffType,
        double duration,
        double value,
        bool absolute = true,
        double delay = 0,
        string skill = "") => new()
    {
        BuffType = buffType,
        SkillName = skill,
        AbsoluteValue = absolute,
        Chance = 1,
        Delay = delay,
        Duration = duration,
        Value = value,
    };
}
