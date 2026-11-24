<p align="center">
  <img src="https://i.imgur.com/6htHu3b.png" />
</p>

# Video Remote Mod

This is a **MelonLoader** Mod for **ChilloutVR**, 
The **Video Remote mod** adds QOL changes to **ChilloutVR** being able to have an easier time to do certain functions while in VR & Desktop.

**ChilloutVR website**: https://abinteractive.net

**Steam Store Page**: https://store.steampowered.com/app/661130/ChilloutVR/

This mod was created with no affiliation and not supported by Alpha Blend Interactive / ABI

Any references to code related to Alpha Blend Interactive / ABI is code owned by them.

**MelonLoader**: https://melonwiki.xyz/#/?id=requirements



# Discord
ChilloutVR Modding Group: https://discord.gg/dndGPM3bxu

# Mod Information

This mod allows you to have Video Remote Controls in the Quick Menu.

This is the feature request for this mod. https://feedback.abinteractive.net/p/video-player-remote-control

# Features

- Pick a video player in the current world, closest first (auto-selects when only one exists)
- Play / Pause / Paste-and-Play, with volume slider
- Spawn a local copy of the video screen with size and pickup controls
- Permission (lock level), Network Sync, Audio Mode and Max Resolution controls
- Watching toggle and Play untrusted URL for the selected player
- Fullscreen: show the video head-locked in your view (local only)
- Video Player Cam: a small movable head-locked video window (local only)
- Audio-only (YouTube) toggle
- Reload current video at the same timestamp
- Manual timestamp seek (hour / minute / second inputs and ±1 / ±5 min jog buttons)
- Auto-generated chapter-style timestamp jumps
- Event log and Debug page (FPS, codec, resolution, audio channels...)
- Save URLs and replay them later, with a per-session URL history and Copy URL to Clipboard
- SponsorBlock — automatically skip sponsor / self-promo / interaction / intro segments
- Load Blank Video preset
- Optional icon tint that follows your menu accent color

# Credits

- **DjShinter (Shin)**: original author. https://github.com/DjShinter
- **Nirvash**: SponsorBlock, URL history, timestamp controls, debug page, audio-only mode and resolution selector. https://github.com/Nirv-git

Both are also listed on the Contributors page in the mod.

# Installation

This mod **no longer requires BTKUILib** — it uses the new UI library built into ChilloutVR (which is based on BTKUILib; the original BTKUILib author now develops it as part of the game).

To install the "**Video Remote Mod**" place "**VideoRemote.dll**" inside your "Mods" folder located in **ChilloutVR** > **Mods**.

# Building this project

I'm not going to provide "**MelonLoader.dll**" & game files related to ChilloutVR, this is on you!

Built for ChilloutVR on Unity 6 (6000.3) with MelonLoader 0.7.3.

The `Libs/` folder needs the current versions of these DLLs (copy from your game install):

| File | Source |
| --- | --- |
| `MelonLoader.dll`, `0Harmony.dll` | `ChilloutVR/MelonLoader/net35/` |
| `Assembly-CSharp.dll`, `GameCommon.dll`, `Newtonsoft.Json.dll`, `Unity.TextMeshPro.dll` | `ChilloutVR/ChilloutVR_Data/Managed/` |
| `UnityEngine.dll`, `UnityEngine.CoreModule.dll`, `UnityEngine.IMGUIModule.dll`, `UnityEngine.ImageConversionModule.dll`, `UnityEngine.InputLegacyModule.dll`, `UnityEngine.InputModule.dll`, `UnityEngine.PhysicsModule.dll`, `UnityEngine.UnityWebRequestModule.dll` | `ChilloutVR/ChilloutVR_Data/Managed/` |

The stock game DLLs are enough, no publicized Assembly-CSharp is needed.

Then open `VideoRemote.sln` and build, or run `dotnet build -c Release`.

# Developer Note

If this mod breaks terms of service, I will shutdown this project immediately as soon as I get note regarding about this, but I believe that this is a quality of life change.

# Is this mod not allowed?

**Any modifications that are not approved** can get your **ABI account terminated** and such this modification is following the "modding guidelines" at the best it could be.

**They reserve the right to punish users using my mod**.

If you are scared of using modifications in your game **do not install mods**.

Official post regarding Modding: https://docs.abinteractive.net/official/legal/tos/#7-modding-our-games

§7.1 You may only create modifications for the purpose of improving the game. Malicious mods are not permitted and will be punished in any case we deem it necessary to take action on. ✅

§7.2 If you plan on publishing your mods to the workshop you must release the mods sourcecode to the public (for example by using github). This does not affect mods hosted on third party platforms directly, that are not released to the workshop. ✅

§7.3 Publishing obfuscated mods to the workshop is not allowed. ✅

§7.4 If you have to overwrite, alter or include base game scripts with your mod, you have to request appropriate permission before releasing it to the public. In this case we also require you to include a disclaimer that the base code of the game is owned by us. This may be a simple one-liner. ✅

§7.5 When releasing mods to the public, it is required to state, that the mod authors and modification are in no affiliation with ABI and not supported by ABI. ✅

§7.6 You may never create cheats / hacks to bypass limitations or gain an unfair advantage in any game mode or game world. ✅

§7.7 When installing mods, the liability for damage and harm caused by such is on the user. ABI will not be liable for any harm, damage or any other negative impact caused by modifications loaded from sources other than the official workshop. ✅
