using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Tables;
using Path = System.IO.Path;
using SptBuff = SPTarkov.Server.Core.Models.Spt.Tables.Buff;

namespace Nocturne.Server;

/// <summary>
/// Registers SJ-0 «Nocturne»: an extra stimulant cloned from SJ9 TGLabs, with its own entry in the
/// game's stimulator buff table.
///
/// Cloning SJ9 is what keeps this mod bundle-free — SJ9's black syringe model is already in the
/// client, so reusing its <c>Prefab</c>/<c>UsePrefab</c> paths means there is nothing to ship and
/// nothing to register with the bundle loader.
///
/// The item is deliberately never added to any loot table, bot inventory or container pool. It
/// exists in the item database, the handbook, the flea price table and (optionally) one trader's
/// assort, and nowhere else — so it cannot be found in raid.
/// </summary>
[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.PostLoad + 50)]
public class NocturneMod(
    ISptLogger<NocturneMod> logger,
    ModHelper modHelper,
    TemplateTable templates,
    GlobalTable globals,
    LocaleTable locales,
    TradersTable traders
) : IOnLoad
{
    private const string LogPrefix = "[Nocturne]";

    private const string DisplayName = "SJ-0 Nocturne";
    private const string ShortName = "SJ-0";

    private const string DescriptionEn =
        "An unreleased TerraGroup Labs prototype. Where the SJ series traded stability for a single "
        + "sharpened edge, SJ-0 tries to hold every edge at once - and bills you for it afterwards.";

    private const string DescriptionKo =
        "테라그룹 랩스의 미출시 시제품. SJ 시리즈가 안정성을 버리고 한 가지를 벼렸다면, "
        + "SJ-0은 모든 날을 동시에 세우려 든다. 그 청구서는 나중에 온다.";

    public Task OnLoadAsync(CancellationToken cancellationToken = default)
    {
        var config = LoadConfig();
        if (config is null)
        {
            return Task.CompletedTask;
        }

        var itemId = new MongoId(NocturneBuffs.ItemId);
        if (templates.Items.ContainsKey(itemId))
        {
            logger.Warning($"{LogPrefix} {itemId} is already registered - another mod uses this id. Doing nothing.");
            return Task.CompletedTask;
        }

        if (!templates.Items.TryGetValue(new MongoId(NocturneBuffs.CloneSourceId), out var source)
            || source.Properties is null)
        {
            logger.Error($"{LogPrefix} SJ9 TGLabs ({NocturneBuffs.CloneSourceId}) is missing from the item database - cannot clone it.");
            return Task.CompletedTask;
        }

        RegisterBuffs();
        RegisterItem(itemId, source, config);
        RegisterHandbookAndPrices(itemId, config);
        RegisterLocales(itemId);

        var soldBy = config.SellAtTrader ? RegisterTraderOffer(itemId, config) : null;

        logger.Success(
            $"{LogPrefix} SJ-0 «Nocturne» registered ({NocturneBuffs.All.Count} effects, "
            + $"handbook {config.HandbookPrice:N0}₽, flea {(config.AllowOnFlea ? $"{FleaPrice(config):N0}₽" : "off")}, "
            + $"trader {soldBy ?? "off"}). Not in any loot table by design."
        );
        return Task.CompletedTask;
    }

    private NocturneConfig? LoadConfig()
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
            logger.Error($"{LogPrefix} could not read config.jsonc from {configDir}; falling back to defaults.", ex);
            return new NocturneConfig();
        }
    }

    private int FleaPrice(NocturneConfig config) =>
        config.FleaPrice > 0 ? config.FleaPrice : config.HandbookPrice;

    private int TraderPrice(NocturneConfig config) =>
        config.TraderPrice > 0 ? config.TraderPrice : config.HandbookPrice;

    /// <summary>
    /// Seeds the buff table with the defaults from <see cref="NocturneBuffs"/>. The client plugin
    /// overwrites this same entry from its F12 values once a profile is loaded; without the plugin
    /// these defaults are what the syringe does.
    /// </summary>
    private void RegisterBuffs()
    {
        var rows = new List<SptBuff>();
        foreach (var spec in NocturneBuffs.All)
        {
            if (!spec.DefaultEnabled)
            {
                continue;
            }

            foreach (var buff in spec.Build(spec.DefaultDuration, spec.DefaultStrength))
            {
                rows.Add(new SptBuff
                {
                    BuffType = buff.BuffType,
                    Chance = buff.Chance,
                    Delay = buff.Delay,
                    Duration = buff.Duration,
                    Value = buff.Value,
                    AbsoluteValue = buff.AbsoluteValue,
                    SkillName = buff.SkillName,
                });
            }
        }

        globals.Configuration.Health.Effects.Stimulator.Buffs[NocturneBuffs.BuffsKey] = rows;
    }

    private void RegisterItem(MongoId itemId, TemplateItem source, NocturneConfig config)
    {
        // TemplateItem and TemplateItemProperties are records, so `with` gives a copy without
        // hand-writing the ~150 properties an item template carries.
        var props = source.Properties! with
        {
            Name = DisplayName,
            ShortName = ShortName,
            Description = DescriptionEn,

            // The whole point of the item: its own key in the stimulator buff table.
            StimulatorBuffs = NocturneBuffs.BuffsKey,

            MedUseTime = config.UseTime,
            BackgroundColor = "violet",

            CanSellOnRagfair = config.AllowOnFlea,
            CanRequireOnRagfair = config.AllowOnFlea,

            // Not_exist keeps it out of every rarity-driven spawn roll, on top of not being in any
            // loot table to begin with.
            RarityPvE = "Not_exist",
            SpawnRarity = "Not_exist",

            // SJ9 blocks being looted off a corpse's primary weapon slot; that is meaningless here.
            UnlootableFromSlot = "Pockets",

            ExamineExperience = 0,
            LootExperience = 0,
            DiscardLimit = -1,
        };

        templates.Items[itemId] = source with
        {
            Id = itemId,
            Name = "SJ0_Nocturne",
            Parent = new MongoId(NocturneBuffs.StimulatorParentId),
            Properties = props,
        };
    }

    private void RegisterHandbookAndPrices(MongoId itemId, NocturneConfig config)
    {
        templates.Handbook.Items.RemoveAll(entry => entry.Id == itemId);
        templates.Handbook.Items.Add(new HandbookItem
        {
            Id = itemId,
            ParentId = new MongoId(NocturneBuffs.HandbookCategoryId),
            Price = config.HandbookPrice,
        });

        // The flea prices offers from this table, not from the handbook, so both get set.
        templates.Prices[itemId] = FleaPrice(config);
    }

    /// <summary>
    /// Adds the item's name, short name and description to every installed locale. Locales are
    /// <c>LazyLoad</c> in 4.1, so this registers a transformer that runs whenever a locale is
    /// actually deserialised rather than forcing them all to load at boot.
    /// </summary>
    private void RegisterLocales(MongoId itemId)
    {
        var id = itemId.ToString();

        foreach (var (localeId, lazyLocale) in locales.Global)
        {
            var description = localeId.StartsWith("ko", StringComparison.OrdinalIgnoreCase)
                ? DescriptionKo
                : DescriptionEn;

            lazyLocale.AddTransformer(locale =>
            {
                // A locale that failed to deserialise arrives as null; hand it straight back
                // rather than taking the boot down over a missing translation file.
                if (locale is null)
                {
                    return locale!;
                }

                locale[$"{id} Name"] = DisplayName;
                locale[$"{id} ShortName"] = ShortName;
                locale[$"{id} Description"] = description;
                return locale;
            });
        }
    }

    /// <summary>Adds a cash offer to one trader's assort. Returns the trader's nickname, or null.</summary>
    private string? RegisterTraderOffer(MongoId itemId, NocturneConfig config)
    {
        var traderId = new MongoId(config.TraderId);
        if (!traders.TryGetValue(traderId, out var trader) || trader.Assort is null)
        {
            logger.Warning($"{LogPrefix} trader {config.TraderId} has no assort - skipping the trader offer.");
            return null;
        }

        // A stable id derived from the item id, so re-running never stacks duplicate offers.
        var offerId = new MongoId(NocturneBuffs.ItemId[..21] + "a55");

        trader.Assort.Items ??= [];
        trader.Assort.Items.RemoveAll(item => item.Id == offerId);
        trader.Assort.Items.Add(new Item
        {
            Id = offerId,
            Template = itemId,
            ParentId = "hideout",
            SlotId = "hideout",
            Upd = new Upd
            {
                UnlimitedCount = config.TraderStockCount < 0,
                StackObjectsCount = config.TraderStockCount < 0 ? 999999 : config.TraderStockCount,
            },
        });

        trader.Assort.BarterScheme[offerId] =
        [
            [
                new BarterScheme
                {
                    Count = TraderPrice(config),
                    Template = Money.ROUBLES,
                },
            ],
        ];

        trader.Assort.LoyalLevelItems[offerId] = Math.Clamp(config.TraderLoyaltyLevel, 1, 4);

        return trader.Base?.Nickname ?? config.TraderId;
    }
}
