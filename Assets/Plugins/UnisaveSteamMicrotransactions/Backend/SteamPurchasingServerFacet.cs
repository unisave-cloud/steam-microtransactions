using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unisave.Facades;
using Unisave.Facets;
using Unisave.Foundation;
using Unisave.HttpClient;
using Unisave.Utils;

namespace Unisave.SteamMicrotransactions
{
    /// <summary>
    /// Implements the "Steam purchasing server" as described by the Steam docs.
    /// It's a part of your backend server, it talks to the Steamworks Web API
    /// and is central to the handling of Steam Microtransactions.
    /// It is responsible for initiating transactions from transaction proposals
    /// and finalizing transactions after the player finished interacting with
    /// the Steam Overlay where the player authorizes the purchase.
    /// During transaction finalization, purchased products are given to the
    /// player here on the server-side and the updated player state must then
    /// be fetched from the server by the client.
    /// </summary>
    public class SteamPurchasingServerFacet : Facet
    {
        private readonly SteamMicrotransactionsConfig config;
        private readonly IContainer services;
        
        public SteamPurchasingServerFacet(
            SteamMicrotransactionsConfig config,
            IContainer services
        )
        {
            this.config = config;
            this.services = services;
            
            config.LogValidationWarnings();
        }

        /// <summary>
        /// Given a list of SteamProduct type full-class-names,
        /// returns their localized metadata
        /// </summary>
        public List<LocalizedProductInfo> DownloadProductsInfo(
            string currency,
            string language,
            List<string> productTypeClassNames
        )
        {
            return productTypeClassNames.Select(className =>
                SteamProduct.CreateInstance(className, services)
                    .GetLocalizedInfo(currency: currency, language: language)
            ).ToList();
        }
        
        /// <summary>
        /// Call this method from anywhere within your game
        /// to initiate a new transaction
        /// </summary>
        /// <param name="transaction">Proposal of a new transaction</param>
        public async Task InitiateTransaction(
            SteamTransactionEntity transaction
        )
        {
            ValidateTransactionProposal(transaction);
            
            FillTransactionProposalWithServerSideData(transaction);

            StoreNewTransaction(transaction);

            Response response = await SendInitiationRequestToSteam(transaction);

            if (response["response"]["result"].AsString != "OK")
                StoreInitiationErrorAndThrow(transaction, response);

            MarkTransactionAsInitiated(transaction, response);

            // The player will now be prompted by the Steam Overlay to authorize
            // and pay the transaction. Then Steam will notify the game via
            // a Steamworks callback which will cause the game to call the
            // FinalizeTransaction facet method below to grant the purchased
            // items to the player.
        }

        /// <summary>
        /// This method is called by the SteamPurchasingClient class after
        /// receiving the Steamworks callback. It finalizes the transaction
        /// with Steam and then gives the bought products to the player.
        /// </summary>
        /// <param name="orderId">The order being finalized</param>
        /// <param name="playerAuthorizedTheTransactionInSteamOverlay">
        /// Player authorized or aborted the transaction in the Steam overlay.
        /// </param>
        /// <returns>The final transaction data</returns>
        public async Task<SteamTransactionEntity> FinalizeTransaction(
            ulong orderId,
            bool playerAuthorizedTheTransactionInSteamOverlay
        )
        {
            var transaction = FindInitiatedTransaction(orderId);

            if (!playerAuthorizedTheTransactionInSteamOverlay)
            {
                MarkTransactionAsAborted(transaction);

                return transaction;
            }

            Response response = await SendFinalizationRequestToSteam(
                transaction
            );

            if (response["response"]["result"].AsString != "OK")
                StoreFinalizationErrorAndThrow(transaction, response);

            MarkTransactionAsAuthorized(transaction);

            await GiveProductsToPlayer(transaction);

            MarkTransactionAsCompleted(transaction);

            return transaction;
        }

        #region "InitiateTransaction implementation"

        private void ValidateTransactionProposal(
            SteamTransactionEntity transaction
        )
        {
            if (transaction.EntityId != null)
                throw new ArgumentException(
                    "Given transaction has already been initiated."
                );

            if (transaction.PlayerSteamId == 0)
                throw new ArgumentException(
                    $"Given transaction does not have " +
                    $"{nameof(transaction.PlayerSteamId)} specified."
                );

            if (transaction.Items.Count == 0)
                throw new ArgumentException(
                    "Given transaction has no items inside of it."
                );
            
            if (string.IsNullOrEmpty(transaction.Currency))
                throw new ArgumentException(
                    "Given transaction has no currency specified."
                );
            
            if (string.IsNullOrEmpty(transaction.Language))
                throw new ArgumentException(
                    "Given transaction has no language specified."
                );
            
            foreach (var item in transaction.Items)
                ValidateProposedTransactionItem(transaction, item);
        }

