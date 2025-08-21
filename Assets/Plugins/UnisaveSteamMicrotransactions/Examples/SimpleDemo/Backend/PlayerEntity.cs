using Unisave.Entities;

namespace Unisave.SteamMicrotransactions.Steam.Examples.SimpleDemo
{
    [EntityCollectionName("players")]
    public class PlayerEntity : Entity
    {
        /// <summary>
        /// Name of the player, e.g. "John Doe"
        /// </summary>
        public string name;

        /// <summary>
        /// Number of gold coins that the player owns
        /// </summary>
        public int goldCoins = 0;
        
        /// <summary>
        /// Does the player have the premium account purchased?
        /// </summary>
        public bool hasPremium = false;
    }
}