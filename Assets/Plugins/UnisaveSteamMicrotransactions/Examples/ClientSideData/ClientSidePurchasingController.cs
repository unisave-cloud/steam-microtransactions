using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Unisave.SteamMicrotransactions.Steam.Examples.ClientSideData
{
    public class ClientSidePurchasingController : MonoBehaviour
    {
        /// <summary>
        /// When true, the warning dialog is rendered as open
        /// (when the UpdateUI method is called)
        /// </summary>
        private bool isWarningDialogOpen;
        
        /// <summary>
        /// The language to use for the purchase
        /// </summary>
        private string language;

        /// <summary>
        /// The currency to use for the purchase
        /// </summary>
        private string currency;

        // information about products, downloaded from the server
        private LocalizedProductInfo diamondsInfo;
        
        // references to UI objects so that they can be controlled
        public GameObject unityEditorWarningPanel;
        public GameObject cardsContainer;
        public GameObject guidePanel;
        public TMP_Text guideText;
        public TMP_Text playerDataText;
        public Button resetPlayerButton;
        public Button purchaseDiamondsButton;
        public TMP_Text diamondsPriceText;
        
        async void Start()
        {
            // In your game, place the SteamManager manually into the scene
            // instead of doing this call:
            SteamManagerProxy.EnsureExistsInScene();
            
            // Show the warning message if the scene is launched from Unity
            isWarningDialogOpen = Application.isEditor;

            // initialize the state
            language = "en";
            currency = "USD";
            UpdateUI();

            await FetchProductInfo();
        }
        
        public void OnCloseUnityEditorWarningDialogButtonClicked()
        {
            isWarningDialogOpen = false;
            UpdateUI();
        }

        private async Task FetchProductInfo()
        {
            guideText.text = "Fetching product information...\n";
            diamondsInfo = await this.DownloadProductInfo<TwentyDiamondsProduct>(
                currency: currency,
                language: language
            );
            
            guideText.text += "Ready.\n";
            UpdateUI();
        }

        public void OnResetPlayerButtonClicked()
        {
            PlayerPrefs.SetInt("PlayerDiamonds", 0);
            PlayerPrefs.Save();
            
            guideText.text = "Player data was reset.\n";
            UpdateUI();
        }
        
        public async void OnBuyDiamondsButtonClicked()
        {
            var transactionProposal = SteamTransactionEntity.NewProposal(
                playerSteamId: SteamUser.GetSteamID().m_SteamID,
                language: language,
                currency: currency
            );
            transactionProposal.AddItem(diamondsInfo, quantity: 1);

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

            // The player finalized the transaction, it has been paid.
            // Now we need to give products to the player here, client-side:
            guideText.text += "Transaction was successful.\n";
            guideText.text += "Giving products to the player...\n";
            
            // this wrapper reports success and exceptions to server
            await this.GiveProductsToPlayerClientSide(flowResult, () =>
            {
                // give 20 diamonds
                int playerDiamonds = PlayerPrefs.GetInt("PlayerDiamonds", 0);
                PlayerPrefs.SetInt("PlayerDiamonds", playerDiamonds + 20);
                PlayerPrefs.Save();
            });
            
            guideText.text += "Done.\n";
            
            UpdateUI();
        }

        /// <summary>
        /// Updates the user interface to match the fields in this behaviour
        /// </summary>
        private void UpdateUI()
        {
            int playerDiamonds = PlayerPrefs.GetInt("PlayerDiamonds", 0);
            
            // the warning dialog
            if (isWarningDialogOpen)
            {
                unityEditorWarningPanel.SetActive(true);
                guidePanel.SetActive(false);
                cardsContainer.SetActive(false);
                return;
            }
            
            // update visibility of top-level containers
            unityEditorWarningPanel.SetActive(false);
            guidePanel.SetActive(true);
            cardsContainer.SetActive(true);
            
            // update cards content
            playerDataText.text = $"Diamonds: {playerDiamonds}";
            diamondsPriceText.text = $"{diamondsInfo?.UnitCost} {diamondsInfo?.Currency}";
            resetPlayerButton.interactable = playerDiamonds > 0;
            purchaseDiamondsButton.interactable = diamondsInfo != null;
        }
    }
}