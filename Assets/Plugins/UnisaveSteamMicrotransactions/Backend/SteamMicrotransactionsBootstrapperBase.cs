using Unisave.Bootstrapping;
using Unisave.Foundation;

namespace Unisave.SteamMicrotransactions
{
    public class SteamMicrotransactionsBootstrapperBase : Bootstrapper
    {
        // run in between the framework and the user
        public override int StageNumber => BootstrappingStage.Modules;
        
        public override void Main()
        {
            // parse config and register it into the service container
            EnvStore env = Services.Resolve<EnvStore>();
            var config = SteamMicrotransactionsConfig.ParseFromEnv(env);
            Services.RegisterInstance(config);
            
            // start the scheduler on another thread-pool thread
            // (will be stopped during service disposal)
            if (config.SchedulerEnabled)
            {
                Services.RegisterSingleton<SimpleScheduler>();
                var scheduler = Services.Resolve<SimpleScheduler>();
                scheduler.Start();
            }
        }
    }
}