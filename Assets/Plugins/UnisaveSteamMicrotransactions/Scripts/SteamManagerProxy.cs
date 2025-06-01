using System;
using System.Reflection;

namespace Unisave.SteamMicrotransactions
{
    /// <summary>
    /// Every usage of this class is just proxied to the `SteamManager` class.
    /// In your own scripts, use SteamManager directly instead of this proxy.
    /// This proxy solves a problem with the Unity `Plugins` folder,
    /// where this folder is compiled earlier than regular C# scripts and thus
    /// cannot interact with these scripts (but the SteamManager is among them).
    /// This proxy class uses C# reflection to communicate with the SteamManager
    /// at runtime, thereby sidestepping the issue. It should ONLY be used
    /// in scripts that are present in this Unisave module, not in your own
    /// scripts.
    /// </summary>
    public static class SteamManagerProxy
    {
        public static bool Initialized {
            get
            {
                var pi = GetSteamManagerType().GetProperty(
                    "Initialized",
                    BindingFlags.Public | BindingFlags.Static
                ) ?? throw new Exception(
                    "Cannot find the `Initialized` property on the " +
                    "`SteamManager` class."
                );
                return (bool) pi.GetValue(null);
            }
        }

        private static Type GetSteamManagerType()
        {
            return Type.GetType("SteamManager") ?? throw new Exception(
                "Cannot find the `SteamManager` class. Make sure you have" +
                " the Steamworks.NET library set up properly."
            );
        }
    }
}