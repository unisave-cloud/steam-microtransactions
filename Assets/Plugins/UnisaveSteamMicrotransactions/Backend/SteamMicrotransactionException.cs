using System;
using System.Runtime.Serialization;

namespace Unisave.SteamMicrotransactions
{
    [Serializable]
    public class SteamMicrotransactionException : Exception
    {
        public SteamMicrotransactionException()
            : this(
                "There was a problem with processing " +
                "a steam microtransaction."
            )
        {
        }

        public SteamMicrotransactionException(string message)
            : base(message)
        {
        }

        public SteamMicrotransactionException(
            string message,
            Exception inner
        ) : base(message, inner)
        {
        }

        public SteamMicrotransactionException(
            string message,
            ulong orderId,
            string errorCode,
            string errorDescription
        ) : this(
            $"{message}\n" +
            $"[{errorCode}] {errorDescription}\n" +
            $"Order ID: {orderId}"
        )
        {
        }

        protected SteamMicrotransactionException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}