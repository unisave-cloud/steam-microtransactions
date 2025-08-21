using System.Collections.Generic;
using Unisave.Facades;

namespace Unisave.SteamMicrotransactions.Steam.Examples.ClientSideData
{
    /// <summary>
    /// Defines a "20 Diamonds" Steam virtual product that can be purchased
    /// and given to the player. This class can only be instantiated
    /// on the backend server.
    /// </summary>
    public class TwentyDiamondsProduct : SteamProduct
    {
        /// <summary>
        /// Your own unique identifier for the item (product).
        /// Is passed to steam API to give a machine-readable identifier to the
        /// product on the steam-side.
        /// </summary>
        public override uint ItemId => 3;
    
        /// <summary>
        /// Cost of this item in every currency in which
        /// a transaction could be initiated. The cost is
        /// per one virtual item (product) and it is in
        /// currency units, not cents.
        /// 
        /// Keys are ISO 4217 currency codes. Supported currencies are:
        /// https://partner.steamgames.com/doc/store/pricing/currencies
        /// </summary>
        public override IReadOnlyDictionary<string, decimal> UnitCost
            => new Dictionary<string, decimal> {
                ["USD"] = 19.99m, // "m" means "decimal type"
                ["EUR"] = 16.99m
            };
    
        /// <summary>
        /// Description of this item in every language in which
        /// a transaction could be initiated. This text will
        /// be displayed to the player by Steam during checkout.
        /// 
        /// Keys are ISO 639-1 language codes. Supported languages are:
        /// https://partner.steamgames.com/doc/store/localization/languages
        /// </summary>
        public override IReadOnlyDictionary<string, string> Description
            => new Dictionary<string, string> {
                ["en"] = "Twenty diamonds.",
                ["de"] = "Zwanzig Diamanten."
            };

        /// <summary>
        /// Optional category for the item. This value is used
        /// for grouping sales data in backend Steam reporting
        /// and is never displayed to the player.
        /// </summary>
        public override string Category => null;

        /// <summary>
        /// Called when a transaction succeeds and the purchased product
        /// should be given to the player. The number of times the product was
        /// purchased is provided as an argument.
        /// </summary>
        public override void GiveToPlayer(int quantity)
        {
            // Nothing here.
            // Product will be given to the player on the client side.
        }
    }
}