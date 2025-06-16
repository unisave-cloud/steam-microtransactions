using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Steamworks;
using Unisave.Facets;
using UnityEngine;

namespace Unisave.SteamMicrotransactions
{
    /// <summary>
    /// Allows you to interact with steam microtransactions from mono behaviours
    /// </summary>
    public static class MonoBehaviourExtensions
    {
        /// <summary>
        /// Get resolved when the Steamworks callback for transaction
        /// finalization is invoked
        /// </summary>
        private static TaskCompletionSource<TransactionResult> callbackTcs = null;
        
        /// <summary>
        /// The Steamworks callback for transaction finalization
        /// </summary>
        private static Callback<MicroTxnAuthorizationResponse_t> callback;

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
        public static UnisaveOperation<TransactionResult> DoTheSteamMicrotransactionUiFlow(
            this MonoBehaviour monoBehaviour,
            SteamTransactionEntity transactionProposal
        ) => new UnisaveOperation<TransactionResult>(monoBehaviour, async () =>
        {
            if (!SteamManagerProxy.Initialized)
            {
                throw new InvalidOperationException(
                    "SteamManager must be initialized before " +
                    "initiating a microtransaction."
                );
            }
            
            if (callbackTcs != null)
            {
                throw new InvalidOperationException(
                    "Only one Steam microtransaction can be handled at a time."
                );
            }
            
            try
            {
                callbackTcs = new TaskCompletionSource<TransactionResult>();
                RegisterCallback();
                
                await monoBehaviour.CallFacet((SteamPurchasingServerFacet f) =>
                    f.InitiateTransaction(transactionProposal)
                );
                
                return await callbackTcs.Task;
            }
            catch (Exception e)
            {
                return TransactionResult.FromException(e);
            }
            finally
            {
                DisposeCallback();
                callbackTcs = null;
            }
        });
        
        /// <summary>
        /// This method is called by Steamworks when the transaction finishes
        /// (player either authorized or aborted the transaction)
        /// </summary>
        public static async void SteamworksCallbackHandler(
            MicroTxnAuthorizationResponse_t response
        )
        {
            if (callbackTcs == null)
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
                callbackTcs.SetResult(TransactionResult.FromException(e));
                return;
            }
        
            // transaction has been aborted by the player
            if (response.m_bAuthorized != 1)
            {
                callbackTcs.SetResult(TransactionResult.FromAbort());
                return;
            }

            // everything went according to plans
            callbackTcs.SetResult(TransactionResult.FromSuccess(transaction));
        }

        private static void RegisterCallback()
        {
            if (callback != null)
            {
                throw new InvalidOperationException(
                    "Cannot register callback while it is already registered."
                );
            }
            
            callback = Callback<MicroTxnAuthorizationResponse_t>
                .Create(SteamworksCallbackHandler);
        }

        private static void DisposeCallback()
        {
            if (callback != null)
            {
                callback.Dispose();
                callback = null;
            }
        }
    }
}