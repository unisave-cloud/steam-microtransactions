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
        /// <summary>
        /// Base URL to the Steam servers
        /// </summary>
        public string SteamApiUrl { get; private set; }
        
        /// <summary>
        /// ID of your game in Steam
        /// </summary>
        public string SteamAppId { get; private set; }
        
        /// <summary>
        /// Your secret key to access the Steam API servers
        /// </summary>
        public string SteamPublisherKey { get; private set; }
        
        /// <summary>
        /// Whether to use the Steam's sandbox API (for testing) or the real one
        /// </summary>
        public bool UseSandbox { get; private set; }
        
        /// <summary>
        /// Is the scheduler responsible for Steam MTX logic enabled?
        /// </summary>
        public bool SchedulerEnabled { get; private set; }
        
        /// <summary>
        /// For how many seconds after worker startup should the scheduler wait,
        /// before it executes its first tick.
        /// </summary>
        public int SchedulerDelaySeconds { get; private set; }
        
        /// <summary>
        /// How many seconds should the scheduler wait in between ticks. 
        /// </summary>
        public int SchedulerPeriodSeconds { get; private set; }

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
                ),
                SchedulerEnabled = env.GetBool(
                    key: "STEAM_MTX_SCHEDULER_ENABLED",
                    defaultValue: true
                ),
                SchedulerDelaySeconds = env.GetInt(
                    key: "STEAM_MTX_SCHEDULER_DELAY_SECONDS",
                    defaultValue: 30
                ),
                SchedulerPeriodSeconds = env.GetInt(
                    key: "STEAM_MTX_SCHEDULER_PERIOD_SECONDS",
                    defaultValue: 15 * 60 // 15 mins
                )
            };
        }

        /// <summary>
        /// Validates values in the config and if they are missing,
        /// warnings are logged
        /// </summary>
        public void LogValidationWarnings()
        {
            if (string.IsNullOrEmpty(SteamAppId))
            {
                Log.Warning(
                    "The STEAM_APP_ID environment variable is " +
                    "missing. Steam API calls will fail due to authentication."
                );
            }
            
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