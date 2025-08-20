using System;
using System.Threading;
using System.Threading.Tasks;

namespace Unisave.SteamMicrotransactions
{
    /// <summary>
    /// Implements a simple scheduler that executes periodic code
    /// for the Steam MTX module. In the future, this should be replaced
    /// by a centralized Unisave scheduler with better error handling.
    /// </summary>
    public class SimpleScheduler : IDisposable
    {
        /// <summary>
        /// Time before the first tick
        /// </summary>
        private readonly TimeSpan delay;
        
        /// <summary>
        /// Time in between ticks
        /// </summary>
        private readonly TimeSpan period;
        
        /// <summary>
        /// The task that represents the scheduler's loop
        /// </summary>
        private Task schedulerTask = null;
        
        /// <summary>
        /// This must be called to cancel the scheduler loop
        /// </summary>
        private readonly CancellationTokenSource schedulerTokenSource
            = new CancellationTokenSource();

        public SimpleScheduler(SteamMicrotransactionsConfig config)
        {
            delay = TimeSpan.FromSeconds(config.SchedulerDelaySeconds);
            period = TimeSpan.FromSeconds(config.SchedulerPeriodSeconds);
        }

        public void Start()
        {
            schedulerTask = Task.Run(MainLoop);
        }
        
        public void Dispose()
        {
            Console.WriteLine("[SteamMTX SimpleScheduler]: Stopping...");
            
            // cancel the scheduler
            schedulerTokenSource.Cancel();
            
            // wait for the scheduler to complete
            try
            {
                if (schedulerTask != null)
                    schedulerTask.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                // do nothing
            }
            catch (Exception e)
            {
                Console.WriteLine("[SteamMTX SimpleScheduler]: " + e);
            }
            
            Console.WriteLine("[SteamMTX SimpleScheduler]: Stopped.");
        }
        
        private async Task MainLoop()
        {
            Console.WriteLine("[SteamMTX SimpleScheduler]: Running.");
            
            await Task.Delay(
                TimeSpan.FromSeconds(10),
                schedulerTokenSource.Token
            );
            
            while (!schedulerTokenSource.Token.IsCancellationRequested)
            {
                Console.WriteLine("[SteamMTX SimpleScheduler]: Executing body.");

                try
                {
                    OnTickBody();
                }
                catch (OperationCanceledException)
                {
                    // do nothing
                }
                catch (Exception e)
                {
                    Console.WriteLine("[SteamMTX SimpleScheduler]: " + e);
                }
                
                await Task.Delay(
                    TimeSpan.FromSeconds(30),
                    schedulerTokenSource.Token
                );
            }
        }

        private void OnTickBody()
        {
            // here we do the interesting logic
        }
    }
}