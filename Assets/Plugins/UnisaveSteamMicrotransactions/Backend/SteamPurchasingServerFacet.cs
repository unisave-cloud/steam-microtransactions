using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unisave.Facades;
using Unisave.Facets;
using Unisave.HttpClient;
using Unisave.Utils;

namespace Unisave.SteamMicrotransactions
{
    public class SteamPurchasingServerFacet : Facet
    {
        private readonly SteamMicrotransactionsConfig config;
        
        public SteamPurchasingServerFacet(SteamMicrotransactionsConfig config)
        {
            this.config = config;
            
            config.LogValidationWarnings();
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

            // You can add additional data to the transaction entity
            // e.g. the currently logged-in player ID
            transaction.authenticatedPlayerId = Auth.Id();

            StoreNewTransaction(transaction);

            Response response = await SendInitiationRequestToSteam(transaction);

            if (response["response"]["result"].AsString != "OK")
                StoreInitiationErrorAndThrow(transaction, response);

            MarkTransactionAsInitiated(transaction, response);

            // The player will be prompted by the Steam App to authorize
            // and pay the transaction. Then Steam will notify your game
            // via a Steamworks callback that is handled automatically
            // by the SteamPurchasingClient class.
        }

        /// <summary>
        /// This method is called by the SteamPurchasingClient class after
        /// receiving the Steamworks callback. It finalizes the transaction
        /// with Steam and then gives the bought products to the player.
        /// </summary>
        /// <param name="orderId">The order being finalized</param>
        /// <param name="authorized">Player authorized or aborted?</param>
        /// <returns>The final transaction data</returns>
        public async Task<SteamTransactionEntity> FinalizeTransaction(
            ulong orderId,
            bool authorized
        )
        {
            var transaction = FindInitiatedTransaction(orderId);

            if (!authorized)
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

            GiveProductsToPlayer(transaction);

            // Here the proper IVirtualProduct.GiveToPlayer(...) methods
            // are called so make sure you implement them.

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

            if (transaction.playerSteamId == 0)
                throw new ArgumentException(
                    $"Given transaction does not have " +
                    $"{nameof(transaction.playerSteamId)} specified."
                );

            if (transaction.items.Count == 0)
                throw new ArgumentException(
                    "Given transaction has no items inside of it."
                );
        }

        private void StoreNewTransaction(SteamTransactionEntity transaction)
        {
            transaction.state = SteamTransactionEntity.BeingPreparedState;
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
                ["orderid"] = transaction.orderId.ToString(),
                ["steamid"] = transaction.playerSteamId.ToString(),
                ["appid"] = config.SteamAppId.ToString(),
                ["itemcount"] = transaction.items.Count.ToString(),
                ["language"] = transaction.language,
                ["currency"] = transaction.currency
            };

            for (int i = 0; i < transaction.items.Count; i++)
            {
                var item = transaction.items[i];

                body[$"itemid[{i}]"] = item.itemId.ToString();
                body[$"qty[{i}]"] = item.quantity.ToString();
                body[$"amount[{i}]"] = item.totalAmountInCents.ToString();
                body[$"description[{i}]"] = item.description;
                if (!string.IsNullOrWhiteSpace(item.category))
                    body[$"category[{i}]"] = item.category;
            }

            return body;
        }

        private void StoreInitiationErrorAndThrow(
            SteamTransactionEntity transaction,
            Response response
        )
        {
            transaction.state = SteamTransactionEntity.InitiationErrorState;
            transaction.errorCode
                = response["response"]["error"]["errorcode"].AsString;
            transaction.errorDescription
                = response["response"]["error"]["errordesc"].AsString;
            transaction.Save();

            throw new SteamMicrotransactionException(
                "Steam rejected transaction initiation.",
                transaction.orderId,
                transaction.errorCode,
                transaction.errorDescription
            );
        }

        private void MarkTransactionAsInitiated(
            SteamTransactionEntity transaction,
            Response response
        )
        {
            transaction.state = SteamTransactionEntity.InitiatedState;
            transaction.transactionId = ulong.Parse(
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
                    t.orderId == orderId &&
                    t.state == SteamTransactionEntity.InitiatedState
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
            transaction.state = SteamTransactionEntity.AbortedState;
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
                    ["orderid"] = transaction.orderId.ToString(),
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
            transaction.state = SteamTransactionEntity.FinalizationErrorState;
            transaction.errorCode
                = response["response"]["error"]["errorcode"].AsString;
            transaction.errorDescription
                = response["response"]["error"]["errordesc"].AsString;
            transaction.Save();

            throw new SteamMicrotransactionException(
                "Steam rejected transaction finalization.",
                transaction.orderId,
                transaction.errorCode,
                transaction.errorDescription
            );
        }

        private void MarkTransactionAsAuthorized(
            SteamTransactionEntity transaction
        )
        {
            transaction.state = SteamTransactionEntity.AuthorizedState;
            transaction.Save();

            Log.Info("Marked transaction as authorized.");
        }

        private void GiveProductsToPlayer(SteamTransactionEntity transaction)
        {
            foreach (var item in transaction.items)
            {
                Type productType = Type.GetType(item.productClass);

                if (productType == null)
                    throw new Exception(
                        $"Cannot find product {item.productClass}"
                    );

                var instance = (IVirtualProduct)Activator.CreateInstance(
                    productType
                );

                var method = productType.GetMethod(
                    nameof(IVirtualProduct.GiveToPlayer)
                );

                if (method == null)
                    throw new Exception(
                        $"Class {item.productClass} does not contain " +
                        $"method {nameof(IVirtualProduct.GiveToPlayer)}"
                    );

                for (int i = 0; i < item.quantity; i++)
                {
                    method.Invoke(instance, new object[] { transaction });

                    Log.Info("Product has been given to the player.", item);
                }
            }
        }

        private void MarkTransactionAsCompleted(
            SteamTransactionEntity transaction
        )
        {
            transaction.state = SteamTransactionEntity.CompletedState;
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