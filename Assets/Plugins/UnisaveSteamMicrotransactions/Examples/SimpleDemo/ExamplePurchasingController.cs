using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Steamworks;
using TMPro;
using Unisave.Facets;
using Unisave.Serialization;
using UnityEngine;
using UnityEngine.UI;

namespace Unisave.SteamMicrotransactions.Steam.Examples.SimpleDemo
{
    public class ExamplePurchasingController : MonoBehaviour
    {
        /// <summary>
        /// When true, the warning dialog is rendered as open
        /// (when the UpdateUI method is called)
        /// </summary>
        private bool isWarningDialogOpen;
        
        /// <summary>
        /// The logged-in player's data, null if not logged in
        /// </summary>
        private PlayerEntity loggedInPlayer;

        /// <summary>
        /// The language to use for the purchase
        /// </summary>
        private string language;

        /// <summary>
        /// The currency to use for the purchase
        /// </summary>
        private string currency;

        // information about products, downloaded from the server
        private LocalizedProductInfo goldSackInfo;
        private LocalizedProductInfo premiumAccountInfo;
        
        // references to UI objects so that they can be controlled
        public GameObject unityEditorWarningPanel;
        public GameObject cardsContainer;
        public GameObject guidePanel;
        public Button loginButton;
        public TMP_Text guideText;
        public TMP_Text playerDataText;
        public Button resetPlayerButton;
        public Button purchasePremiumButton;
        public TMP_Text goldSackPriceText;
        public TMP_Text premiumAccountPriceText;
        
        void Start()
        {
            // In your game, place the SteamManager manually into the scene
            // instead of doing this call:
            SteamManagerProxy.EnsureExistsInScene();

            // Show the warning message if the scene is launched from Unity
            isWarningDialogOpen = Application.isEditor;
            
            // initialize the state
            loggedInPlayer = null;
            language = "en";
            currency = "USD";

            UpdateUI();
        }

        public void OnCloseUnityEditorWarningDialogButtonClicked()
        {
            isWarningDialogOpen = false;
            UpdateUI();
        }

        public async void OnLoginButtonClicked()
        {
            loginButton.interactable = false;
            
            guideText.text = "Logging in...\n";
            await this.CallFacet((DummyAuthFacet f) => f.LoginAsJohnDoe());
            
            guideText.text += "Fetching player...\n";
            loggedInPlayer = await this.CallFacet(
                (DummyAuthFacet f) => f.WhoAmI()
            );
            
            guideText.text += "Fetching product information...\n";
            goldSackInfo = await this.DownloadProductInfo<GoldSackProduct>(
                currency: currency,
                language: language
            );
            premiumAccountInfo = await this.DownloadProductInfo<PremiumAccountProduct>(
                currency: currency,
                language: language
            );
            
            guideText.text = $"Logged in as: {loggedInPlayer.name}\n" +
                             $"({loggedInPlayer.EntityId})";
            UpdateUI();
        }

        public async void OnResetPlayerButtonClicked()
        {
            guideText.text = "Resetting player data...\n";
            
            loggedInPlayer = await this.CallFacet(
                (DummyAuthFacet f) => f.ResetPlayerData()
            );
            UpdateUI();
            
            guideText.text += "Done.\n";
        }
        
        public async void OnBuyGoldButtonClicked()
        {
            var transactionProposal = SteamTransactionEntity.NewProposal(
                playerSteamId: SteamUser.GetSteamID().m_SteamID,
                language: language,
                currency: currency
            );
            transactionProposal.AddItem(goldSackInfo, quantity: 1);

            await DoTheFlow(transactionProposal);
        }

        public async void OnBuyPremiumButtonClicked()
        {
            var transactionProposal = SteamTransactionEntity.NewProposal(
                playerSteamId: SteamUser.GetSteamID().m_SteamID,
                language: language,
                currency: currency
            );
            transactionProposal.AddItem(premiumAccountInfo, quantity: 1);

            await DoTheFlow(transactionProposal);
        }

        /// <summary>
        /// Passes a proposed Steam transaction through the system, updates
        /// the UI accordingly and fetches the new player data on success.
        /// </summary>
        private async Task DoTheFlow(
            SteamTransactionEntity transactionProposal
        )
        {
            guideText.text = "Opening Steam Overlay...\n";
            
            var flowResult = await this.DoSteamCheckoutFlow(
                transactionProposal
            );

            // The player just closed the Steam Overlay without paying.
            if (flowResult.WasAborted)
            {
                guideText.text += "Player aborted the transaction.\n";
                return;
            }
            
            // There was some problem with the transaction.
            // Show the error to the player so that he can send you a screenshot.
            if (flowResult.WasError)
            {
                guideText.text += "TRANSACTION ERROR. See the Unity console.\n";
                Debug.LogError("TRANSACTION ERROR: " + flowResult.ErrorMessage);
                return;
            }

            // The player finalized the transaction, purchased items have been
            // added to their account, we now need to reload the account.
            guideText.text += "Transaction was successful.\n";
            Debug.Log(
                "The completed transaction entity: " +
                Serializer.ToJsonString(flowResult.Transaction)
            );
            guideText.text += "Reloading the player...\n";
            loggedInPlayer = await this.CallFacet(
                (DummyAuthFacet f) => f.WhoAmI()
            );
            guideText.text += "Done.\n";
            
            UpdateUI();
        }

        /// <summary>
        /// Updates the user interface to match the fields in this behaviour
        /// </summary>
        private void UpdateUI()
        {
            // the warning dialog
            if (isWarningDialogOpen)
            {
                unityEditorWarningPanel.SetActive(true);
                guidePanel.SetActive(false);
                loginButton.gameObject.SetActive(false);
                cardsContainer.SetActive(false);
                return;
            }
            
            // the login screen
            if (loggedInPlayer == null)
            {
                unityEditorWarningPanel.SetActive(false);
                guidePanel.SetActive(true);
                loginButton.gameObject.SetActive(true);
                cardsContainer.SetActive(false);
                return;
            }
            
            // === now we are in the logged-in screen ===
            
            // update visibility of top-level containers
            unityEditorWarningPanel.SetActive(false);
            loginButton.gameObject.SetActive(false);
            guidePanel.SetActive(true);
            cardsContainer.SetActive(true);
            
            // update cards content
            playerDataText.text = $"{loggedInPlayer.name}\n" +
                                  $"Gold: {loggedInPlayer.goldCoins}\n" + 
                                  $"Premium: {loggedInPlayer.hasPremium}";
            goldSackPriceText.text = $"{goldSackInfo.UnitCost} {goldSackInfo.Currency}";
            premiumAccountPriceText.text = $"{premiumAccountInfo.UnitCost} {premiumAccountInfo.Currency}";
            resetPlayerButton.interactable = loggedInPlayer.hasPremium
                                             || loggedInPlayer.goldCoins > 0;
            purchasePremiumButton.interactable = !loggedInPlayer.hasPremium;
        }
    }
}