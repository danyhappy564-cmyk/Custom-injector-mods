using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace Nocturne.Server;

/// <summary>
/// Puts the syringe on a trader's shelf.
/// </summary>
/// <remarks>
/// Split out of <see cref="NocturneMod"/> because the two steps cannot share a slot. The item has
/// to be registered at <see cref="OnLoadOrder.Preload"/> or the database integrity check kills the
/// server, but traders are only wired up at <see cref="OnLoadOrder.TraderRegistration"/> - putting
/// an assort entry in at Preload risks writing into a trader table that is not ready yet, which
/// fails quietly and simply leaves the item unbuyable.
///
/// Adding to an assort late is safe in a way adding an item template is not: assorts are read per
/// request, and ragfair's trader-offer pass runs at <see cref="OnLoadOrder.RagfairCallbacks"/>,
/// well after this.
/// </remarks>
[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.TraderRegistration + 50)]
public class NocturneTraderOffer(
    ISptLogger<NocturneTraderOffer> logger,
    NocturneConfigLoader configLoader,
    TemplateTable templates,
    TradersTable traders
) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken = default)
    {
        var config = configLoader.Value;
        if (!config.SellAtTrader)
        {
            return Task.CompletedTask;
        }

        var itemId = new MongoId(NocturneBuffs.ItemId);
        if (!templates.Items.ContainsKey(itemId))
        {
            // The Preload step bailed out (id clash, or SJ9 missing). Nothing to sell.
            return Task.CompletedTask;
        }

        var traderId = new MongoId(config.TraderId);
        if (!traders.TryGetValue(traderId, out var trader) || trader.Assort is null)
        {
            logger.Warning(
                $"{NocturneMod.LogPrefix} trader {config.TraderId} has no assort - the syringe will not be sold. "
                + (config.AllowOnFlea
                    ? "It is still buyable on the flea."
                    : "AllowOnFlea is off too, so there is now no way to obtain it."));
            return Task.CompletedTask;
        }

        // A stable id derived from the item id, so re-running never stacks duplicate offers.
        var offerId = new MongoId(NocturneBuffs.ItemId[..21] + "a55");
        var price = config.TraderPrice > 0 ? config.TraderPrice : config.HandbookPrice;

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
                    Count = price,
                    Template = Money.ROUBLES,
                },
            ],
        ];

        var loyalty = Math.Clamp(config.TraderLoyaltyLevel, 1, 4);
        trader.Assort.LoyalLevelItems[offerId] = loyalty;

        logger.Success(
            $"{NocturneMod.LogPrefix} sold by {trader.Base?.Nickname ?? config.TraderId} "
            + $"at LL{loyalty} for {price:N0}₽.");
        return Task.CompletedTask;
    }
}