        private void ValidateProposedTransactionItem(
            SteamTransactionEntity transaction,
            SteamTransactionItem item
        )
        {
            SteamProduct product = SteamProduct.CreateInstance(
                item.ProductTypeClassName,
                services
            );

            LocalizedProductInfo productInfo = product.GetLocalizedInfo(
                currency: transaction.Currency,
                language: transaction.Language
            );

            if (item.ItemId != productInfo.ItemId)
                throw new ArgumentException(
                    "Item does not match the product in: 'ItemId'"
                );
            
            if (item.UnitCost != productInfo.UnitCost)
                throw new ArgumentException(
                    "Item does not match the product in: 'ItemId'"
                );
            
            if (item.Description != productInfo.Description)
                throw new ArgumentException(
                    "Item does not match the product in: 'ItemId'"
                );
            
            if (item.Category != productInfo.Category)
                throw new ArgumentException(
                    "Item does not match the product in: 'ItemId'"
                );
            
            if (item.Quantity <= 0)
                throw new ArgumentException(
                    "Item quantity must be a positive integer."
                );
            
            // NOTE: TotalAmountInCents is re-calculated later
        }

        private void FillTransactionProposalWithServerSideData(
            SteamTransactionEntity transaction
        )
        {
            // Generate order id for the transaction
            transaction.OrderId = SteamTransactionEntity.GenerateRandomOrderId();

            // Remember the unisave-user that initiated the transaction
            transaction.UnisavePlayerId = Auth.Id();
            
            // Recompute total value in cents for each item, to make sure
            // the client does not sneak in a different cost.
            foreach (var item in transaction.Items)
                item.RecomputeTotalAmountInCents();
        }
        
        private void StoreNewTransaction(SteamTransactionEntity transaction)
        {
            transaction.State = SteamTransactionState.BeingPrepared;
            transaction.Save();
        }

        private async Task<Response> SendInitiationRequestToSteam(
            SteamTransactionEntity transaction
        )
        {
            // https://partner.steamgames.com/doc/webapi/ISteamMicroTxn#InitTxn

            var response = await Http.PostAsync(
                GetSteamApiUrl() + "InitTxn/v3/",
                BuildInitiationRequestBody(transaction)
            );

            if (!response.IsOk)
            {
                string body = await response.BodyAsync();
                Log.Info("Steam API response body:\n" + body);
                response.Throw();
            }

            return response;
        }

        private Dictionary<string, string> BuildInitiationRequestBody(
            SteamTransactionEntity transaction
        )
        {
            var body = new Dictionary<string, string>
            {
                ["key"] = config.SteamPublisherKey.ToString(),
                ["orderid"] = transaction.OrderId.ToString(),
                ["steamid"] = transaction.PlayerSteamId.ToString(),
                ["appid"] = config.SteamAppId.ToString(),
                ["itemcount"] = transaction.Items.Count.ToString(),
                ["language"] = transaction.Language,
                ["currency"] = transaction.Currency
            };

            for (int i = 0; i < transaction.Items.Count; i++)
            {
                var item = transaction.Items[i];

                body[$"itemid[{i}]"] = item.ItemId.ToString();
                body[$"qty[{i}]"] = item.Quantity.ToString();
                body[$"amount[{i}]"] = item.TotalAmountInCents.ToString();
                body[$"description[{i}]"] = item.Description;
                if (!string.IsNullOrWhiteSpace(item.Category))
                    body[$"category[{i}]"] = item.Category;
            }

            return body;
        }

        private void StoreInitiationErrorAndThrow(
            SteamTransactionEntity transaction,
            Response response
        )
        {
            transaction.State = SteamTransactionState.InitiationError;
            transaction.ErrorCode
                = response["response"]["error"]["errorcode"].AsString;
            transaction.ErrorDescription
                = response["response"]["error"]["errordesc"].AsString;
            transaction.Save();

            throw new SteamMicrotransactionException(
                "Steam rejected transaction initiation.",
                transaction.OrderId,
                transaction.ErrorCode,
                transaction.ErrorDescription
            );
        }

