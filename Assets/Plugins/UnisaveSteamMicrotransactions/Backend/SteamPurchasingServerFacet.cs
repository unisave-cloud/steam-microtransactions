using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LightJson;
using Unisave.Facades;
using Unisave.Facets;
using Unisave.Foundation;
using Unisave.Serialization;
using Unisave.SteamMicrotransactions.Steam.Steam;

namespace Unisave.SteamMicrotransactions.Steam
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
        private readonly IContainer services;
        private readonly SteamWebMtxApi steamApi;
        
        public SteamPurchasingServerFacet(
            SteamMicrotransactionsConfig config,
            IContainer services,
            SteamWebMtxApi steamApi
        )
        {
            this.services = services;
            this.steamApi = steamApi;
            
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
        public async Task<SteamTransactionEntity> InitiateTransaction(
            SteamTransactionEntity transaction
        )
        {
            ValidateTransactionProposal(transaction);
            
            FillTransactionProposalWithServerSideData(transaction);

            StoreNewTransaction(transaction);

            await CaptureExceptions(
                transaction,
                SteamTransactionState.InitiationError,
                async () =>
                {
                    ulong transactionId = await steamApi.InitTxn(transaction);
                    
                    MarkTransactionAsInitiated(transaction, transactionId);
                }
            );

            // The player will now be prompted by the Steam Overlay to authorize
            // and pay the transaction. Then Steam will notify the game via
            // a Steamworks callback which will cause the game to call the
            // FinalizeTransaction facet method below to grant the purchased
            // items to the player.

            return transaction;
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

            await CaptureExceptions(
                transaction,
                SteamTransactionState.FinalizationError,
                async () =>
                {
                    await steamApi.FinalizeTxn(transaction.OrderId);

                    MarkTransactionAsAuthorized(transaction);

                    await GiveProductsToPlayer(transaction);

                    MarkTransactionAsCompleted(transaction);
                }
            );

            return transaction;
        }

        /// <summary>
        /// Call this method to upload an exception that occured in the client
        /// code. Entity ID, and for security also order ID is required.
        /// The uploaded exception must be already serialized into a JSON object.
        /// </summary>
        public void UploadClientException(
            ulong orderId,
            string entityId,
            JsonObject exception
        )
        {
            // find the entity in the database
            var transaction = FindTransaction(orderId, entityId);
            
            // if it already had an exception do nothing
            if (transaction.State == SteamTransactionState.Exception)
            {
                Log.Warning(
                    $"Ignoring exception upload for transaction {entityId} " +
                    $"because it already has an exception."
                );
                return;
            }
            
            // store the exception
            transaction.Exception = exception;
            transaction.StateBeforeException = transaction.State;
            transaction.State = SteamTransactionState.Exception;
            transaction.Save();
        }

        /// <summary>
        /// This is called for client-side player data transactions,
        /// after the products are given to the player client-side.
        /// It changes the state of the transaction in the database.
        /// </summary>
        public void NotifyOfClientSideCompletion(ulong orderId, string entityId)
        {
            // find the entity in the database
            var transaction = FindTransaction(orderId, entityId);
            
            // it has to be in the "completed" state
            if (transaction.State != SteamTransactionState.Completed)
            {
                Log.Warning(
                    $"Ignoring client completion notification for {entityId} " +
                    $"because it is not in the completed state. " +
                    $"It is in state: {transaction.State}"
                );
                return;
            }
            
            // change the state
            transaction.State = SteamTransactionState.ClientSideCompleted;
            transaction.Save();
            
            Log.Info("Marked transaction as client-side-completed.");
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
                    "Item does not match the product in: 'UnitCost'"
                );
            
            if (item.Description != productInfo.Description)
                throw new ArgumentException(
                    "Item does not match the product in: 'Description'"
                );
            
            if (item.Category != productInfo.Category)
                throw new ArgumentException(
                    "Item does not match the product in: 'Category'"
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
            // Clear data that should not be set by the client
            transaction.TransactionId = 0;
            transaction.ErrorCode = null;
            transaction.ErrorDescription = null;
            transaction.Exception = null;
            transaction.StateBeforeException = null;
            transaction.SteamReportOrder = null;
            transaction.SteamReportOrderTimestamp = null;
            
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

        private void MarkTransactionAsInitiated(
            SteamTransactionEntity transaction,
            ulong assignedTransactionId
        )
        {
            transaction.State = SteamTransactionState.Initiated;
            transaction.TransactionId = assignedTransactionId;
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
                throw new ArgumentException(
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
        /// Finds a transaction by its order ID and entity ID
        /// (both for better security - entity ID can be guessed, both cannot)
        /// </summary>
        private SteamTransactionEntity FindTransaction(
            ulong orderId,
            string entityId
        )
        {
            var transaction = DB.Find<SteamTransactionEntity>(entityId);
            
            if (transaction == null || transaction.OrderId != orderId)
                throw new ArgumentException(
                    $"No transaction with ID {entityId} and " +
                    $"order id {orderId} was found."
                );

            return transaction;
        }

        /// <summary>
        /// Captures unexpected exceptions and sets the transaction state
        /// to "exception" and stores the exception within the entity.
        /// The SteamApiFailureException is handled differently and the
        /// entity state is set to the argument provided to this method.
        /// </summary>
        /// <param name="transaction">The transaction entity being worked on</param>
        /// <param name="transactionStateAfterFailure">
        /// What state should the entity transition to when
        /// a SteamApiFailureException exception occurs.
        /// </param>
        /// <param name="action">The code being observed for exceptions</param>
        private async Task CaptureExceptions(
            SteamTransactionEntity transaction,
            string transactionStateAfterFailure,
            Func<Task> action
        )
        {
            try
            {
                await action.Invoke();
            }
            catch (SteamApiFailureException e)
            {
                // store the Steam API failure in the transaction
                transaction.State = transactionStateAfterFailure;
                transaction.ErrorCode = e.ErrorCode;
                transaction.ErrorDescription = e.ErrorDescription;
                transaction.Save();
                
                // let the exception propagate upward
                throw;
            }
            catch (Exception e)
            {
                // do nothing, if the transaction already had an exception
                if (transaction.State == SteamTransactionState.Exception)
                    throw;
                
                // store the exception
                transaction.Exception = Serializer.ToJson<Exception>(e);
                transaction.StateBeforeException = transaction.State;
                transaction.State = SteamTransactionState.Exception;
                transaction.Save();
                
                // let the exception propagate upward
                throw;
            }
        }
    }
}