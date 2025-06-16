using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Steamworks;
using TMPro;
using Unisave.Facets;
using UnityEngine;
using UnityEngine.UI;

namespace Unisave.SteamMicrotransactions.Examples.SimpleDemo
{
    public class ExamplePurchasingController : MonoBehaviour
    {
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

        // information about both products, downloaded from the server
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
            if (Application.isEditor)
                DisplayUnityEditorWarningDialog();
            else
                CloseUnityEditorWarningDialog();
            
            // initialize the state
            loggedInPlayer = null;
            language = "en";
            currency = "USD";
        }

        public async void OnLoginButtonClicked()
        {
            loginButton.interactable = false;
            
            guideText.text = "Logging in...\n";
            await this.CallFacet((DummyAuthFacet f) => f.LoginAsJohnDoe());
            
            guideText.text = "Fetching player...\n";
            loggedInPlayer = await this.CallFacet(
                (DummyAuthFacet f) => f.WhoAmI()
            );
            
            guideText.text = "Fetching product information...\n";
            goldSackInfo = await this.DownloadProductInfo<GoldSackProduct>(
                currency: currency,
                language: language
            );
            premiumAccountInfo = await this.DownloadProductInfo<PremiumAccountProduct>(
                currency: currency,
                language: language
            );
            
            // display the downloaded price
            goldSackPriceText.text = $"{goldSackInfo.UnitCost} {goldSackInfo.Currency}";
            premiumAccountPriceText.text = $"{premiumAccountInfo.UnitCost} {premiumAccountInfo.Currency}";
            
            UpdateLoggedInUI();
            
            guideText.text = $"Logged in as: {loggedInPlayer.name}\n" +
                             $"({loggedInPlayer.EntityId})";
        }

        public async void OnResetPlayerButtonClicked()
        {
            guideText.text = "Resetting player data...\n";
            
            loggedInPlayer = await this.CallFacet(
                (DummyAuthFacet f) => f.ResetPlayerData()
            );
            UpdateLoggedInUI();
            
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
            
            var result = await this.DoTheSteamMicrotransactionUiFlow(
                transactionProposal
            );

            // The player just closed the Steam Overlay without checking out.
            if (result.WasAborted)
            {
                guideText.text += "Player aborted the transaction.\n";
                return;
            }

            // The player finalized the transaction, purchased items have been
            // added to their account, we now need to reload the account.
            if (result.WasSuccess)
            {
                guideText.text += "Transaction was successful.\n";
                guideText.text += "Reloading the player...\n";
                loggedInPlayer = await this.CallFacet(
                    (DummyAuthFacet f) => f.WhoAmI()
                );
                UpdateLoggedInUI();
                guideText.text += "Done.\n";
                return;
            }
            
            // Else - There was an unexpected error. Show that error to the
            // player so that they can send you a screenshot.
            guideText.text += "TRANSACTION ERROR. See the Unity console.\n";
            Debug.LogError("TRANSACTION ERROR: " + result.Error);
        }

        private void DisplayUnityEditorWarningDialog()
        {
            unityEditorWarningPanel.SetActive(true);
            guidePanel.SetActive(false);
            loginButton.gameObject.SetActive(false);
            cardsContainer.SetActive(false);
        }

        public void CloseUnityEditorWarningDialog()
        {
            unityEditorWarningPanel.SetActive(false);
            guidePanel.SetActive(true);
            loginButton.gameObject.SetActive(true);
            cardsContainer.SetActive(false);
        }

        private void UpdateLoggedInUI()
        {
            // check that this method is not called in incorrect state
            if (loggedInPlayer == null)
            {
                Debug.LogError(
                    "Cannot render logged-in UI, no player is logged in."
                );
                return;
            }
            
            // update visibility of top-level containers
            unityEditorWarningPanel.SetActive(false);
            loginButton.gameObject.SetActive(false);
            guidePanel.SetActive(true);
            cardsContainer.SetActive(true);
            
            // update cards content
            playerDataText.text = $"{loggedInPlayer.name}\n" +
                                  $"Gold: {loggedInPlayer.goldCoins}\n" + 
                                  $"Premium: {loggedInPlayer.hasPremium}";
            resetPlayerButton.interactable = loggedInPlayer.hasPremium
                                             || loggedInPlayer.goldCoins > 0;
            purchasePremiumButton.interactable = !loggedInPlayer.hasPremium;
        }
    }
}