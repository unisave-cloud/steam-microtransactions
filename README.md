Unisave Steam Microtransactions
===============================

<a href="https://unisave.cloud/" target="_blank">
    <img alt="Website" src="https://img.shields.io/badge/Website-unisave.cloud-blue">
</a>
<a href="https://discord.gg/XV696Tp" target="_blank">
    <img alt="Discord" src="https://img.shields.io/discord/564878084499832839?label=Discord">
</a>

This repository contains the Unisave Steam Microtransactions module sources. To get started read the [documentation page](https://unisave.cloud/docs/steam-microtransactions).


## Public User Documentation

- [Steam Microtransactions](https://unisave.cloud/docs/steam-microtransactions)
- [Installing Steamworks in Unity](https://unisave.cloud/guides/installing-steamworks-in-unity)
- [Testing Steam Overlay with the Unity Editor](https://unisave.cloud/guides/testing-steam-overlay-with-the-unity-editor)


## Local Documentation

- [Development setup](docs/development-setup.md)
- [Testing via Steam Client](docs/testing-via-steam-client.md)


## After Cloning

You need to install the precise version of Unity that the project currently uses, see the version in [`ProjectSettings/ProjectVersion.txt`](ProjectSettings/ProjectVersion.txt).

After cloning do:

1. Open the project in Unity (check the Unity version.)
2. Ignore compile errors.
3. Install required Unity packages in `Window > Package Manager` in `Packages: Unity Registry`
    - `TextMeshPro` (so that examples compile)
    - `JetBrains Rider Editor` (so that csproj and sln files are generated and Rider works well)
    - If some package installation fails, just restart Unity and retry.
    - Restart Unity once all are installed, if errors don't disappear right away.
4. Import the [Unisave asset](https://assetstore.unity.com/packages/slug/142705) from the asset store
5. Import Text Mesh Pro `Window > TextMeshPro > Import TMP Essential Resources`
6. Install Steamworks.NET as described in [this guide](https://unisave.cloud/guides/installing-steamworks-in-unity)
7. Set up Unisave cloud connection so that examples can be compiled and executed.
8. Set your own Steam App ID in `steam_appid.txt` and `SteamManager.cs` so that examples can be tested against Steam.

Now you should be able to launch example scenes. However note that testing Steam Overlay is complicated, see the documentation.


## Development

See the [Development setup](docs/development-setup.md) documentation page.
