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
        public Button loginButton;
        public TMP_Text guideText;
        
        void Start()
        {
            // In your game, place the SteamManager manually into the scene
            // instead of doing this call:
            SteamManagerProxy.EnsureExistsInScene();
        }

        public async void OnLoginButtonClicked()
        {
            loginButton.interactable = false;
            
            guideText.text = "Logging in...\n";
            await this.CallFacet((DummyAuthFacet f) => f.LoginAsJohnDoe());
            
            guideText.text = "Fetching player...\n";
            var john = await this.CallFacet((DummyAuthFacet f) => f.WhoAmI());
            
            guideText.text = $"Logged in as: {john.name}\n({john.EntityId})";
        }

        public async void OnBuyGoldButtonClicked()
        {
            // TODO: update UI
            guideText.text = "Openning Steam Overlay...\n";
            
            var transactionProposal = new SteamTransactionEntity {
                playerSteamId = SteamUser.GetSteamID().m_SteamID,
                language = "en",
                currency = "USD"
            };
            // TODO: gold pack product
            transactionProposal.AddProduct<ExampleVirtualProduct>(quantity: 3);

            var result = await this.InitiateSteamMicrotransaction(
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
                // TODO: reload player account
                return;
            }
            
            // Else - There was an unexpected error. Show that error to the
            // player so that they can send you a screenshot.
            guideText.text += "TRANSACTION ERROR. See the Unity console.\n";
            Debug.LogError("TRANSACTION ERROR: " + result.Error);
        }
    }
}