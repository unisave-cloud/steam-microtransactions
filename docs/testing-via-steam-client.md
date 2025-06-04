# Testing via Steam Client

Steam documentation on game uploading:
https://partner.steamgames.com/doc/sdk/uploading


## Preparing a game in Steamworks web portal

The Steamworks dashboard is here:
https://partner.steamgames.com/dashboard

I use my steam account `jirek97`. I'm added into Peter's organization.

[Traildown: Downhill Mountain Biking](https://partner.steamgames.com/apps/landing/2192070) is a stale game for which I have a branch set up for testing. Its Steam app ID is `2192070`.

When you go to `Technical Info > Edit Steamworks Settings` you get to [the page](https://partner.steamgames.com/apps/view/2192070) that contains all of the Steamworks settings for Traildown.

In `Steam Pipe > Builds` tab, there's a `unisave` beta branch created for my tests. In `Installation > General Installation` tab, there's a launch option for linux, that I use for testing, which is only enabled for the `unisave` branch. Also in `Steam Pipe > Depots` tab, there's a depot with ID `2192071` which the one that contains all built file for all OSes. Builds will be uploaded under this depot.


## Setting up Steamworks SDK

Download Steamworks SDK from [here](https://partner.steamgames.com/downloads/list), the latest version otherwise it updates itself at the first use.

Unzip the SDK into `~/unisave/steamworks_sdk`.

Make the content builder cmd executable:

```bash
chmod +x ~/unisave/steamworks_sdk/tools/ContentBuilder/builder_linux/steamcmd.sh
chmod +x ~/unisave/steamworks_sdk/tools/ContentBuilder/builder_linux/linux32/steamcmd
```


## Preparing the Build Config File

The content builder tool requires configuration files for upload to work.

Create a `unisave-test.vdf` file in the `scripts` folder:

```bash
code ~/unisave/steamworks_sdk/tools/ContentBuilder/scripts/unisave-test.vdf
```

And place the following content into it (use TABS for indentation!):

```groovy
"AppBuild"
{
	"AppID" "2192070" // your AppID
	"Desc" "Uploads a Unisave test build" // internal description for this build

	"ContentRoot" "../content/" // root content folder, relative to location of this file
	"BuildOutput" "../output/" // build output folder for build logs and build cache files

	"SetLive" "unisave" // after upload, set it live for this beta branch

	"Depots"
	{
		"2192071" // your DepotID
		{
			"FileMapping"
			{
				"LocalPath" "*" // all files from contentroot folder
				"DepotPath" "." // mapped into the root of the depot
				"recursive" "1" // include all subfolders
			}
		}
	}
}
```


## Building from Unity

You will build the Unity project into this folder:

```
~/unisave/steamworks_sdk/tools/ContentBuilder/content
```

First, make sure that folder is empty:

```bash
rm -r ~/unisave/steamworks_sdk/tools/ContentBuilder/content/*
```

Then open up Unity and start a build `Shift + Ctrl + B`:

1. Make sure the `SimpleDemo` scene is included.
2. Platform: `Windows + Mac + Linux`.
3. Target Platform: `Linux`.
4. Development build: `yes`.
5. No profiler, no profiler, no debugger, compression `Default`.

Click on `Build`, select the `content` folder above and name the resulting file `unisave-test.x86_64`. Submit the dialog and make the build finish.

The built game should be placed into the `content` folder and the `unisave-test.x86_64` should be executable and when launched, it should start up.


## Uploading the build to Steam

Enter the `ContentBuilder` folder of the SDK:

```bash
cd ~/unisave/steamworks_sdk/tools/ContentBuilder
```

Start the steamcmd CLI tool:

```bash
./builder_linux/steamcmd.sh
```

It may update and when done, you should see the CLI:

```
Steam Console Client (c) Valve Corporation - version 1747702063
-- type 'quit' to exit --
Loading Steam API...OK

Steam>
```

Login via this command and provide the password and handle the two-phase auth:

```
login jirek97
```

Trigger the build with the `unisave-test.vdf` config file created earlier:

> **Note:** The path is relative to the `builder_linux/steamcmd.sh` file, not the `ContentBuilder` despite being launched from that place.

```
run_app_build ../scripts/unisave-test.vdf
```

It should finish and succeed:

```
[2025-06-04 21:33:01]: Starting AppID 2192070 build (flags 0x0).
[2025-06-04 21:33:01]: Building depot 2192072...

Building file mapping...
Scanning content
.. 23.1MB (13%)
... 49.4MB (29%)
.... 78.8MB (46%)
... 103.9MB (61%)
... 129.1MB (75%)
... 152.5MB (89%)
.. 170.0MB (100%)
IPC function call IClientUtils::GetAPICallResult took too long: 64 msec
Uploading content...
. 15.2MB (12%)
.. 28.2MB (23%)
. 41.7MB (34%)
.. 55.2MB (45%)
.. 68.5MB (56%)
. 81.5MB (67%)
.. 94.4MB (78%)
. 108.0MB (89%)
.. 120.6MB (100%)
IPC function call IClientUtils::GetAPICallResult took too long: 162 msec

[2025-06-04 21:33:22]: Successfully finished AppID 2192070 build (BuildID 18739676).
```


## Testing the steam via Steam Client

Start the steam client (if the UI gets stuck, toggle fullscreen, it's because of the i3wm).

Right-click `Traildown` in the library, select `Properties...`. In the `Betas` tab in `Beta Participation` select `unisave` form the dropdown (or rather, provide password since it's a private branch and then click to switch to it).

Update the game and launch it via the steam client.

Now you can test it to see. MAKE SURE THE PAYMENT GOES VIA THE SANDBOX API!

Good luck.
