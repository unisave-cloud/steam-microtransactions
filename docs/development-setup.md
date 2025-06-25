# Development setup (💸 Steam MTX module)

## Setting up

- Clone the repository `git clone git@github.com:unisave-cloud/steam-microtransactions.git`
- Follow the [After Cloning](../README.md#after-cloning) checklist in the root README file.


## New feature development

- Add the `-dev` suffix to the version in `ModuleMeta.cs`.
- Since you've likely re-installed Steamworks, update the versions in `ModuleMeta.cs` to match.
- Add the feature and commit changes.


## Testing new functionality and debugging

Testing Steam Overlay is a little complicated. Read the public guide to understand the problem and then the local documentation page to follow a personalized checklist:

- [Public: Testing Steam Overlay with the Unity Editor](https://unisave.cloud/guides/testing-steam-overlay-with-the-unity-editor)
- [Local: Testing via Steam Client](testing-via-steam-client.md)


## Deploying new version to GitHub

- Update the `Documentation.pdf` file in `Assets/Plugins/UnisaveSteamMicrotransactions`.
- Remove the `-dev` suffix and commit the new version to github (or make a `-rc.1` release candidate)
- Go to `Assets/Plugins` folder, right-click the `UnisaveSteamMicrotransactions` folder and choose `Export package...`
- Untick `Include dependencies` checkbox at the bottom of the dialog
- Select all files and export it into downloads folder and name it `unisave-steam-microtransactions-1.2.3-alpha.unitypackage`
- Create a github release page and attach the `.unitypackage` there


## Deploying further to the asset store

- Install *Asset Store Publishing Tools* into the project
- Open menu `Tools > Asset Store > Uploader` and log in
- Open the asset in the publisher portal (https://publisher.unity.com/) and click the blue button `Create new draft to edit`
- Refresh the upload tools in Unity and select the draft
- Select `Upload from pre-exported .unitypackage file` and select the file that was uploaded to github
- Upload the package
- Ignore the popup stating you need newer version of unity (yes for new uploads not for upgrades)
- Fill out and check all the tabs of the package draft
- Click submit
