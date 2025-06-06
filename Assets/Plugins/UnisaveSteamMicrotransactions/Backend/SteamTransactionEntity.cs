using System;
using System.Collections.Generic;
using Unisave.Entities;

namespace Unisave.SteamMicrotransactions
{
    public class SteamTransactionEntity : Entity
    {
        /// <summary>
        /// State of the transaction
        /// </summary>
        public string state = BeingPreparedState;

        /// <summary>
        /// SteamID of the player that initiated the transaction
        /// </summary>
        public ulong playerSteamId;

        /// <summary>
        /// ID of the entity of the authenticated player
        /// </summary>
        public string authenticatedPlayerId;

        /// <summary>
        /// Unique ID of the order
        /// (generated by your game (see the entity constructor))
        /// </summary>
        public ulong orderId;

        /// <summary>
        /// Unique ID of the transaction
        /// (generated by steam when the transaction is initiated)
        /// </summary>
        public ulong transactionId;

        /// <summary>
        /// What language will item descriptions have
        /// </summary>
        public string language = "en";

        /// <summary>
        /// What currency are item prices in
        /// </summary>
        public string currency = "USD";

        /// <summary>
        /// Items that are part of the transaction
        /// </summary>
        public List<Item> items = new List<Item>();

        /// <summary>
        /// Code of the error, if an error occured
        /// </summary>
        public string errorCode;

        /// <summary>
        /// Description of the error, if an error occured
        /// </summary>
        public string errorDescription;



// =========================================================================
//                    Don't worry about the code below
// =========================================================================



        #region "Transaction states"

        /// <summary>
        /// The transaction is being prepared and it has not yet been initiated
        /// </summary>
        public const string BeingPreparedState = "being-prepared";

        /// <summary>
        /// The transaction has been initiated and now it waits
        /// for authentication by the player via the Steam app
        /// </summary>
        public const string InitiatedState = "initiated";

        /// <summary>
        /// The transaction initiation HTTP request to Steam failed,
        /// the transaction is dead now.
        /// </summary>
        public const string InitiationErrorState = "initiation-error";

        /// <summary>
        /// The transaction has been authorized by the player
        /// but the virtual products have not yet been given to the player
        /// </summary>
        public const string AuthorizedState = "auhorized";

        /// <summary>
        /// The transaction has been aborted by the player,
        /// the transaction is dead now.
        /// </summary>
        public const string AbortedState = "aborted";

        /// <summary>
        /// The transaction finalization HTTP request to Steam failed,
        /// the transaction is dead now.
        /// </summary>
        public const string FinalizationErrorState = "finalization-error";

        /// <summary>
        /// The purchased products have been given to the player,
        /// the transaction is dead now.
        /// </summary>
        public const string CompletedState = "completed";

        #endregion

        /// <summary>
        /// An item of the transaction
        /// </summary>
        public class Item
        {
            // See for details:
            // https://partner.steamgames.com/doc/webapi/ISteamMicroTxn#InitTxn

            public uint itemId;
            public int quantity;
            public int totalAmountInCents;
            public string description;
            public string category;

            /// <summary>
            /// Full class name of the IVirtualProduct
            /// </summary>
            public string productClass;
        }

        public SteamTransactionEntity()
        {
            // randomize order ID
            var random = new Random();
            byte[] buf = new byte[8];
            random.NextBytes(buf);
            orderId = BitConverter.ToUInt64(buf, 0);
        }

        /// <summary>
        /// Adds a product into the transaction in a given quantity
        /// </summary>
        /// <param name="quantity"></param>
        /// <typeparam name="TProduct"></typeparam>
        /// <exception cref="ArgumentException"></exception>
        public void AddProduct<TProduct>(int quantity = 1)
            where TProduct : IVirtualProduct, new()
        {
            if (quantity <= 0)
                throw new ArgumentException(
                    "Product quantity must be a positive integer."
                );

            var product = new TProduct();

            if (!product.Description.ContainsKey(language))
                throw new ArgumentException(
                    $"The product {typeof(TProduct)} does not define " +
                    $"description in language '{language}'."
                );

            if (!product.UnitCost.ContainsKey(currency))
                throw new ArgumentException(
                    $"The product {typeof(TProduct)} does not define " +
                    $"unit cost in currency '{currency}'."
                );

            items.Add(new Item
            {
                itemId = product.ItemId,
                quantity = quantity,
                totalAmountInCents = (int)Math.Ceiling(
                    product.UnitCost[currency] * quantity * 100
                ),
                description = product.Description[language],
                category = product.Category,

                productClass = typeof(TProduct).FullName
            });
        }
    }
}