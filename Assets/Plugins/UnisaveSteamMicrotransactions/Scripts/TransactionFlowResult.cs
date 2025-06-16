using System;

namespace Unisave.SteamMicrotransactions
{
    /// <summary>
    /// Holds the result of a Steam microtransaction
    /// </summary>
    public class TransactionFlowResult
    {
        /// <summary>
        /// Transaction entity at its latest known state
        /// </summary>
        public SteamTransactionEntity Transaction { get; set; }
        
        /// <summary>
        /// Contains the error message if there was an error
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// Whether there was an error during the transaction processing.
        /// If true, the error human-readable message will be stored in the
        /// ErrorMessage field. Abort is also considered a kind of error,
        /// although it should be handled separately without displaying the
        /// error.
        /// </summary>
        public bool WasError => ErrorMessage != null;
        
        /// <summary>
        /// True only if not aborted and there was no error.
        /// </summary>
        public bool WasSuccess => !WasError && !WasAborted;
        
        /// <summary>
        /// Whether was the transaction aborted by the user
        /// (the user exited the Steam Overlay without finishing the checkout)
        /// </summary>
        public bool WasAborted { get; set; }

        public static TransactionFlowResult FromSuccess(
            SteamTransactionEntity transaction
        )
        {
            return new TransactionFlowResult() {
                Transaction = transaction,
                ErrorMessage = null,
                WasAborted = false
            };
        }
        
        public static TransactionFlowResult FromException(Exception e)
        {
            return new TransactionFlowResult() {
                Transaction = null,
                ErrorMessage = e.Message,
                WasAborted = false
            };
        }
        
        public static TransactionFlowResult FromAbort()
        {
            return new TransactionFlowResult() {
                Transaction = null,
                ErrorMessage = "You've aborted the transaction.",
                WasAborted = true
            };
        }
    }
}