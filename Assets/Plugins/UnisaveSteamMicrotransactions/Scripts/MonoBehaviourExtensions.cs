using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LightJson;
using Steamworks;
using Unisave.Facets;
using Unisave.Serialization;
using UnityEngine;

namespace Unisave.SteamMicrotransactions.Steam
{
    /// <summary>
    /// Allows you to interact with steam microtransactions from mono behaviours
    /// </summary>
    public static class MonoBehaviourExtensions
    {
        /// <summary>
        /// Downloads metadata about a steam product, localized into the
        /// specified currency and language.
        /// </summary>
        public static async Task<LocalizedProductInfo> DownloadProductInfo<TProduct>(
            this MonoBehaviour monoBehaviour,
            string currency,
            string language
        ) where TProduct : SteamProduct
        {
            List<string> typeNames = new List<string>() {
                typeof(TProduct).FullName
            };
            
            var infos = await monoBehaviour.CallFacet(
                (SteamPurchasingServerFacet f) => f.DownloadProductsInfo(
                    currency,
                    language,
                    typeNames
                )
            );
            
            return infos[0];
        }
        
        #region "Checkout flow and its callbacks"
        
        /// <summary>
        /// For how many seconds should we wait for the Steam Overlay to open?
        /// (just open, not close - that depends on the player)
        /// This is used to detect issues with the Steam Overlay not opening,
        /// for example during faulty Steam Client communication.
        /// </summary>
        public const int OverlayOpeningTimeoutSeconds = 30_000;
        
        /// <summary>
        /// Gets resolved when the steam overlay is displayed
        /// (cancellation is handled in parallel with Task.Delay)
        /// </summary>
        private static TaskCompletionSource<object> overlayCallbackTcs = null;
        
        /// <summary>
        /// The Steamworks callback for Steam Overlay openning/closing
        /// </summary>
        private static Callback<GameOverlayActivated_t> overlayCallback;
        
        /// <summary>
        /// Get resolved when the Steamworks callback for transaction
        /// finalization is invoked (the authorization callback)
        /// </summary>
        private static TaskCompletionSource<CheckoutFlowResult> authorizationCallbackTcs = null;
        
        /// <summary>
        /// The Steamworks callback for transaction finalization
        /// </summary>
        private static Callback<MicroTxnAuthorizationResponse_t> authorizationCallback;
        
        /// <summary>
        /// Accepts a Steam microtransaction proposal as an argument and
        /// performs the UI flow with the player. First, it sends the proposal
        /// to the Unisave steam purchasing server, initiates the transaction.
        /// This opens the Steam Overlay UI. They player now either checks out
        /// or aborts. Then the transaction is finalized and if successful,
        /// the purchased items are given to the player. Then this method
        /// returns with a description of what happened.
        /// </summary>
        /// <param name="monoBehaviour"></param>
        /// <param name="transactionProposal"></param>
        /// <returns></returns>
        public static UnisaveOperation<CheckoutFlowResult> DoSteamCheckoutFlow(
            this MonoBehaviour monoBehaviour,
            SteamTransactionEntity transactionProposal
        ) => new UnisaveOperation<CheckoutFlowResult>(monoBehaviour, async () =>
        {
            if (!SteamManagerProxy.Initialized)
            {
                throw new InvalidOperationException(
                    "SteamManager must be initialized before " +
                    "initiating a microtransaction."
                );
            }
            
            if (authorizationCallbackTcs != null)
            {
                throw new InvalidOperationException(
                    "Only one Steam microtransaction can be handled at a time."
                );
            }
            
            try
            {
                overlayCallbackTcs = new TaskCompletionSource<object>();
                authorizationCallbackTcs = new TaskCompletionSource<CheckoutFlowResult>();
                RegisterCallbacks();
                
                var transaction = await monoBehaviour.CallFacet(
                    (SteamPurchasingServerFacet f) =>
                        f.InitiateTransaction(transactionProposal)
                );

                await WaitForOverlayToOpen(monoBehaviour, transaction);
                
                // wait for the player to close the overlay
                return await authorizationCallbackTcs.Task;
            }
            catch (Exception e)
            {
                return CheckoutFlowResult.FromException(e);
            }
            finally
            {
                DisposeCallbacks();
                authorizationCallbackTcs = null;
            }
        });

        private static async Task WaitForOverlayToOpen(
            MonoBehaviour monoBehaviour,
            SteamTransactionEntity transaction
        )
        {
            var overlayTask = overlayCallbackTcs.Task;
            var timeoutTask = Task.Delay(5_000);

            // wait for the first of them
            var finishedTask = await Task.WhenAny(overlayTask, timeoutTask);
            
            // handle successful opening
            if (finishedTask == overlayTask)
                return;
            
            // we timed out! Now we need to throw an exception and report it
            var exception = new TimeoutException(
                $"The Steam Overlay did not open in " +
                $"{OverlayOpeningTimeoutSeconds} after transaction " +
                $"initiation. There is likely some issue with communication " +
                $"between your game and the Steam client. Make sure your game " +
                $"was launched via the Steam client and that it has correct app ID."
            );
            JsonObject serializedException = Serializer.ToJson<Exception>(exception);
            serializedException.Remove("$type");
            
            // upload the exception to the server
            await monoBehaviour.CallFacet(
                (SteamPurchasingServerFacet f) => f.UploadClientException(
                    transaction.OrderId,
                    transaction.EntityId,
                    serializedException
                )
            );

            // throw the exception
            throw exception;
        }
        
