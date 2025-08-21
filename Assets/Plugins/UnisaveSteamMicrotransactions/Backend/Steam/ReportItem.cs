namespace Plugins.UnisaveSteamMicrotransactions.Backend.Steam
{
    /// <summary>
    /// Represents one item record within a transaction (an order),
    /// returned by the GetReport/v5/ Steam API,
    /// taken directly from the Steam API documentation:
    /// https://partner.steamgames.com/doc/webapi/ISteamMicroTxn#GetReport
    /// </summary>
    public class ReportItem
    {
        /// <summary>
        /// Game ID number of item.
        /// </summary>
        public uint itemid;
        
        public uint ItemId => itemid;

        /// <summary>
        /// Quantity of this item.
        /// </summary>
        public int qty;
        
        public int Quantity => qty;

        /// <summary>
        /// Total cost to user minus VAT (in cents). (199 = 1.99)
        /// </summary>
        public int amount;

        public int TotalAmountInCentsWithoutVat => amount;

        /// <summary>
        /// Total VAT or tax (in cents). (19 = .19)
        /// </summary>
        public int vat;

        public int TotalVatInCents => vat;

        /// <summary>
        /// Status of items within the order.
        /// </summary>
        public string itemstatus;
        
        public string ItemStatus => itemstatus;
        
        // NOTE: "storepurchasereference" is missing since this module does not
        // support item purchases through the steam store (DLC packages)
    }
}