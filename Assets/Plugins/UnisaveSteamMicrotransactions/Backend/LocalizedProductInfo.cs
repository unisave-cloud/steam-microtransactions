using System;

namespace Unisave.SteamMicrotransactions.Steam
{
    /// <summary>
    /// Holds information about a product that can be purchased,
    /// localized into the given language and currency.
    /// This class is designed to be used by the client.
    /// </summary>
    public class LocalizedProductInfo
    {
        /// <summary>
        /// Your own unique identifier for the item (product)
        /// </summary>
        public uint ItemId { get; }
        
        /// <summary>
        /// The currency into which this product info is localized
        /// </summary>
        public string Currency { get; }
        
        /// <summary>
        /// The language into which this product info is localized
        /// </summary>
        public string Language { get; }
        
        /// <summary>
        /// Cost of one unit of the product in the chosen currency
        /// </summary>
        public decimal UnitCost { get; }
        
        /// <summary>
        /// Human-readable description of the product in the language
        /// into which this product info is localized. Same as
        /// what Steam Overlay displays during checkout.
        /// </summary>
        public string Description { get; }
        
        /// <summary>
        /// Optional category for the item. This value is used
        /// for grouping sales data in backend Steam reporting
        /// and is never displayed to the player.
        /// </summary>
        public string Category { get; }
        
        /// <summary>
        /// The specific SteamProduct type that this localization corresponds to
        /// </summary>
        public string ProductTypeClassName { get; }

        public LocalizedProductInfo(
            uint itemId,
            string currency,
            string language,
            decimal unitCost,
            string description,
            string category,
            string productTypeClassName
        )
        {
            ItemId = itemId;
            Currency = currency;
            Language = language;
            UnitCost = unitCost;
            Description = description;
            Category = category;
            ProductTypeClassName = productTypeClassName;
        }
    }
}