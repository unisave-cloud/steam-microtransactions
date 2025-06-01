using Unisave.Facades;
using Unisave.Foundation;

/*
 * Environment variables needed:
 * 
 * STEAM_API_URL=https://partner.steam-api.com/
 * STEAM_APP_ID=480
 * STEAM_PUBLISHER_KEY=secret
 * STEAM_USE_MICROTRANSACTION_SANDBOX=false
 */

namespace Unisave.SteamMicrotransactions
{
    /// <summary>
    /// Server-side configuration for the Steam microtransactions module
    /// </summary>
    public class SteamMicrotransactionsConfig
    {
        public string SteamApiUrl { get; private set; }
        
        public string SteamAppId { get; private set; }
        
        public string SteamPublisherKey { get; private set; }
        
        public bool UseSandbox { get; private set; }

        public static SteamMicrotransactionsConfig ParseFromEnv(EnvStore env)
        {
            return new SteamMicrotransactionsConfig
            {
                SteamApiUrl = env.GetString(
                    key: "STEAM_API_URL",
                    defaultValue: "https://partner.steam-api.com/"
                ),
                SteamAppId = env.GetString("STEAM_APP_ID"),
                SteamPublisherKey = env.GetString("STEAM_PUBLISHER_KEY"),
                UseSandbox = env.GetBool(
                    key: "STEAM_USE_MICROTRANSACTION_SANDBOX",
                    defaultValue: true
                )
            };
        }

        /// <summary>
        /// Validates values in the config and if they are missing,
        /// warnings are logged
        /// </summary>
        public void LogValidationWarnings()
        {
            if (string.IsNullOrEmpty(SteamPublisherKey))
            {
                Log.Warning(
                    "The STEAM_PUBLISHER_KEY environment variable is " +
                    "missing. Steam API calls will fail due to authentication."
                );
            }
        }
    }
}