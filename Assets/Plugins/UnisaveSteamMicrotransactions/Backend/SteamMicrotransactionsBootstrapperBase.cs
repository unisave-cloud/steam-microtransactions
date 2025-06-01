using Unisave.Bootstrapping;
using Unisave.Foundation;

namespace Unisave.SteamMicrotransactions
{
    public class SteamMicrotransactionsBootstrapperBase : Bootstrapper
    {
        public override void Main()
        {
            // parse config and register it into the service container
            EnvStore env = Services.Resolve<EnvStore>();
            var config = SteamMicrotransactionsConfig.ParseFromEnv(env);
            Services.RegisterInstance(config);
        }
    }
}