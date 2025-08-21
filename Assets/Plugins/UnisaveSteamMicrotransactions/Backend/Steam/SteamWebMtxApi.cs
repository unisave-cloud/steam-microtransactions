using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LightJson;
using Unisave.HttpClient;
using Unisave.Utils;

namespace Unisave.SteamMicrotransactions.Steam.Steam
{
    /// <summary>
    /// Abstracts away access to the Steam ISteamMicroTxn API
    /// </summary>
    public class SteamWebMtxApi
    {
        /// <summary>
        /// The HTTP client used to make web requests
        /// </summary>
        private readonly IHttp http;
        
        /// <summary>
        /// Configuration data
        /// </summary>
        private readonly SteamMicrotransactionsConfig config;

        public SteamWebMtxApi(IHttp http, SteamMicrotransactionsConfig config)
        {
            this.http = http;
            this.config = config;
        }
        
        /// <summary>
        /// Calls the InitTxn/v3/ endpoint, for which Steam says:
        /// Creates a new purchase. Send the order information along with
        /// the Steam ID to seed the transaction on Steam.
        /// </summary>
        /// <param name="transaction">
        /// The request body is constructed from a proposed transaction entity
        /// </param>
        /// <returns>The transaction ID assigned by Steam</returns>
        public async Task<ulong> InitTxn(SteamTransactionEntity transaction)
        {
            // https://partner.steamgames.com/doc/webapi/ISteamMicroTxn#InitTxn

            var response = await http.PostAsync(
                GetBaseUrl() + "InitTxn/v3/",
                BuildInitTxnRequestBody(transaction)
            );
            
            await SteamApiHttpException.ThrowIfHttpIsNon200Async(
                "Steam API InitTxn/v3/ endpoint invocation failed:",
                response
            );

            JsonObject responseJson = await response.JsonAsync();
            
            SteamApiFailureException.ThrowIfApiFailure(
                "Steam API InitTxn/v3/ endpoint returned a failure:",
                responseJson
            );

            // return the assigned transaction ID
            return ulong.Parse(
                responseJson["response"]["params"]["transid"].AsString
            );
        }
        
        private Dictionary<string, string> BuildInitTxnRequestBody(
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

        /// <summary>
        /// Calls the FinalizeTxn/v2/ endpoint, for which Steam says:
        /// Completes a purchase that was started by the InitTxn API.
        /// </summary>
        /// <param name="orderId">
        /// Order ID of the transaction to finalize
        /// </param>
        public async Task FinalizeTxn(ulong orderId)
        {
            // https://partner.steamgames.com/doc/webapi/ISteamMicroTxn#FinalizeTxn

            var response = await http.PostAsync(
                GetBaseUrl() + "FinalizeTxn/v2/",
                new Dictionary<string, string>
                {
                    ["key"] = config.SteamPublisherKey.ToString(),
                    ["orderid"] = orderId.ToString(),
                    ["appid"] = config.SteamAppId.ToString(),
                }
            );

            await SteamApiHttpException.ThrowIfHttpIsNon200Async(
                "Steam API FinalizeTxn/v2/ endpoint invocation failed:",
                response
            );

            JsonObject responseJson = await response.JsonAsync();
            
            SteamApiFailureException.ThrowIfApiFailure(
                "Steam API FinalizeTxn/v2/ endpoint returned a failure:",
                responseJson
            );
            
            // nothing interesting to return
        }

        /// <summary>
        /// Calls the "GetReport/v5/" API endpoint, for which Steam says:
        /// Steam offers transaction reports that can be downloaded for
        /// reconciliation purposes. These reports show detailed information
        /// about each transaction that affects the settlement of funds into
        /// your accounts.
        /// </summary>
        /// <param name="type">
        /// Report type (One of: "GAMESALES", "STEAMSTORESALES", "SETTLEMENT",
        /// "CHARGEBACK", "SUBSCRIPTION")
        /// Note: Apparently this just filters the returned transactions by their
        /// status. Using GAMESALES returns all transaction made from within
        /// the game.
        /// </param>
        /// <param name="time">Since when (UTC) start listing transactions</param>
        /// <param name="maxResults">
        /// Maximum number of items to return, should be between 1K and 10K.
        /// </param>
        public async Task GetReport(string type, DateTime time, int maxResults)
        {
            // https://partner.steamgames.com/doc/webapi/ISteamMicroTxn#GetReport
            
            var response = await http.GetAsync(
                GetBaseUrl() + "GetReport/v5/",
                new Dictionary<string, string>()
                {
                    ["key"] = config.SteamPublisherKey,
                    ["appid"] = config.SteamAppId,
                    ["type"] = type,
                    ["time"] = time.ToString("yyyy-MM-dd'T'HH:mm:ss.ffK"),
                    ["maxresults"] = maxResults.ToString()
                }
            );

            await SteamApiHttpException.ThrowIfHttpIsNon200Async(
                "Steam API GetReport/v5/ endpoint invocation failed:",
                response
            );

            JsonObject responseJson = await response.JsonAsync();
            
            SteamApiFailureException.ThrowIfApiFailure(
                "Steam API GetReport/v5/ endpoint returned a failure:",
                responseJson
            );

            // TODO: parse the response and return
        }
        
        /// <summary>
        /// URL of the Steam microtransactions API, ending with a slash,
        /// basically this: "https://partner.steam-api.com/ISteamMicroTxn/",
        /// with paying attention to the sandbox mode.
        /// </summary>
        private string GetBaseUrl()
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