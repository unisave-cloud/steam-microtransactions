using System;
using System.Threading;
using System.Threading.Tasks;

namespace Unisave.SteamMicrotransactions.Steam
{
    /// <summary>
    /// Job that calls the GetReport Steam API for the transactions in the
    /// past 90 days (configurable) and appends this Steam-side info to
    /// transaction entities in the database.
    ///
    /// The GetReport API is documented here:
    /// https://partner.steamgames.com/doc/webapi/ISteamMicroTxn#GetReport
    /// </summary>
    public class ProcessSteamReportJob
    {
        private readonly SteamMicrotransactionsConfig config;

        public ProcessSteamReportJob(SteamMicrotransactionsConfig config)
        {
            this.config = config;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            // this value sweeps across time from past to present
            DateTime processedUntil = DateTime.UtcNow.Subtract(
                TimeSpan.FromDays(config.ReconcileTransactionsYoungerThanDays)
            );

            while (true)
            {
                // -> Call GetReport API from that time and retrieve results
                
                // -> check cancellation
                
                // -> Process these results (write to the DB)
                
                // -> check cancellation
                
                // -> If there is only one or no result, break the loop
                break;

                // -> Advance the processedUntil to the time of the last result
            }
        }
    }
}