using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Unisave.HttpClient;

namespace Unisave.SteamMicrotransactions.Steam.Steam
{
    /// <summary>
    /// Thrown by the SteamWebMtxApi service if the Steam API returns
    /// a non 2xx HTTP response.
    /// </summary>
    [Serializable]
    public class SteamApiHttpException : Exception
    {
        private SteamApiHttpException() { }

        private SteamApiHttpException(string message) : base(message) { }

        private SteamApiHttpException(string message, Exception inner)
            : base(message, inner) { }
        
        protected SteamApiHttpException(
            SerializationInfo info,
            StreamingContext context
        ) : base(info, context) { }

        /// <summary>
        /// Creates and throws the exception when the Steam API returned
        /// an HTTP-erroneous response. Metadata about the response will be
        /// present in the exception message.
        /// </summary>
        public static async Task ThrowIfHttpIsNon200Async(
            string message,
            Response response
        )
        {
            // 2xx responses are considered OK, anything else fails
            if (response.IsOk)
                return;

            // what does the Steam API has to say about the problem
            int status = response.Status;
            string body = await response.BodyAsync();
            
            // throw
            throw new SteamApiHttpException(
                $"{message}\nNon-2xx HTTP response, status {status} " +
                $"and body:\n{body}"
            );
        }
    }
}