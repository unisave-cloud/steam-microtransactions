using System;
using System.Runtime.Serialization;
using LightJson;

namespace Unisave.SteamMicrotransactions.Steam
{
    /// <summary>
    /// Thrown by the SteamWebMtxApi service if the Steam API returns
    /// the result "Failure", instead of "OK". This is an API-level problem,
    /// the HTTP request-response was 200 and fine.
    /// </summary>
    [Serializable]
    public class SteamApiFailureException : Exception
    {
        /// <summary>
        /// Steam transaction error code, if an error was returned
        /// by the Steam API. The list of codes:
        /// https://partner.steamgames.com/doc/features/microtransactions/implementation#error_codes
        /// </summary>
        public string ErrorCode { get; private set; }

        /// <summary>
        /// Description of the steam error, if an error was returned
        /// by the Steam API. The list of errors:
        /// https://partner.steamgames.com/doc/features/microtransactions/implementation#error_codes
        /// </summary>
        public string ErrorDescription { get; private set; }
        
        private SteamApiFailureException(
            string message,
            string errorCode,
            string errorDescription
        ) : base(message)
        {
            ErrorCode = errorCode;
            ErrorDescription = errorDescription;
        }

        protected SteamApiFailureException(
            SerializationInfo info,
            StreamingContext context
        ) : base(info, context) { }

        /// <summary>
        /// Inspects the "response.result" value of the JSON body and
        /// if it is "Failure", it throws the exception with error metadata.
        /// </summary>
        /// <param name="message">Human-readable message about the context</param>
        /// <param name="responseBody">The JSON body of the API response</param>
        public static void ThrowIfApiFailure(
            string message,
            JsonObject responseBody
        )
        {
            string result = responseBody["response"]["result"].AsString;

            // this is the condition on which we throw the exception
            if (result != "Failure")
                return;
            
            string errorCode
                = responseBody["response"]["error"]["errorcode"].AsString;
            string errorDescription
                = responseBody["response"]["error"]["errordesc"].AsString;

            throw new SteamApiFailureException(
                $"{message}\n[{errorCode}] {errorDescription}",
                errorCode,
                errorDescription
            );
        }
    }
}