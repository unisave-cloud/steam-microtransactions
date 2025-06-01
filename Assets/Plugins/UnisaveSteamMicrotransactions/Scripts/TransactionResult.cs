using System;

namespace Unisave.SteamMicrotransactions
{
    /// <summary>
    /// Holds the result of a Steam microtransaction
    /// </summary>
    public class TransactionResult
    {
        /// <summary>
        /// Transaction entity at its latest known state
        /// </summary>
        public SteamTransactionEntity Transaction { get; set; }
        
        /// <summary>
        /// Contains the error message if there was an error
        /// </summary>
        public string Error { get; set; }
        
        /// <summary>
        /// The transaction finished completely
        /// </summary>
        public bool WasSuccess => Error == null;
        
        /// <summary>
        /// The transaction was aborted by the user
        /// (the user exited the Steam Overlay without finishing the checkout)
        /// </summary>
        public bool WasAborted { get; set; }

        public static TransactionResult FromSuccess(
            SteamTransactionEntity transaction
        )
        {
            return new TransactionResult() {
                Transaction = transaction,
                Error = null,
                WasAborted = false
            };
        }
        
        public static TransactionResult FromException(Exception e)
        {
            return new TransactionResult() {
                Transaction = null,
                Error = e.Message,
                WasAborted = false
            };
        }
        
        public static TransactionResult FromAbort()
        {
            return new TransactionResult() {
                Transaction = null,
                Error = "You've aborted the transaction.",
                WasAborted = true
            };
        }
    }
}