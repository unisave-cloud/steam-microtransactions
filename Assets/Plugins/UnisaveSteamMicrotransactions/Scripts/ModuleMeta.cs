namespace Unisave.SteamMicrotransactions
{
    /// <summary>
    /// Contains metadata about the Unisave Steam Microtransactions module
    /// </summary>
    public static class ModuleMeta
    {
        /// <summary>
        /// Version of this module
        /// </summary>
        public static readonly string Version = "1.0.0";

        /// <summary>
        /// The version of Steamworks.NET this module was last tested against
        /// https://github.com/rlabrecque/Steamworks.NET/releases
        /// </summary>
        public static readonly string SteamworksNetVersion = "2024.8.0";

        /// <summary>
        /// The version of Steamworks SDK used by the Steamworks.NET version
        /// that this module was last tested against
        /// </summary>
        public static readonly string SteamworksSdkVersion = "1.60";
    }
}