        private static void OverlayCallbackHandler(GameOverlayActivated_t payload)
        {
            // wait only for the event, when the overlay is SHOWN and NOT by the USER
            if (payload.m_bActive == 1 && !payload.m_bUserInitiated)
            {
                // stop waiting for the overlay
                overlayCallbackTcs.SetResult(null);
            }
        }
        
        /// <summary>
        /// This method is called by Steamworks when the transaction finishes
        /// (player either authorized or aborted the transaction)
        /// </summary>
        private static async void SteamworksAuthorizationCallbackHandler(
            MicroTxnAuthorizationResponse_t response
        )
        {
            if (authorizationCallbackTcs == null)
            {
                Debug.LogWarning(
                    "Steamworks microtransaction callback was called, " +
                    "but no TCS is exists."
                );
                return;
            }
            
            // finish the transaction
            SteamTransactionEntity transaction;
            try
            {
                bool playerAuthorizedTheTransactionInSteamOverlay
                    = response.m_bAuthorized == 1;
                
                transaction = await FacetClient.CallFacet(
                    null, // no caller -> always return from the await call
                    (SteamPurchasingServerFacet f) => f.FinalizeTransaction(
                        response.m_ulOrderID,
                        playerAuthorizedTheTransactionInSteamOverlay
                    )
                );
            }
            catch (Exception e)
            {
                authorizationCallbackTcs.SetResult(CheckoutFlowResult.FromException(e));
                return;
            }
        
            // transaction has been aborted by the player
            if (response.m_bAuthorized != 1)
            {
                authorizationCallbackTcs.SetResult(CheckoutFlowResult.FromAbort());
                return;
            }

            // everything went according to plans
            authorizationCallbackTcs.SetResult(CheckoutFlowResult.FromSuccess(transaction));
        }

        private static void RegisterCallbacks()
        {
            if (authorizationCallback != null)
            {
                throw new InvalidOperationException(
                    "Cannot register callback while it is already registered."
                );
            }
            
            authorizationCallback = Callback<MicroTxnAuthorizationResponse_t>
                .Create(SteamworksAuthorizationCallbackHandler);
            overlayCallback = Callback<GameOverlayActivated_t>
                .Create(OverlayCallbackHandler);
        }

        private static void DisposeCallbacks()
        {
            if (authorizationCallback != null)
            {
                authorizationCallback.Dispose();
                authorizationCallback = null;
            }
            
            if (overlayCallback != null)
            {
                overlayCallback.Dispose();
                overlayCallback = null;
            }
        }
        
        #endregion

        /// <summary>
        /// Wrap your client-side product-granting code in this method to
        /// report exceptions and success to the server to update the
        /// transaction state appropriately. This should be done right after
        /// the DoSteamCheckoutFlow method returns and does so successfully.
        /// </summary>
        /// <param name="monoBehaviour"></param>
        /// <param name="flowResult">The result of the checkout flow, so that
        /// this method can read the transaction entity.</param>
        /// <param name="action">The code block that this method wraps,
        /// that actually grants the purchased products.</param>
        public static Task GiveProductsToPlayerClientSide(
            this MonoBehaviour monoBehaviour,
            CheckoutFlowResult flowResult,
            Action action
        )
        {
            return monoBehaviour.GiveProductsToPlayerClientSide(
                flowResult,
                () => {
                    action.Invoke();
                    return Task.CompletedTask;
                }
            );
        }
        
        /// <summary>
        /// Wrap your client-side product-granting code in this method to
        /// report exceptions and success to the server to update the
        /// transaction state appropriately. This should be done right after
        /// the DoSteamCheckoutFlow method returns and does so successfully.
        /// </summary>
        /// <param name="monoBehaviour"></param>
        /// <param name="flowResult">The result of the checkout flow, so that
        /// this method can read the transaction entity.</param>
        /// <param name="action">The code block that this method wraps,
        /// that actually grants the purchased products.</param>
        public static async Task GiveProductsToPlayerClientSide(
            this MonoBehaviour monoBehaviour,
            CheckoutFlowResult flowResult,
            Func<Task> action
        )
        {
            // check transaction was successful
            if (!flowResult.WasSuccess)
                throw new ArgumentException(
                    "Cannot give products to player because the " +
                    "transaction did not succeed."
                );
            
            // the transaction that we work with
            SteamTransactionEntity transaction = flowResult.Transaction;

            // check the transaction is in the proper state
            // (this should be the case, unless someone gave us weird data)
            if (transaction.State != SteamTransactionState.Completed)
                throw new InvalidOperationException(
                    "Only completed transactions can be client-side completed."
                );

            try
            {
                // give products to player
                Debug.Log(
                    "Giving products client-side to the player for " +
                    "transaction " + transaction.EntityId
                );
                
                await action.Invoke();
                
                Debug.Log("Products have been given client-side.");
            }
            catch (Exception e)
            {
                // prepare payload
                JsonObject exception = Serializer.ToJson(e);
                exception.Remove("$type");
                
                // upload the exception to the server
                await monoBehaviour.CallFacet(
                    (SteamPurchasingServerFacet f) => f.UploadClientException(
                        transaction.OrderId,
                        transaction.EntityId,
                        exception
                    )
                );
                
                // let the exception propagate upward
                throw;
            }
            
            // report success
            await monoBehaviour.CallFacet(
                (SteamPurchasingServerFacet f) => f.NotifyOfClientSideCompletion(
                    transaction.OrderId,
                    transaction.EntityId
                )
            );
        }
    }
}