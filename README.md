# EventTracker
A heavily customizable event tracker mod for Neon White to help with speedrunning practice, PBs, and more.

![image](https://github.com/stxticOVFL/EventTracker/assets/29069561/8efc6d74-95ac-453b-8225-7a935c1d1aec)

## Features
- Precise time tracking of various events (e.g. killing an enemy, discarding a weapon card)
- Comparing times to recorded personal bests
- Customizable sidebar display (X/Y/padding/entry count)
- Pick and choose which events get logged
- Saving tracked events to disk as easily readable files (in the ghost directory as `tracker.txt` and others)
- Scrolling display at the end
  - Can also be set to _only_ display at the end of a run, as can PB differences

## Installation

1. Download [MelonLoader](https://github.com/LavaGang/MelonLoader/releases/latest) and install it onto your `Neon White.exe`.
2. Run the game once. This will create required folders.
3. Download the **Mono** version of [Melon Preferences Manager](https://github.com/Bluscream/MelonPreferencesManager/releases/latest), and put the .DLLs from that zip into the `Mods` folder of your Neon White install.
    - The preferences manager is required to customize the event tracker, using F5 (by default).
4. Download `EventTracker.dll` from the [Releases page](https://github.com/stxticOVFL/EventTracker/releases/latest) and drop it in the `Mods` folder.

## Building & Contributing
This project uses Visual Studio 2022 as its project manager. When opening the Visual Studio solution, ensure your references are corrected by right clicking and selecting `Add Reference...` as shown below. 
Most will be in `Neon White_data/Managed`. Some will be in `MelonLoader/net35`, **not** `net6`. Select the `MelonPrefManager` mod for that reference.

![image](https://github.com/stxticOVFL/EventTracker/assets/29069561/ed3d94e3-52f2-48ea-9d69-84b018cf4336)

Once your references are correct, build using the keybind or like the picture below.

![image](https://github.com/stxticOVFL/EventTracker/assets/29069561/40a50e46-5fc2-4acc-a3c9-4d4edb8c7d83)

Make any edits as needed, and make a PR for review. PRs are very appreciated.

### Additional Notes
It's recommended to add `--melonloader.hideconsole` to your game launch properties (Steam -> Right click Neon White -> Properties -> Launch Options) to hide the console that MelonLoader spawns.

![image](https://github.com/stxticOVFL/EventTracker/assets/29069561/9c037da5-7323-435f-9e55-80904f799ae0)
![image](https://github.com/stxticOVFL/EventTracker/assets/29069561/4a4fa519-15b4-486f-a354-6ff7d0672df4)