        private void MarkTransactionAsInitiated(
            SteamTransactionEntity transaction,
            Response response
        )
        {
            transaction.State = SteamTransactionState.Initiated;
            transaction.TransactionId = ulong.Parse(
                response["response"]["params"]["transid"].AsString
            );
            transaction.Save();

            Log.Info("Marked transaction as initiated.");
        }

        #endregion

        #region "FinalizeTransaction implementation"

        private SteamTransactionEntity FindInitiatedTransaction(ulong orderId)
        {
            var transaction = DB.TakeAll<SteamTransactionEntity>()
                .Filter(t =>
                    t.OrderId == orderId &&
                    t.State == SteamTransactionState.Initiated
                )
                .First();

            if (transaction == null)
                throw new SteamMicrotransactionException(
                    $"No initiated transaction with " +
                    $"order id {orderId} was found."
                );

            return transaction;
        }

        private void MarkTransactionAsAborted(
            SteamTransactionEntity transaction
        )
        {
            transaction.State = SteamTransactionState.Aborted;
            transaction.Save();

            Log.Info("Marked transaction as aborted.");
        }

        private async Task<Response> SendFinalizationRequestToSteam(
            SteamTransactionEntity transaction
        )
        {
            // https://partner.steamgames.com/doc/webapi/ISteamMicroTxn#FinalizeTxn

            var response = await Http.PostAsync(
                GetSteamApiUrl() + "FinalizeTxn/v2/",
                new Dictionary<string, string>
                {
                    ["key"] = config.SteamPublisherKey.ToString(),
                    ["orderid"] = transaction.OrderId.ToString(),
                    ["appid"] = config.SteamAppId.ToString(),
                }
            );

            if (!response.IsOk)
            {
                string body = await response.BodyAsync();
                Log.Info("Steam API response body:\n" + body);
                response.Throw();
            }

            return response;
        }

        private void StoreFinalizationErrorAndThrow(
            SteamTransactionEntity transaction,
            Response response
        )
        {
            transaction.State = SteamTransactionState.FinalizationError;
            transaction.ErrorCode
                = response["response"]["error"]["errorcode"].AsString;
            transaction.ErrorDescription
                = response["response"]["error"]["errordesc"].AsString;
            transaction.Save();

            throw new SteamMicrotransactionException(
                "Steam rejected transaction finalization.",
                transaction.OrderId,
                transaction.ErrorCode,
                transaction.ErrorDescription
            );
        }

        private void MarkTransactionAsAuthorized(
            SteamTransactionEntity transaction
        )
        {
            transaction.State = SteamTransactionState.Authorized;
            transaction.Save();

            Log.Info("Marked transaction as authorized.");
        }

        private async Task GiveProductsToPlayer(
            SteamTransactionEntity transaction
        )
        {
            // Create child IoC container that has the transaction entity
            // registered so that it can be resolved from the product classes
            // if necessary.
            var childServices = services.CreateChildContainer();
            childServices.RegisterInstance(transaction);
            
            // first, instantiate all item-product pairs
            var itemProductPairs = transaction.Items.Select(
                item => (item, SteamProduct.CreateInstance(
                    item.ProductTypeClassName,
                    childServices
                ))
            ).ToArray();
            
            // for each item, give the corresponding product to the player
            foreach (var (item, product) in itemProductPairs)
            {
                Log.Info(
                    $"Giving product {product.GetType().FullName} to the " +
                    $"player in quantity {item.Quantity}x..."
                );
                
                await product.GiveToPlayerAsync(item.Quantity);

                Log.Info(
                    $"Product {product.GetType().FullName} has been " +
                    $"given to the player."
                );
            }
        }

        private void MarkTransactionAsCompleted(
            SteamTransactionEntity transaction
        )
        {
            transaction.State = SteamTransactionState.Completed;
            transaction.Save();

            Log.Info("Marked transaction as completed.");
        }

        #endregion

        /// <summary>
        /// URL of the Steam microtransactions API, ending with a slash
        /// </summary>
        private string GetSteamApiUrl()
        {
            // base url for all Steam APIs
            string steamApi = Str.Finish(config.SteamApiUrl, "/");

            // create the microtransactions API URL
            if (config.UseSandbox)
            {
                return steamApi + "ISteamMicroTxnSandbox/";
            }
            else
            {
                return steamApi + "ISteamMicroTxn/";
            }
        }
    }
}