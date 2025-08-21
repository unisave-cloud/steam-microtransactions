using System;
using System.Collections.Generic;

namespace Plugins.UnisaveSteamMicrotransactions.Backend.Steam
{
    /// <summary>
    /// Represents one transaction, returned by the GetReport/v5/ Steam API,
    /// taken directly from the Steam API documentation:
    /// https://partner.steamgames.com/doc/webapi/ISteamMicroTxn#GetReport
    /// </summary>
    public class ReportOrder
    {
        /// <summary>
        /// Unique 64-bit ID for order. (This will be 0 for recurring
        /// subscriptions initiated from the Steam store, use transid instead.)
        /// </summary>
        public ulong orderid;

        public ulong OrderId => orderid;

        /// <summary>
        /// Unique 64-bit Steam transaction ID.
        /// </summary>
        public ulong transid;
        
        public ulong TransactionId => transid;

        /// <summary>
        /// The Steam ID of user that the order/transaction belongs to.
        /// </summary>
        public ulong steamid;
        
        public ulong PlayerSteamId => steamid;

        /// <summary>
        /// Status of the order. See: "Appendix A: Status Values"
        /// https://partner.steamgames.com/doc/features/microtransactions/implementation#status_values
        /// </summary>
        public string status;

        public string Status => status;

        /// <summary>
        /// ISO 4217 currency code.
        /// </summary>
        public string currency;
        
        public string Currency => currency;

        /// <summary>
        /// Time of the most recent update to the transaction.
        /// (RFC 3339 UTC formatted like: 2010-01-01T00:00:00Z)
        /// </summary>
        public string time;
        
        public DateTime Time => DateTime.ParseExact(
            time,
            "yyyy-MM-dd'T'HH:mm:ssK",
            null
        );
        
        /// <summary>
        /// ISO 3166-1-alpha-2 country code.
        /// </summary>
        public string country;
        
        public string Country => country;

        /// <summary>
        /// US State. Empty for non-US countries.
        /// </summary>
        public string usstate;
        
        public string UsState => usstate;

        /// <summary>
        /// Individual items purchased in the transaction (in the order)
        /// </summary>
        public List<ReportItem> items = new List<ReportItem>();
        
        public List<ReportItem> Items => items;
    }
}