using Unisave.Facades;
using Unisave.Facets;

namespace Unisave.SteamMicrotransactions.Examples.SimpleDemo
{
    /// <summary>
    /// Dummy facet that performs login for the John Doe player
    /// </summary>
    public class DummyAuthFacet : Facet
    {
        /// <summary>
        /// Finds or creates the John Doe player and logs him in.
        /// </summary>
        public void LoginAsJohnDoe()
        {
            // find John
            var john = DB.TakeAll<PlayerEntity>()
                .Filter(e => e.name == "John Doe")
                .First();

            // register John if he does not exist
            if (john == null)
            {
                john = new PlayerEntity() {
                    name = "John Doe",
                };
                john.Save();
            }

            // login as John
            Auth.Login(john);
        }

        /// <summary>
        /// Returns the currently logged-in player
        /// </summary>
        public PlayerEntity WhoAmI()
        {
            return Auth.GetPlayer<PlayerEntity>();
        }
    }
}