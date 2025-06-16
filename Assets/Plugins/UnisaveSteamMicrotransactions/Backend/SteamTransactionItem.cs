using System;

namespace Unisave.SteamMicrotransactions
{
    /// <summary>
    /// Represents one item (product) in a steam microtransaction.
    /// 
    /// Gets used as arguments to the steam init txn call:
    /// https://partner.steamgames.com/doc/webapi/ISteamMicroTxn#InitTxn
    /// </summary>
    public class SteamTransactionItem
    {
        /// <summary>
        /// Your own unique identifier for the item (product).
        /// Is passed to steam API to give a machine-readable identifier to the
        /// product on the steam-side.
        /// </summary>
        public uint ItemId { get; }
        
        /// <summary>
        /// How many times is the item present in the transaction
        /// </summary>
        public int Quantity { get; }
        
        /// <summary>
        /// How much should be (was) charged for one instance of the product.
        /// The currency is taken from the parent SteamTransactionEntity
        /// instance and when multiplied by quantity and 100 should equal the
        /// TotalAmountInCents value. But this value is only informative,
        /// the total value is what is sent to Steam and ultimately charged.
        /// </summary>
        public decimal UnitCost { get; }
        
        /// <summary>
        /// How much should be (was) charged for the item in the given quantity.
        /// The amount is represented in cents (which aligns with the Steam API)
        /// and the currency is taken from the parent SteamTransactionEntity
        /// instance.
        /// </summary>
        public int TotalAmountInCents { get; private set; }
        
        /// <summary>
        /// Human-readable description of the product, displayed to the user
        /// in the Steam overlay during checkout.
        /// </summary>
        public string Description { get; }
        
        /// <summary>
        /// Optional category for the item. This value is used
        /// for grouping sales data in backend Steam reporting
        /// and is never displayed to the player.
        /// Maximum length is 64 characters. Null means "no category set".
        /// </summary>
        public string Category { get; }

        /// <summary>
        /// Full class name of the SteamProduct type
        /// that this item corresponds to.
        /// </summary>
        public string ProductTypeClassName { get; }

        public SteamTransactionItem(
            uint itemId,
            int quantity,
            decimal unitCost,
            string description,
            string category,
            string productTypeClassName
        )
        {
            ItemId = itemId;
            Quantity = quantity;
            UnitCost = unitCost;
            Description = description;
            Category = category;
            ProductTypeClassName = productTypeClassName;
            
            TotalAmountInCents = 0;
            RecomputeTotalAmountInCents();
        }

        /// <summary>
        /// Sets the value of TotalAmountInCents
        /// based on the UnitCost and Quantity.
        /// </summary>
        public void RecomputeTotalAmountInCents()
        {
            TotalAmountInCents = (int)Math.Ceiling(
                UnitCost * Quantity * 100
            );
        }
    }
}