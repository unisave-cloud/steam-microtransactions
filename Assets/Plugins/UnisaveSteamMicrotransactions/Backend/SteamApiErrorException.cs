using System;
using System.Runtime.Serialization;

namespace Unisave.SteamMicrotransactions
{
    [Serializable]
    public class SteamApiErrorException : Exception
    {
        private SteamApiErrorException(string message)
            : base(message)
        {
        }

        public SteamApiErrorException(
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

        protected SteamApiErrorException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}