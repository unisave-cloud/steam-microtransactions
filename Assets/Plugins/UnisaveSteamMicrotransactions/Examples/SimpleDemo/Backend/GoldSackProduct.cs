using System.Collections.Generic;
using Unisave.Facades;

namespace Unisave.SteamMicrotransactions.Steam.Examples.SimpleDemo
{
    /// <summary>
    /// Defines a sack of gold coins Steam virtual product that can be purchased
    /// and given to the player. This class can only be instantiated
    /// on the backend server.
    /// </summary>
    public class GoldSackProduct : SteamProduct
    {
        /// <summary>
        /// Your own unique identifier for the item (product).
        /// Is passed to steam API to give a machine-readable identifier to the
        /// product on the steam-side.
        /// </summary>
        public override uint ItemId => 1;
    
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
                ["USD"] = 5.00m, // "m" means "decimal type"
                ["EUR"] = 4.25m
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
                ["en"] = "Sack of 100 gold coins.",
                ["de"] = "Beutel mit 100 Goldmünzen."
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
            // get the logged-in player
            var player = Auth.GetPlayer<PlayerEntity>();
            
            // and give him the gold
            player.goldCoins += 100 * quantity;
            player.Save();
        }
    }
}