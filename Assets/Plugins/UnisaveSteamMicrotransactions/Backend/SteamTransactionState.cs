namespace Unisave.SteamMicrotransactions
{
    /// <summary>
    /// String-enum representing states that a steam transaction can be in.
    /// </summary>
    public static class SteamTransactionState
    {
        /// <summary>
        /// The transaction is being prepared, and it has not yet been initiated
        /// </summary>
        public const string BeingPrepared = "being-prepared";

        /// <summary>
        /// The transaction has been initiated, and now it waits
        /// for authentication by the player via the Steam app
        /// </summary>
        public const string Initiated = "initiated";

        /// <summary>
        /// The transaction initiation HTTP request to Steam failed,
        /// the transaction is dead now.
        /// </summary>
        public const string InitiationError = "initiation-error";

        /// <summary>
        /// The transaction has been authorized by the player (it has been paid)
        /// but the virtual products have not yet been given to the player
        /// </summary>
        public const string Authorized = "auhorized";

        /// <summary>
        /// The transaction has been aborted by the player (the player left the
        /// Steam Overlay UI without paying), the transaction is dead now.
        /// </summary>
        public const string Aborted = "aborted";

        /// <summary>
        /// The transaction finalization HTTP request to Steam failed,
        /// the transaction is dead now.
        /// </summary>
        public const string FinalizationError = "finalization-error";

        /// <summary>
        /// The purchased products have been given to the player,
        /// the transaction is dead now.
        /// </summary>
        public const string Completed = "completed";
    }
}