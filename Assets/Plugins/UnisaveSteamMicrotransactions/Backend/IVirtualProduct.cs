using System.Collections.Generic;

namespace Unisave.SteamMicrotransactions
{
    /// <summary>
    /// Interface for a virtual product (item)
    /// that can be purchased via a Steam microtransaction
    /// </summary>
    public interface IVirtualProduct
    {
        /// <summary>
        /// Your own unique identifier for the item (product)
        /// </summary>
        uint ItemId { get; }

        /// <summary>
        /// Cost of this item in every currency in which
        /// a transaction could be initiated. The cost is
        /// per one virtual item (product) and it is in
        /// currency units, not cents.
        /// </summary>
        IReadOnlyDictionary<string, decimal> UnitCost { get; }

        /// <summary>
        /// Description of this item in every language in which
        /// a transaction could be initiated. This text will
        /// be displayed to the player by Steam during checkout.
        /// </summary>
        IReadOnlyDictionary<string, string> Description { get; }

        /// <summary>
        /// Optional category for the item. This value is used
        /// for grouping sales data in backend Steam reporting
        /// and is never displayed to the player.
        /// </summary>
        string Category { get; }

        /// <summary>
        /// Called when a transaction succeeds and the purchased product
        /// should be given to the player. The method is called as many times,
        /// as the specified product quantity.
        /// </summary>
        /// <param name="transaction"></param>
        void GiveToPlayer(SteamTransactionEntity transaction);
    }
}