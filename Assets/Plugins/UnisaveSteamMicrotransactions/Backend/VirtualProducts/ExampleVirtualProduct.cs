using System.Collections.Generic;
using Unisave.Facades;
using Unisave.SteamMicrotransactions;

/*
 * SteamMicrotransactions template - v0.9.0
 * ----------------------------------------
 *
 * This is an example product that can be purchased via Steam.
 * Modify, duplicate, and rename this the way you need.
 * Example products: "medium gold pack", "premium for a month", "no ads" ...
 * 
 * Read more from Steam:
 * https://partner.steamgames.com/doc/features/microtransactions/implementation
 *
 * List of currencies supported by Steam:
 * https://partner.steamgames.com/doc/store/pricing/currencies
 */

public class ExampleVirtualProduct : IVirtualProduct
{
    /// <summary>
    /// Your own unique identifier for the item (product)
    /// </summary>
    public uint ItemId => 1;
    
    /// <summary>
    /// Cost of this item in every currency in which
    /// a transaction could be initiated. The cost is
    /// per one virtual item (product) and it is in
    /// currency units, not cents.
    /// </summary>
    public IReadOnlyDictionary<string, decimal> UnitCost
        => new Dictionary<string, decimal> {
            ["USD"] = 5.00m, // "m" means "decimal type"
            ["EUR"] = 4.25m
        };
    
    /// <summary>
    /// Description of this item in every language in which
    /// a transaction could be initiated. This text will
    /// be displayed to the player by Steam during checkout.
    /// </summary>
    public IReadOnlyDictionary<string, string> Description
        => new Dictionary<string, string> {
            ["en"] = "An example product, that a user can buy.",
            ["de"] = "Ein Beispielprodukt, das ein Benutzer kaufen kann."
        };

    /// <summary>
    /// Optional category for the item. This value is used
    /// for grouping sales data in backend Steam reporting
    /// and is never displayed to the player.
    /// </summary>
    public string Category => null;

    /// <summary>
    /// Called when a transaction succeeds and the purchased product
    /// should be given to the player. The method is called as many times,
    /// as the specified product quantity.
    /// </summary>
    public void GiveToPlayer(SteamTransactionEntity transaction)
    {
        Log.Info(
            $"Giving item {nameof(ExampleVirtualProduct)} to the player..."
        );
        
        // Implement your logic here.
        // Say you want to give gold to the player:
        //
        //    var player = Auth.GetPlayer<PlayerEntity>();
        //    player.gold += 1_000;
        //    player.Save();
        //
    }
}

