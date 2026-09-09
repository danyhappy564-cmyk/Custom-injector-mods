namespace Nocturne.Server;

/// <summary>
/// Shape of <c>config/config.jsonc</c>. Names are matched case-sensitively by SPT's JSON reader,
/// so they must stay spelled exactly as the shipped config writes them.
/// </summary>
public class NocturneConfig
{
    /// <summary>Handbook price in roubles. Also the base SPT prices flea offers from.</summary>
    public int HandbookPrice { get; set; } = 145000;

    /// <summary>
    /// Flea price in roubles. SPT seeds dynamic offers from the price table rather than the
    /// handbook, so this is the number you actually pay on the flea. 0 means "use HandbookPrice".
    /// </summary>
    public int FleaPrice { get; set; }

    /// <summary>Whether the item may be listed on and bought from the flea market at all.</summary>
    public bool AllowOnFlea { get; set; } = true;

    /// <summary>Sell it at a trader as well. With the flea off, this is the only way to get one.</summary>
    public bool SellAtTrader { get; set; } = true;

    /// <summary>Trader id. Default is Therapist, who sells the vanilla stims.</summary>
    public string TraderId { get; set; } = "54cb57776803fa99248b456e";

    /// <summary>Trader loyalty level required, 1-4.</summary>
    public int TraderLoyaltyLevel { get; set; } = 3;

    /// <summary>Trader price in roubles. 0 means "use HandbookPrice".</summary>
    public int TraderPrice { get; set; }

    /// <summary>How many the trader restocks. -1 for unlimited.</summary>
    public int TraderStockCount { get; set; } = 3;

    /// <summary>Seconds the injection animation takes.</summary>
    public double UseTime { get; set; } = 2;
}
