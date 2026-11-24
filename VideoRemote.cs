using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ABI.CCK.Components;
using ABI_RC.Core.Networking;
using ABI_RC.Core.Networking.API;
using ABI_RC.Core.Networking.API.Responses;
using ABI_RC.Core.Savior;
using ABI_RC.Core.UI;
using ABI_RC.Systems.PlayerColors;
using ABI_RC.VideoPlayer;
using ABI_RC.VideoPlayer.Scripts;
using ABI_RC.Systems.UI.UILib;
using ABI_RC.Systems.UI.UILib.UIObjects;
using ABI_RC.Systems.UI.UILib.UIObjects.Components;
using ABI_RC.Systems.UI.UILib.UIObjects.Objects;
using HarmonyLib;
using GameCommon.Models.VideoPlayer;
using MelonLoader;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace VideoRemote
{
    internal static class ModBuildInfo
    {
        public const string Name = "Video Remote";
        public const string Author = "Shin, Nirvash";
        public const string Version = "1.8.0";
        public const string Description = "This allows you to use the video player with the menu.";
        public const string DownloadLink = "https://github.com/DjShinter/VideoRemote/releases";
    }

    public sealed class VideoRemoteMod : MelonMod
    {
        private static VideoRemoteMod Instance;

        private const string ModCategory = "VideoRemoteMod";
        private static MelonPreferences_Category cat;
        private static MelonPreferences_Entry<bool> sponsorSkip_sponsor;
        private static MelonPreferences_Entry<bool> sponsorSkip_selfpromo;
        private static MelonPreferences_Entry<bool> sponsorSkip_interaction;
        private static MelonPreferences_Entry<bool> sponsorSkip_intro;
        internal static MelonPreferences_Entry<bool> videoHistory_En;
        private static MelonPreferences_Entry<bool> iconTint_En;

        // Pages and categories
        private static Page MainPage;
        private static Category VideoPlayerListMain;
        private static Category VideoPlayerList;
        private static Category AdvancedOptions;
        private static Category videoName;
        private static Page AdvOptionsPage;
        private static Page TimeStampPage;
        private static Page LogPage;
        private static Page DebugPage;
        private static Page SponsorSkipEvents;
        private static Page savedURLsPage;
        private static Page URLHistoryPage;
        private static Page ContributorsPage;
        private static string ContributorsPageString;
        private static bool _avatarsRequested = false;
        private static int _iconVersion = 0;
        private static HashSet<string> _tintableSet;
        private static SliderFloat volumeSlider;

        // Page IDs for breadcrumb tracking
        private static string VideoFolderString;
        private static string MainPageString;
        private static string SponsorSkipEventsString;
        private static string AdvOptionsString;
        private static string TimeStampPageString;
        private static string LogPageString;
        private static string DebugPageString;
        private static string savedURLsPageString;
        private static string URLHistoryPageString;
        private static string lastQMPage = "";

        private static bool _initialized = false;
        private static ViewManagerVideoPlayer VideoPlayerSelected;

        private const string FolderRoot = "UserData/VideoRemote/";
        private const string savedURLsFileName = "savedURLsList.txt";
        private const string historyURLFileName = "historyURLsList.txt";

        private static Dictionary<string, (string, DateTime)> savedURLs = new Dictionary<string, (string, DateTime)>();
        private static List<(string, string, DateTime)> historyURLs = new List<(string, string, DateTime)>();

        // Local screen
        private static GameObject localScreen;
        private static bool pickupable = false;
        private static float sizeScale = 1f;

        // Fullscreen (head-locked camera screen)
        private static GameObject fullscreenObj;
        private static float fullscreenSize = 1f;
        private static ToggleButton fullscreenToggle;

        // Video Player Cam (a movable, head-locked window showing the video - a side-window Fullscreen)
        private static GameObject vpCamObj;
        private static float vpCamSize = 0.4f;
        private static float vpCamOffsetX = 0.6f;
        private static float vpCamOffsetY = -0.2f;
        private static ToggleButton vpCamToggle;
        private const float vpCamDistance = 1f;

        // SponsorBlock
        private static bool sponsorSkip = false;
        private static object sponsorSkipCheckCoroutine;
        private static object sponsorSkipLiveCoroutine;
        private static string lastvideo = "zzzzzzzzzzzzzzzzzzzzz09";
        private static string sponsorskipVideo = "xxxxxxxxxxxxxxxxxxxx08";
        private static SponsorSkipSegment[] sponsorskipResult;
        private static readonly List<(string, float[])> sponsorskips = new List<(string, float[])>();
        private static float[] lastskip = new float[2];

        private static bool TryingToReplay = false;
        private static int timeStampHour = 0;
        private static int timeStampMin = 0;
        private static int timeStampSec = 0;
        private static float historyLastCheck = 0f;

        public override void OnInitializeMelon()
        {
            Instance = this;

            cat = MelonPreferences.CreateCategory(ModCategory, "Video Remote", true, true);
            sponsorSkip_sponsor     = MelonPreferences.CreateEntry(ModCategory, "sponsorSkip_sponsor",     true,  "Skips Segment Sponsor");
            sponsorSkip_selfpromo   = MelonPreferences.CreateEntry(ModCategory, "sponsorSkip_selfpromo",   false, "Skips Segment selfpromo");
            sponsorSkip_interaction = MelonPreferences.CreateEntry(ModCategory, "sponsorSkip_interaction", false, "Skips Segment interaction");
            sponsorSkip_intro       = MelonPreferences.CreateEntry(ModCategory, "sponsorSkip_intro",       false, "Skips Segment intro");
            videoHistory_En         = MelonPreferences.CreateEntry(ModCategory, "videoHistory_En",         false, "Record history of played URLs for all Video Players. Checks every time a video is Played");
            iconTint_En             = MelonPreferences.CreateEntry(ModCategory, "iconTint_En",             false, "Tint white icons with the menu accent color (matches your CVR menu theme)");

            _initialized = false;
            VideoPlayerSelected = null;

            if (!Directory.Exists(FolderRoot))
                Directory.CreateDirectory(FolderRoot);

            LoadURLs();
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (SceneManager.GetActiveScene().name == "Init" && !_initialized)
            {
                _initialized = true;
                SetupIcons();
            }
            VideoPlayerSelected = null;
            // These are tied to the old scene's video player; drop them on scene change.
            DestroyFullscreen();
            if (fullscreenToggle != null) fullscreenToggle.ToggleValue = false;
            DestroyVideoPlayerCam();
            if (vpCamToggle != null) vpCamToggle.ToggleValue = false;
        }

        // Icon name → embedded file name. Logo is intentionally first and excluded from tinting.
        private static readonly (string name, string file, bool tintable)[] IconTable =
        {
            ("VideoPlayerModLogo",              "VideoPlayerModLogo.png",      false),
            ("VideoPlayerModPlay2",             "Play.png",                    true),
            ("VideoPlayerModPause2",            "Pause.png",                   true),
            ("VideoPlayerModNewScreen",         "NewScreen.png",               true),
            ("VideoPlayerModWhite-Minus",       "White-Minus.png",             true),
            ("VideoPlayerModWhite-Plus",        "White-Plus.png",              true),
            ("VideoPlayerModSound",             "Sound.png",                   true),
            ("VideoPlayerModKeys",              "Keys.png",                    true),
            ("VideoPlayerModVideoPlayer",       "VideoPlayer.png",             true),
            ("VideoPlayerModVideoPlayer-World", "VideoPlayer-World.png",       true),
            ("VideoPlayerModVideoPlayer-Prop",  "VideoPlayer-Prop.png",        true),
            ("VideoPlayerModPastePlay",         "PastePlay.png",               true),
            ("VideoPlayerModSave",              "Save.png",                    true),
            ("VideoPlayerModSponsorSkip",       "SponsorSkip.png",             true),
            ("VideoPlayerModArrow-Left",        "Arrow-Left.png",              true),
            ("VideoPlayerModArrow-Right",       "Arrow-Right.png",             true),
            ("VideoPlayerModAdvSettings2",      "AdvSettings.png",             true),
            ("VideoPlayerModReloadVideo",       "ReloadVideo.png",             true),
            ("VideoPlayerModLoadURLs",          "LoadURLs.png",                true),
            ("VideoPlayerModUp",                "Up.png",                      true),
            ("VideoPlayerModTimestamp",         "Timestamp.png",               true),
            ("VideoPlayerModClock-Hours",       "Clock-Hours.png",             true),
            ("VideoPlayerModClock-Minutes",     "Clock-Minutes.png",           true),
            ("VideoPlayerModClock-Seconds",     "Clock-Seconds.png",           true),
            ("VideoPlayerModPlayBlack",         "PlayBlack.png",               true),
            ("VideoPlayerModReset",             "Reset.png",                   true),
            ("VideoPlayerModDebug",             "Debug.png",                   true),
            ("VideoPlayerModRes",               "Res.png",                     true),
            ("VideoPlayerModEventLog",          "EventLog.png",                true),
            ("VideoPlayerModRemoveLink",        "RemoveLink.png",              true),
            ("VideoPlayerModURLHistory",        "URLHistory.png",              true),
            ("VideoPlayerMod-jog-5",            "Minus5.png",                  true),
            ("VideoPlayerMod-jog-1",            "Minus1.png",                  true),
            ("VideoPlayerMod-jog1",             "Plus1.png",                   true),
            ("VideoPlayerMod-jog5",             "Plus5.png",                   true),
            ("VideoPlayerModContributors",      "Contributors.png",            true),
            ("VideoPlayerMod-untrusted",        "Untrusted.png",               true),
            ("VideoPlayerMod-copy",             "Copy.png",                    true),
        };

        private static void SetupIcons()
        {
            _tintableSet = new HashSet<string>(IconTable.Where(x => x.tintable).Select(x => x.name));
            RegisterIcons();
            SetupUI();
            // SetupUI creates buttons with base icon names. If the user prefs tint ON,
            // walk the tree once and swap every tintable icon to its versioned URL.
            if (iconTint_En.Value) RetintAllButtons();
            QuickMenuAPI.OnOpenedPage += OnPageOpen;
            QuickMenuAPI.OnBackAction += OnPageBack;
            PlayerColorsManager.OnLocalPlayerColorChanged += OnLocalPlayerColorChanged;
        }

        // Resolves a base icon name to the name buttons should actually use.
        // Tintable icons get a "_v<N>" suffix so that each tint refresh produces a fresh URL
        // that cohtml hasn't cached yet. Non-tintable icons (logo, avatars) stay as-is.
        internal static string Icon(string baseName)
        {
            if (string.IsNullOrEmpty(baseName)) return baseName;
            if (_tintableSet == null || !_tintableSet.Contains(baseName)) return baseName;
            if (!iconTint_En.Value) return baseName;
            return baseName + "_v" + _iconVersion;
        }

        private static string StripVersion(string iconName)
        {
            if (string.IsNullOrEmpty(iconName)) return iconName;
            var idx = iconName.LastIndexOf("_v", StringComparison.Ordinal);
            if (idx < 0) return iconName;
            if (int.TryParse(iconName.Substring(idx + 2), out _))
                return iconName.Substring(0, idx);
            return iconName;
        }

        private static void RegisterIcons()
        {
            var tint = iconTint_En.Value ? (Color?)PlayerColorsManager.CurrentColors.SecondaryColor : null;
            foreach (var (name, file, tintable) in IconTable)
            {
                // Always write the untinted (white) PNG under the base name.
                using (var rawStream = LoadIconStream($"VideoRemote.UI.Images.{file}", null))
                {
                    if (rawStream != null) QuickMenuAPI.PrepareIcon(ModCategory, name, rawStream);
                }
                // Additionally, if tinting is on AND this icon is tintable, write a tinted variant
                // under the versioned name. Buttons use this versioned name (see Icon()) so the
                // URL is new each refresh and cohtml's cache can't serve a stale image.
                if (tintable && tint != null)
                {
                    using var tinted = LoadIconStream($"VideoRemote.UI.Images.{file}", tint);
                    if (tinted != null) QuickMenuAPI.PrepareIcon(ModCategory, name + "_v" + _iconVersion, tinted);
                }
            }
        }

        // Walk every Button in the UI tree under MainPage and update its icon to the current
        // value of Icon(baseName). Used after a tint refresh so existing buttons swap to the
        // new versioned URL.
        private static void RetintAllButtons()
        {
            if (MainPage == null) return;
            RetintRecursive(MainPage);
        }

        private static void RetintRecursive(QMUIElement element)
        {
            if (element is Button btn)
            {
                var baseName = StripVersion(btn.ButtonIcon);
                if (_tintableSet != null && _tintableSet.Contains(baseName))
                {
                    try { btn.ButtonIcon = Icon(baseName); } catch { /* button may be deleted */ }
                }
            }
            var subs = HarmonyLib.Traverse.Create(element).Field("SubElements").GetValue<List<QMUIElement>>();
            if (subs == null) return;
            foreach (var s in subs.ToArray()) RetintRecursive(s);
        }

        private static Stream LoadIconStream(string resourceName, Color? tint)
        {
            var asm = typeof(VideoRemoteMod).Assembly;
            using var raw = asm.GetManifestResourceStream(resourceName);
            if (raw == null)
            {
                MelonLogger.Warning($"Missing embedded icon: {resourceName}");
                return null;
            }
            using var ms = new MemoryStream();
            raw.CopyTo(ms);
            var bytes = ms.ToArray();

            if (tint == null) return new MemoryStream(bytes);

            Texture2D tex = null;
            try
            {
                tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!tex.LoadImage(bytes)) return new MemoryStream(bytes);

                var pixels = tex.GetPixels();
                var c = tint.Value;
                for (var i = 0; i < pixels.Length; i++)
                {
                    var p = pixels[i];
                    pixels[i] = new Color(p.r * c.r, p.g * c.g, p.b * c.b, p.a);
                }
                tex.SetPixels(pixels);
                tex.Apply();
                return new MemoryStream(tex.EncodeToPNG());
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Failed to tint icon {resourceName}: {ex.Message}");
                return new MemoryStream(bytes);
            }
            finally
            {
                if (tex != null) UnityEngine.Object.Destroy(tex);
            }
        }

        private static void OnLocalPlayerColorChanged(PlayerColors _)
        {
            try { RefreshTintedIcons(); }
            catch (Exception ex) { MelonLogger.Warning("Failed to re-tint icons on color change\n" + ex); }
        }

        // Generate fresh versioned files and swap every existing button's icon to the new URLs.
        // Called when the user toggles the tint pref or changes their CVR menu accent.
        private static void RefreshTintedIcons()
        {
            _iconVersion++;
            RegisterIcons();
            RetintAllButtons();
        }

        private static void SetupUI()
        {
            MainPage = new Page(ModCategory, "VideoRemotePage", true, "VideoPlayerModLogo")
            {
                MenuTitle = ModBuildInfo.Name,
                MenuSubtitle = ModBuildInfo.Description
            };
            MainPageString = MainPage.ElementID;

            videoName = MainPage.AddCategory("No Video Player Selected");

            // Video player picker
            var pickPage = videoName.AddPage("Select Video Player", "VideoPlayerModLogo", "List Video Players in the World", ModCategory);
            VideoFolderString = pickPage.ElementID;
            VideoPlayerListMain = pickPage.AddCategory("", false);
            VideoPlayerList = pickPage.AddCategory("Video Players In World");

            // Top-level playback buttons
            var playBtn = videoName.AddButton("Play Video", "VideoPlayerModPlay2", "Play the Video");
            playBtn.OnPress += () =>
            {
                if (VideoPlayerSelected != null)
                {
                    VideoPlayerSelected.Play();
                    MelonCoroutines.Start(Instance.SetCurrentVideoNameDelay());
                }
                else AlertNoPlayer();
            };

            var pauseBtn = videoName.AddButton("Pause Video", "VideoPlayerModPause2", "Pause the Video");
            pauseBtn.OnPress += () =>
            {
                if (VideoPlayerSelected != null)
                {
                    VideoPlayerSelected.Pause();
                    MelonCoroutines.Start(Instance.SetCurrentVideoNameDelay());
                }
                else AlertNoPlayer();
            };

            var pasteBtn = videoName.AddButton("Paste and Play Video", "VideoPlayerModPastePlay", "Paste and Play the Video");
            pasteBtn.OnPress += () =>
            {
                if (VideoPlayerSelected != null)
                {
                    QuickMenuAPI.ShowConfirm("Confirm", "Paste and Play Video?", () =>
                    {
                        VideoPlayerSelected.PasteAndPlay();
                        MelonCoroutines.Start(Instance.SetCurrentVideoNameDelay());
                    }, () => { }, "Yes", "No");
                }
                else AlertNoPlayer();
            };

            var volumeCat = MainPage.AddCategory("", false);
            volumeSlider = volumeCat.AddSlider("Volume", "Video Player Volume", 0.5f, 0f, 1f);
            volumeSlider.OnValueUpdated += _ =>
            {
                if (VideoPlayerSelected != null)
                    VideoPlayerSelected.SetLocalAudioVolume(volumeSlider.SliderValue);
            };

            AdvancedOptions = MainPage.AddCategory("", false);
            PopulateAdvancedButtons(init: true);

            // Local screen controls
            var localCat = MainPage.AddCategory("Local Video Player Screen");
            var spawnBtn = localCat.AddButton("Spawn/Toggle Local Screen", "VideoPlayerModNewScreen",
                "Creates a local copy of the video player screen in front of you.<p>You must select a video player first.");
            spawnBtn.OnPress += () =>
            {
                if (VideoPlayerSelected == null && (localScreen == null || localScreen.Equals(null)))
                {
                    QuickMenuAPI.ShowAlertToast("Can not create local screen. Video Player Not Selected or does not exist.", 3);
                    MelonLogger.Msg("Can not create local screen. Video Player Not Selected or does not exist.");
                    return;
                }
                ToggleLocalScreen();
            };
            var smallerBtn = localCat.AddButton("Smaller", "VideoPlayerModWhite-Minus", "Decreases the screen size");
            smallerBtn.OnPress += () =>
            {
                if (sizeScale > 0.15f) sizeScale -= 0.15f;
                UpdateLocalScreen();
            };
            var largerBtn = localCat.AddButton("Larger", "VideoPlayerModWhite-Plus", "Increases the screen size");
            largerBtn.OnPress += () =>
            {
                sizeScale += 0.15f;
                UpdateLocalScreen();
            };
            var pickupToggle = localCat.AddToggle("Pickupable", "Toggles pickup of the local screen", pickupable);
            pickupToggle.OnValueUpdated += v =>
            {
                pickupable = v;
                UpdateLocalScreen();
            };

            // Fullscreen (head-locked camera screen)
            var fsCat = MainPage.AddCategory("Fullscreen");
            fullscreenToggle = fsCat.AddToggle("Fullscreen",
                "Render the selected video straight into your view (head-locked).<p>Local only - others don't see it.",
                IsFullscreenActive());
            fullscreenToggle.OnValueUpdated += v =>
            {
                if (v) EnableFullscreen();
                else DestroyFullscreen();
            };
            var fsSize = fsCat.AddSlider("Fullscreen Size", "Scale of the fullscreen video", fullscreenSize, 0.5f, 3f);
            fsSize.OnValueUpdated += val =>
            {
                fullscreenSize = val;
                if (fullscreenObj != null && !fullscreenObj.Equals(null))
                {
                    var fs = fullscreenObj.GetComponent<FullscreenScreen>();
                    if (fs != null) fs.SizeMultiplier = val;
                }
            };

            // Video Player Cam - a movable head-locked window showing the video (a side-window version of Fullscreen)
            var vpCamCat = MainPage.AddCategory("Video Player Cam");
            vpCamToggle = vpCamCat.AddToggle("Video Player Cam",
                "Shows the selected video in a small head-locked window you can move around your view.<p>Local only.",
                IsVideoPlayerCamActive());
            vpCamToggle.OnValueUpdated += v =>
            {
                if (v) EnableVideoPlayerCam();
                else DestroyVideoPlayerCam();
            };
            var vpCamSizeSlider = vpCamCat.AddSlider("Window Size", "Size of the window in your view", vpCamSize, 0.15f, 2f);
            vpCamSizeSlider.OnValueUpdated += val =>
            {
                vpCamSize = val;
                ApplyVideoPlayerCam();
            };
            var vpCamXSlider = vpCamCat.AddSlider("Move Left/Right", "Horizontal position of the window", vpCamOffsetX, -2f, 2f);
            vpCamXSlider.OnValueUpdated += val =>
            {
                vpCamOffsetX = val;
                ApplyVideoPlayerCam();
            };
            var vpCamYSlider = vpCamCat.AddSlider("Move Up/Down", "Vertical position of the window", vpCamOffsetY, -1.5f, 1.5f);
            vpCamYSlider.OnValueUpdated += val =>
            {
                vpCamOffsetY = val;
                ApplyVideoPlayerCam();
            };

            // Contributors page
            var creditsCat = MainPage.AddCategory("About", false);
            ContributorsPage = creditsCat.AddPage("Contributors", "VideoPlayerModContributors", "People who helped build this mod", ModCategory);
            ContributorsPageString = ContributorsPage.ElementID;
            CreatePageContributors();
        }

        private static void AlertNoPlayer()
        {
            QuickMenuAPI.ShowAlertToast("Video Player Not Selected or does not exist.", 2);
            MelonLogger.Msg("Video Player Not Selected or does not exist.");
        }

        private static void SaveUrl(ViewManagerVideoPlayer vp)
        {
            const string dateFmt = "yyyy'-'MM'-'dd";
            if (string.IsNullOrWhiteSpace(vp.videoUrl.text))
            {
                MelonLogger.Msg("There was nothing to save.");
                return;
            }

            var vidName = Utils.VideoNameFormat(vp);
            var path = FolderRoot + savedURLsFileName;
            using (var sw = File.Exists(path) ? File.AppendText(path) : File.CreateText(path))
            {
                sw.WriteLine($"{DateTime.Now.ToString(dateFmt)} ␟ {vidName} ␟ {vp.videoUrl.text}");
            }

            if (savedURLs.ContainsKey(vp.videoUrl.text))
                savedURLs.Remove(vp.videoUrl.text);
            savedURLs.Add(vp.videoUrl.text, (vidName, DateTime.Now));
        }

        private static void RemoveURL(string videoUrl)
        {
            const string dateFmt = "yyyy'-'MM'-'dd";
            if (!savedURLs.ContainsKey(videoUrl)) return;

            savedURLs.Remove(videoUrl);
            try
            {
                File.WriteAllLines(FolderRoot + savedURLsFileName,
                    savedURLs.Select(p => $"{p.Value.Item2.ToString(dateFmt)} ␟ {p.Value.Item1} ␟ {p.Key}"),
                    Encoding.UTF8);
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Error writing video list after removing URL\n" + ex);
            }
        }

        private static void LoadURLs()
        {
            const string dateFmt = "yyyy-MM-dd";
            var path = FolderRoot + savedURLsFileName;
            if (!File.Exists(path)) return;
            try
            {
                savedURLs = new Dictionary<string, (string, DateTime)>(File.ReadAllLines(path)
                    .Select(line => line.Split(new[] { '␟' }, StringSplitOptions.RemoveEmptyEntries))
                    .Where(parts => parts.Length == 3)
                    .ToLookup(p => p[2].Trim(), p => (p[1].Trim(), Utils.ParseDate(p[0].Trim(), dateFmt)))
                    .ToDictionary(p => p.Key, p => p.First()));
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Error reading previously saved URLs \n" + ex);
            }
        }

        private static void PopulateVideoList()
        {
            if (VideoPlayerListMain.IsGenerated) VideoPlayerListMain.ClearChildren();
            if (VideoPlayerList.IsGenerated) VideoPlayerList.ClearChildren();

            var reload = VideoPlayerListMain.AddButton("Load Video Players", "VideoPlayerModLogo", "Reload the list of Video Players in the world");
            reload.OnPress += PopulateVideoList;

            if (!UnityEngine.Object.FindAnyObjectByType<CVRVideoPlayer>()) return;

            // Closest players first
            var players = UnityEngine.Object.FindObjectsByType<CVRVideoPlayer>(FindObjectsSortMode.None)
                .Select(p => (vp: p, dist: Vector3.Distance(PlayerUiPosition(p), Camera.main.transform.position)))
                .OrderBy(x => x.dist)
                .ToList();

            foreach (var (cvrVp, distF) in players)
            {
                var uis = Traverse.Create(cvrVp).Field("VideoPlayerUis").GetValue<List<IVideoPlayerUi>>();
                foreach (var vp in uis.Cast<ViewManagerVideoPlayer>())
                {
                    var dist = distF.ToString("F2").TrimEnd('0');
                    var playerType = cvrVp.gameObject.transform.GetPlayerType();
                    var icon = playerType == "Prop" ? "VideoPlayerModVideoPlayer-Prop" : "VideoPlayerModVideoPlayer-World";

                    var btn = VideoPlayerList.AddButton(
                        "Video Player\n" + playerType,
                        icon,
                        $"Video:{Utils.VideoNameFormat(vp)}, Distance:{dist}<p>{cvrVp.gameObject.transform.GetPath()}");
                    btn.OnPress += () =>
                    {
                        VideoPlayerSelected = vp;
                        MelonLogger.Msg(cvrVp.playerId + " has been selected");
                    };
                }
            }
        }

        // Lock levels the player allows; falls back to every level if the server sent none
        private static List<VideoPlayerLockOption> GetLockOptions(VideoPlayerLockOption available)
        {
            var all = Enum.GetValues(typeof(VideoPlayerLockOption)).Cast<VideoPlayerLockOption>()
                .Where(o => o != VideoPlayerLockOption.None).ToList();
            var allowed = all.Where(o => (available & o) != 0).ToList();
            return allowed.Count > 0 ? allowed : all;
        }

        private static Vector3 PlayerUiPosition(CVRVideoPlayer p) =>
            p.videoPlayerUIPosition != null ? p.videoPlayerUIPosition.position : p.transform.position;

        private static void PopulateAdvancedButtons(bool init)
        {
            if (AdvancedOptions.IsGenerated) AdvancedOptions.ClearChildren();

            AdvOptionsPage = AdvancedOptions.AddPage("VideoPlayer Options", "VideoPlayerModAdvSettings2",
                "Set Permissions, Network Sync, Audio Mode, Reload current video, Timestamp Controls, Info and Debug", ModCategory);
            AdvOptionsString = AdvOptionsPage.ElementID;
            CreatePageAdvOptionsPage(init);

            SponsorSkipEvents = AdvancedOptions.AddPage("SponsorBlock", "VideoPlayerModSponsorSkip",
                "SponsorBlock can automatically skip sponsor segments in Youtube videos.", ModCategory);
            SponsorSkipEventsString = SponsorSkipEvents.ElementID;
            CreatePageSponsorSkip();

            var saveBtn = AdvancedOptions.AddButton("Save URL", "VideoPlayerModSave",
                $"Stores the current video URL into the ChilloutVR/{FolderRoot} Folder.");
            saveBtn.OnPress += () =>
            {
                if (Utils.IsVideoPlayerValid(VideoPlayerSelected))
                {
                    SaveUrl(VideoPlayerSelected);
                    QuickMenuAPI.ShowAlertToast($"Saved URL! Located in ChilloutVR/{FolderRoot}{savedURLsFileName}", 3);
                    MelonLogger.Msg($"Saved URL! Located in ChilloutVR/{FolderRoot}{savedURLsFileName}");
                }
                else AlertNoPlayer();
            };

            savedURLsPage = AdvancedOptions.AddPage("Load Saved URLs", "VideoPlayerModLoadURLs", "Load previously saved URLS", ModCategory);
            savedURLsPageString = savedURLsPage.ElementID;
            CreatePageSavedURLsPage();
        }

        private static void CreatePageAdvOptionsPage(bool init)
        {
            if (AdvOptionsPage != null && AdvOptionsPage.IsGenerated)
                AdvOptionsPage.ClearChildren();

            var top = AdvOptionsPage.AddCategory("", false);

            // Permission (lock level)
            var permCurrent = Utils.IsVideoPlayerValid(VideoPlayerSelected)
                ? VideoPlayerSelected.videoPlayer.ControlPermission.ToString()
                : "None Selected";
            var permBtn = top.AddButton("Permission: " + permCurrent, "VideoPlayerModKeys",
                "Choose who can control this video player");
            permBtn.OnPress += () =>
            {
                if (!Utils.IsVideoPlayerValid(VideoPlayerSelected)) { AlertNoPlayer(); return; }

                var player = VideoPlayerSelected.videoPlayer;
                var options = GetLockOptions(Traverse.Create(player).Property("AvailableLockOptions").GetValue<VideoPlayerLockOption>());
                var names = options.Select(o => o.ToString()).ToArray();
                var selection = new ABI_RC.Systems.UI.UILib.UIObjects.Objects.MultiSelection("Permission", names,
                    Math.Max(0, options.IndexOf(player.ControlPermission)));
                selection.OnOptionUpdated += i =>
                {
                    if (!Utils.IsVideoPlayerValid(VideoPlayerSelected)) return;
                    VideoPlayerSelected.videoPlayer.SetControlPermission(options[i]);
                    permBtn.ButtonText = "Permission: " + options[i];
                };
                QuickMenuAPI.OpenMultiSelect(selection);
            };

            // Network sync toggle
            var netSync = top.AddToggle("Network Sync", "Toggles Network Sync to state", true);
            netSync.OnValueUpdated += sync =>
            {
                if (Utils.IsVideoPlayerValid(VideoPlayerSelected))
                    VideoPlayerSelected.videoPlayer.SetNetworkSync(sync);
                else
                {
                    QuickMenuAPI.ShowAlertToast("Video Player Not Selected or does not exist.", 3);
                    MelonLogger.Msg("Video Player Not Selected or does not exist.");
                }
            };

            // Audio mode cycle
            var audioCurrent = VideoPlayerSelected != null
                ? VideoPlayerSelected.videoPlayer.audioPlaybackMode.ToString()
                : "None Selected";
            var audioBtn = top.AddButton("Audio Mode: " + audioCurrent, "VideoPlayerModSound",
                "Toggles audio mode between Direct, AudioSource, RoomScale<p>Currently: " + audioCurrent);
            audioBtn.OnPress += () =>
            {
                if (!Utils.IsVideoPlayerValid(VideoPlayerSelected)) { AlertNoPlayer(); return; }
                try
                {
                    var newMode = VideoPlayerSelected.videoPlayer.audioPlaybackMode switch
                    {
                        VideoPlayerUtils.AudioMode.Direct      => VideoPlayerUtils.AudioMode.AudioSource,
                        VideoPlayerUtils.AudioMode.AudioSource => VideoPlayerUtils.AudioMode.RoomScale,
                        VideoPlayerUtils.AudioMode.RoomScale   => VideoPlayerUtils.AudioMode.Direct,
                        _                                      => VideoPlayerUtils.AudioMode.AudioSource,
                    };
                    VideoPlayerSelected.videoPlayer.SetAudioMode(newMode);
                    audioBtn.ButtonText = $"Audio Mode: {newMode}";
                    audioBtn.ButtonTooltip = $"Toggles audio mode between Direct, AudioSource, RoomScale<p>Currently: {newMode}";
                }
                catch (Exception ex)
                {
                    MelonLogger.Error("Error when changing video audio source\n" + ex);
                    audioBtn.ButtonText = "Audio Mode: Error";
                    QuickMenuAPI.ShowAlertToast("Error when changing video audio source.", 3);
                }
            };

            // Resolution multi-select
            var resCurrent = VideoPlayerSelected != null
                ? ((int)VideoPlayerSelected.videoPlayer.maxResolution).ToString()
                : "None Selected";
            var resBtn = top.AddButton("Set Video Resolution", "VideoPlayerModRes",
                "Set Video Max Resolution<p>Currently: " + resCurrent);
            resBtn.OnPress += () =>
            {
                if (!Utils.IsVideoPlayerValid(VideoPlayerSelected)) { AlertNoPlayer(); return; }
                var options = Enum.GetValues(typeof(VideoPlayerUtils.Resolution))
                    .Cast<int>().Select(v => v.ToString()).ToArray();
                var current = ((int)VideoPlayerSelected.videoPlayer.maxResolution).ToString();
                var ms = new MultiSelection("Video Resolution", options, Array.IndexOf(options, current));
                QuickMenuAPI.OpenMultiSelect(ms);
                ms.OnOptionUpdated += idx =>
                {
                    VideoPlayerSelected.videoPlayer.SetMaxResolution(int.Parse(options[idx]));
                };
            };

            // Audio only toggle
            var audioOnly = VideoPlayerSelected?.videoPlayer?.VideoPlayer != null && VideoPlayerSelected.videoPlayer.ytAudioOnly;
            var audioOnlyToggle = top.AddToggle("Audio Only", "Toggle Audio Only mode for Youtube<p>(Requires video reload)", audioOnly);
            audioOnlyToggle.OnValueUpdated += v =>
            {
                if (Utils.IsVideoPlayerValid(VideoPlayerSelected))
                    VideoPlayerSelected.videoPlayer.ytAudioOnly = v;
                else AlertNoPlayer();
            };

            // Reload at timestamp
            var reloadBtn = top.AddButton("Reload video at timestamp", "VideoPlayerModReloadVideo",
                "Reloads the current video and sets it back to the current timestamp");
            reloadBtn.OnPress += () =>
            {
                if (!Utils.IsVideoPlayerValid(VideoPlayerSelected)) { AlertNoPlayer(); return; }
                if (TryingToReplay)
                    QuickMenuAPI.ShowAlertToast("Video reload already running", 2);
                else
                    MelonCoroutines.Start(Instance.ReloadVideo());
            };

            // Timestamp subpage
            TimeStampPage = top.AddPage("Set to TimeStamp", "VideoPlayerModTimestamp", "Set videoplayer to timestamp", ModCategory);
            TimeStampPageString = TimeStampPage.ElementID;
            if (init) CreatePageTimeStampPage();

            // Logs + Debug + Blank video
            var debugCat = AdvOptionsPage.AddCategory("", false);
            LogPage = debugCat.AddPage("Event Logs", "VideoPlayerModEventLog", "Video Player Event Logs", ModCategory);
            LogPageString = LogPage.ElementID;
            if (init) CreatePageLogPage();

            DebugPage = debugCat.AddPage("DebugPage", "VideoPlayerModDebug", "DebugPage", ModCategory);
            DebugPageString = DebugPage.ElementID;
            if (init) CreatePageDebug();

            var blankBtn = debugCat.AddButton("Load Blank Video", "VideoPlayerModPlayBlack", "Load a blank video, just 480p black with no audio");
            blankBtn.OnPress += () =>
            {
                if (!Utils.IsVideoPlayerValid(VideoPlayerSelected)) { AlertNoPlayer(); return; }
                QuickMenuAPI.ShowConfirm("Confirm", "Play Video?<p><p><p>Blank Video<p><p><p>1min long, Black, 480p", () =>
                {
                    VideoPlayerSelected.videoPlayer.SetVideoUrl("https://www.youtube.com/watch?v=-Pg819il8lY");
                    MelonCoroutines.Start(Instance.SetCurrentVideoNameDelay());
                }, () => { }, "Yes", "No");
            };

            // URL history
            var historyCat = AdvOptionsPage.AddCategory("", false);
            URLHistoryPage = historyCat.AddPage("URL History Page", "VideoPlayerModURLHistory",
                "Videos played on VideoPlayers since game start", ModCategory);
            URLHistoryPageString = URLHistoryPage.ElementID;
            if (init) CreatePageURLHistoryPage();

            var historyToggle = historyCat.AddToggle("URL History",
                "Record URL History for ALL VideoPlayers in the world - Checks every time a video is Played<p>With this off history is only saved when you have a VideoPlayer selected and interact with this menu.",
                videoHistory_En.Value);
            historyToggle.OnValueUpdated += v => videoHistory_En.Value = v;

            if (Utils.IsVideoPlayerValid(VideoPlayerSelected))
            {
                var watchToggle = historyCat.AddToggle("Watching Current Player",
                    "Toggles if you are 'watching' the selected player. Watching means if videos will load on the player",
                    VideoPlayerSelected.videoPlayer.IsWatching);
                watchToggle.OnValueUpdated += v =>
                {
                    if (!Utils.IsVideoPlayerValid(VideoPlayerSelected)) { AlertNoPlayer(); return; }
                    VideoPlayerSelected.videoPlayer.SetWatching(v);
                    CreatePageAdvOptionsPage(false);
                };

                if (VideoPlayerSelected.videoPlayer.UntrustedVideoPending)
                {
                    var untrusted = historyCat.AddButton("Play untrusted URL", "VideoPlayerMod-untrusted",
                        "Plays and allows for the session the untrusted URL host: " + VideoPlayerSelected.videoPlayer.lastNetworkVideoUrl);
                    untrusted.OnPress += () =>
                    {
                        if (!Utils.IsVideoPlayerValid(VideoPlayerSelected)) { AlertNoPlayer(); return; }
                        if (VideoPlayerSelected.videoPlayer.UntrustedVideoPending)
                            VideoPlayerSelected.videoPlayer.HandleUntrustedVideo(VideoPlayerUtils.UntrustedVideoAction.WatchAndAllowForSession);
                        CreatePageAdvOptionsPage(false);
                    };
                }
            }

            var themeCat = AdvOptionsPage.AddCategory("Theme");
            var tintToggle = themeCat.AddToggle("Tint icons with menu accent",
                "When enabled, this mod's icons are tinted with your CVR menu accent color.<p>Re-applies automatically when you change your menu color.",
                iconTint_En.Value);
            tintToggle.OnValueUpdated += v =>
            {
                iconTint_En.Value = v;
                MelonPreferences.Save();
                RefreshTintedIcons();
            };
        }

        private static void CreatePageTimeStampPage()
        {
            if (TimeStampPage != null && TimeStampPage.IsGenerated)
                TimeStampPage.ClearChildren();

            var totalSec = timeStampHour * 3600 + timeStampMin * 60 + timeStampSec;
            if (!Utils.IsVideoPlayerValid(VideoPlayerSelected))
            {
                TimeStampPage.AddCategory("No Player Selected");
                return;
            }
            if (VideoPlayerSelected.videoPlayer.VideoPlayer.Info.VideoMetaData.IsLivestream)
            {
                TimeStampPage.AddCategory("Video is a livestream: No time controls allowed");
                return;
            }
            if (!(VideoPlayerSelected.videoPlayer.VideoPlayer.Info.VideoMetaData.GetDuration() > 0.0))
            {
                TimeStampPage.AddCategory("No video playing");
                return;
            }

            var info = "Playing: " + Utils.VideoNameFormat(VideoPlayerSelected) + "<p>" +
                       "Currently at: " + Utils.FormatTime((float)VideoPlayerSelected.videoPlayer.VideoPlayer.Time) + "<p>" +
                       "End time: " + Utils.FormatTime((float)VideoPlayerSelected.videoPlayer.VideoPlayer.Info.VideoMetaData.GetDuration()) + "<p>";
            var infoCat = TimeStampPage.AddCategory("temp");
            infoCat.CategoryName = info;

            var setCat = TimeStampPage.AddCategory("Timestamp: " + Utils.FormatTime(totalSec));
            var setBtn = setCat.AddButton("Set to timestamp", "VideoPlayerModTimestamp",
                "Set video to " + Utils.FormatTime(totalSec));
            setBtn.OnPress += () =>
            {
                if (VideoPlayerSelected?.videoPlayer?.VideoPlayer != null &&
                    totalSec < VideoPlayerSelected.videoPlayer.VideoPlayer.Info.VideoMetaData.GetDuration() - 5.0)
                {
                    MelonLogger.Msg("Manual set timestamp to: " + Utils.FormatTime(totalSec) +
                        ", Was at: " + Utils.FormatTime((float)VideoPlayerSelected.videoPlayer.VideoPlayer.Time));
                    QuickMenuAPI.ShowAlertToast("Manual set timestamp to: " + Utils.FormatTime(totalSec), 2);
                    VideoPlayerSelected.videoPlayer.SetVideoTimestamp(totalSec);
                    CreatePageTimeStampPage();
                }
                else
                {
                    QuickMenuAPI.ShowAlertToast("Video Player does not exist or timestamp beyond length of video", 3);
                    MelonLogger.Msg("Video Player does not exist or timestamp beyond length of video");
                }
            };

            var setHour = setCat.AddButton("Set Hour", "VideoPlayerModClock-Hours", "Set the hour value");
            setHour.OnPress += () => QuickMenuAPI.OpenNumberInput("Hours", timeStampHour, a => { timeStampHour = (int)a; CreatePageTimeStampPage(); });
            var setMin = setCat.AddButton("Set Minutes", "VideoPlayerModClock-Minutes", "Set the minute value");
            setMin.OnPress += () => QuickMenuAPI.OpenNumberInput("Minutes", timeStampMin, a => { timeStampMin = (int)a; CreatePageTimeStampPage(); });
            var setSec = setCat.AddButton("Set Seconds", "VideoPlayerModClock-Seconds", "Set the second value");
            setSec.OnPress += () => QuickMenuAPI.OpenNumberInput("Seconds", timeStampSec, a => { timeStampSec = (int)a; CreatePageTimeStampPage(); });

            // Jog controls
            var jogCat = TimeStampPage.AddCategory("Jog Controllers");
            var jogs = new (string icon, int minutes)[]
            {
                ("VideoPlayerMod-jog-5", -5),
                ("VideoPlayerMod-jog-1", -1),
                ("VideoPlayerMod-jog1",   1),
                ("VideoPlayerMod-jog5",   5),
            };
            foreach (var (icon, minutes) in jogs)
            {
                var jogSec = 60f * minutes;
                var jogBtn = jogCat.AddButton($"{minutes} Min", icon, $"Set video {minutes} minute(s)");
                jogBtn.OnPress += () =>
                {
                    var target = (float)VideoPlayerSelected.videoPlayer.VideoPlayer.Time + jogSec;
                    if (VideoPlayerSelected?.videoPlayer?.VideoPlayer != null &&
                        target < VideoPlayerSelected.videoPlayer.VideoPlayer.Info.VideoMetaData.GetDuration() - 5.0 && target > 0f)
                    {
                        MelonLogger.Msg("Jogged timestamp to: " + Utils.FormatTime(target) +
                            ", Was at: " + Utils.FormatTime((float)VideoPlayerSelected.videoPlayer.VideoPlayer.Time));
                        QuickMenuAPI.ShowAlertToast("Jogged timestamp to: " + Utils.FormatTime(target), 2);
                        VideoPlayerSelected.videoPlayer.SetVideoTimestamp(target);
                        CreatePageTimeStampPage();
                    }
                    else
                    {
                        QuickMenuAPI.ShowAlertToast("Video Player does not exist or timestamp beyond length of video", 3);
                        MelonLogger.Msg("Video Player does not exist or timestamp beyond length of video");
                    }
                };
            }

            // Generated timestamp segments
            TimeStampPage.AddCategory("Generated Timestamps");
            var duration = (float)VideoPlayerSelected.videoPlayer.VideoPlayer.Info.VideoMetaData.GetDuration();
            if (!(duration > 0.0)) return;
            var step = duration / 10f;
            for (var i = 0; i < 10; i += 2)
            {
                var a = step * i;
                var b = step * (i + 1);
                var row = TimeStampPage.AddCategory("temp");
                row.CategoryName = "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; " +
                    Utils.FormatTime(a) +
                    " &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; " +
                    Utils.FormatTime(b) + " ";
                for (var col = 0; col < 2; col++)
                {
                    var target = col == 0 ? a : b;
                    var seekBtn = row.AddButton("Set videoplayer to time", "VideoPlayerModTimeStamp", "Set video to " + Utils.FormatTime(target));
                    seekBtn.OnPress += () =>
                    {
                        if (VideoPlayerSelected?.videoPlayer?.VideoPlayer != null &&
                            target < VideoPlayerSelected.videoPlayer.VideoPlayer.Info.VideoMetaData.GetDuration() - 5.0)
                        {
                            MelonLogger.Msg("Manual set segment timestamp to: " + Utils.FormatTime(target) +
                                ", Was at: " + Utils.FormatTime((float)VideoPlayerSelected.videoPlayer.VideoPlayer.Time));
                            QuickMenuAPI.ShowAlertToast("Manual set segment timestamp to: " + Utils.FormatTime(target), 2);
                            VideoPlayerSelected.videoPlayer.SetVideoTimestamp(target);
                            CreatePageTimeStampPage();
                        }
                        else
                        {
                            QuickMenuAPI.ShowAlertToast("Video Player does not exist or segment timestamp beyond length of video", 3);
                            MelonLogger.Msg("Video Player does not exist or segment timestamp beyond length of video");
                            CreatePageTimeStampPage();
                        }
                    };
                    var sendBtn = row.AddButton("Send time to custom input", "VideoPlayerModUp", "Send this time value to custom input above");
                    sendBtn.OnPress += () =>
                    {
                        timeStampHour = Utils.GetTimeSeg(target, "hour");
                        timeStampMin  = Utils.GetTimeSeg(target, "min");
                        timeStampSec  = Utils.GetTimeSeg(target, "sec");
                        CreatePageTimeStampPage();
                    };
                }
            }
        }

        private static void CreatePageLogPage()
        {
            if (LogPage != null && LogPage.IsGenerated) LogPage.ClearChildren();
            if (!Utils.IsVideoPlayerValid(VideoPlayerSelected))
            {
                LogPage.AddCategory("No Player Selected");
                return;
            }

            var top = LogPage.AddCategory("", false);
            var refreshBtn = top.AddButton("Refresh", "VideoPlayerModReset", "Refresh page");
            refreshBtn.OnPress += CreatePageLogPage;

            LogPage.AddCategory("Video player log entries:");
            if (VideoPlayerSelected.logEntries.Count == 0)
            {
                LogPage.AddCategory("None");
                return;
            }

            var ownerToken = "Unknown (" + MetaPort.Instance.ownerId + ")";
            var rx = new System.Text.RegularExpressions.Regex(@"(\d+\.\d+)");
            foreach (var entry in VideoPlayerSelected.logEntries)
            {
                var line = entry.Replace(ownerToken, AuthManager.Username);
                var m = rx.Match(line);
                if (m.Success && float.TryParse(m.Groups[1].Value, out var secs))
                    line = line.Replace(m.Groups[1].Value + " seconds", $"{m.Groups[1].Value} seconds ({Utils.FormatTime(secs)})");
                LogPage.AddCategory(line);
            }
        }

        private static void CreatePageDebug()
        {
            if (DebugPage != null && DebugPage.IsGenerated) DebugPage.ClearChildren();
            if (!Utils.IsVideoPlayerValid(VideoPlayerSelected))
            {
                DebugPage.AddCategory("No Player Selected");
                return;
            }

            var top = DebugPage.AddCategory("Debug Info:");
            var refreshBtn = top.AddButton("Refresh", "VideoPlayerModReset", "Refresh page");
            refreshBtn.OnPress += CreatePageDebug;

            var title = VideoPlayerSelected.videoPlayer.currentlySelectedVideo?.videoTitle ?? "";
            var playlist = VideoPlayerSelected.videoPlayer.currentlySelectedPlaylist?.playlistTitle ?? "";
            var vp = VideoPlayerSelected.videoPlayer.VideoPlayer;

            var sb = new StringBuilder();
            sb.Append("Video title: ").Append(title.Truncate(25)).Append("<p>");
            sb.Append("Playlist title: ").Append(playlist.Truncate(25)).Append("<p>");
            sb.Append("URL: ").Append(vp.Info.VideoMetaData.GetUrl()).Append("<p>");
            sb.Append($"Video FPS: {vp.Info.VideoMetaData.GetVideoFps()} Player FPS: {vp.Info.GetPlayerFps()}<p>");
            sb.Append("Connection: ").Append(vp.Info.IsConnectionLost() ? "Lost" : "Connected").Append("<p>");
            sb.Append($"Video Player Time: {vp.Info.Time}<p>");
            sb.Append($"Audio Player Time: {vp.Info.AudioTime}<p>");
            sb.Append($"Video Texture Res: {vp.Info.VideoMetaData.GetVideoWidth()} x {vp.Info.VideoMetaData.GetVideoHeight()}<p>");
            sb.Append($"Render Texture Res: {VideoPlayerSelected.videoPlayer.ProjectionTexture.width} x {VideoPlayerSelected.videoPlayer.ProjectionTexture.height}<p>");
            sb.Append("Video Codec: ").Append(GetVideoCodec()).Append("<p>");
            sb.Append($"Audio Mode: {VideoPlayerSelected.videoPlayer.audioPlaybackMode}<p>");
            sb.Append($"Audio Channels: {vp.Info.VideoMetaData.GetAudioChannels()}");

            var cat = DebugPage.AddCategory("temp");
            cat.CategoryName = sb.ToString();

            static string GetVideoCodec()
            {
                var codec = VideoPlayerSelected.videoPlayer.VideoPlayer.Info.VideoMetaData?.GetVideoCodec();
                return string.IsNullOrEmpty(codec) ? "Unknown" : codec;
            }
        }

        private static void CreatePageSponsorSkip()
        {
            if (SponsorSkipEvents != null && SponsorSkipEvents.IsGenerated)
                SponsorSkipEvents.ClearChildren();

            var top = SponsorSkipEvents.AddCategory("", false);
            var enableToggle = top.AddToggle("Enable", "Enable SponsorBlock for this VideoPlayer", sponsorSkip);
            enableToggle.OnValueUpdated += v => { sponsorSkip = v; StartSponsorSkip(); };

            var typesCat = SponsorSkipEvents.AddCategory("Segments types to skip");
            void RegisterSkipToggle(string label, string tip, MelonPreferences_Entry<bool> entry)
            {
                var t = typesCat.AddToggle(label, tip, entry.Value);
                t.OnValueUpdated += v =>
                {
                    entry.Value = v;
                    if (Utils.IsVideoPlayerValid(VideoPlayerSelected) && VideoPlayerSelected.videoPlayer.lastNetworkVideoUrl == sponsorskipVideo)
                        PrepSkipList();
                };
            }
            RegisterSkipToggle("Sponsor",     "Skip sponsor segments.<p>Doesn't skip if <5 seconds.",      sponsorSkip_sponsor);
            RegisterSkipToggle("Self Promo",  "Skip self promo segments.<p>Doesn't skip if <5 seconds.",   sponsorSkip_selfpromo);
            RegisterSkipToggle("Interaction", "Skip interaction segments.<p>Doesn't skip if <5 seconds.",  sponsorSkip_interaction);
            RegisterSkipToggle("Intro",       "Skip intro segments.<p>Doesn't skip if <5 seconds.",        sponsorSkip_intro);

            if (!sponsorSkip)
            {
                SponsorSkipEvents.AddCategory("SponsorBlock not enabled");
                return;
            }
            if (!Utils.IsVideoPlayerValid(VideoPlayerSelected))
            {
                SponsorSkipEvents.AddCategory("No video player selected");
                return;
            }
            if (sponsorskipVideo == "API_Repsonse_Not_Found")
            {
                SponsorSkipEvents.AddCategory("Video not found in SponsorBlock API");
                return;
            }
            if (VideoPlayerSelected.videoPlayer.lastNetworkVideoUrl != sponsorskipVideo)
            {
                SponsorSkipEvents.AddCategory("SponsorBlock hasn't grabbed current video yet, or no video is playing");
                SponsorSkipEvents.AddCategory("Go back a page to refresh this menu");
                return;
            }
            if (sponsorskipResult == null || sponsorskipResult.Length == 0)
            {
                SponsorSkipEvents.AddCategory("SponsorBlock didn't find any results");
                return;
            }

            SponsorSkipEvents.AddCategory("------------ All events for video ------------");
            foreach (var x in sponsorskipResult.OrderBy(s => s.segment[0]))
            {
                var info = "Type: " + Utils.SkipCatSwitch(x.category) +
                    (x.category == "poi_highlight" ? "   <<<<<<<<<<<<<<" : "") + "<p>";
                if (!string.IsNullOrEmpty(x.description))
                    info += "Desc: " + x.description + "<p>";
                info += Utils.FormatTime(x.segment[0]) + " - " + Utils.FormatTime(x.segment[1]) + "<p>";

                var row = SponsorSkipEvents.AddCategory("temp");
                row.CategoryName = info;
                var seg = x.segment;

                var toStart = row.AddButton("Jump to Start", "VideoPlayerModArrow-Left", "Set video to start time of event " + Utils.FormatTime(seg[0]));
                toStart.OnPress += () =>
                {
                    if (Utils.IsVideoPlayerValid(VideoPlayerSelected) && VideoPlayerSelected.videoPlayer.lastNetworkVideoUrl == sponsorskipVideo)
                    {
                        MelonLogger.Msg("|SponsorBlock| Manual skip to: " + Utils.FormatTime(seg[0]) +
                            ", Was at: " + Utils.FormatTime((float)VideoPlayerSelected.videoPlayer.VideoPlayer.Time));
                        QuickMenuAPI.ShowAlertToast("Manual skip to: " + Utils.FormatTime(seg[0]), 2);
                        lastskip = seg;
                        VideoPlayerSelected.videoPlayer.SetVideoTimestamp(seg[0]);
                    }
                    else
                    {
                        QuickMenuAPI.ShowAlertToast("Video Player does not exist or wrong video playing.", 3);
                        MelonLogger.Msg("Video Player Not Selected or does not exist or wrong video playing - skipEvent");
                    }
                };
                var toEnd = row.AddButton("Jump to End", "VideoPlayerModArrow-Right", "Set video to end time of event " + Utils.FormatTime(seg[1]));
                toEnd.OnPress += () =>
                {
                    if (!Utils.IsVideoPlayerValid(VideoPlayerSelected) || VideoPlayerSelected.videoPlayer.lastNetworkVideoUrl != sponsorskipVideo)
                    {
                        QuickMenuAPI.ShowAlertToast("Video Player does not exist or wrong video playing.", 3);
                        MelonLogger.Msg("Video Player Not Selected or does not exist or wrong video playing - skipEvent");
                        return;
                    }
                    if (seg[1] > VideoPlayerSelected.videoPlayer.VideoPlayer.Info.VideoMetaData.GetDuration() - 5.0)
                    {
                        MelonLogger.Msg("|SponsorBlock| Not skipping to " + Utils.FormatTime(seg[1]) + ", less than 5 seconds till end of video");
                        QuickMenuAPI.ShowAlertToast("Not skipping, less than 5 seconds till end of video!", 3);
                    }
                    else
                    {
                        MelonLogger.Msg("|SponsorBlock| Manual skip to: " + Utils.FormatTime(seg[1]) +
                            ", Was at: " + Utils.FormatTime((float)VideoPlayerSelected.videoPlayer.VideoPlayer.Time));
                        QuickMenuAPI.ShowAlertToast("Manual skip to: " + Utils.FormatTime(seg[1]), 2);
                        lastskip = seg;
                        VideoPlayerSelected.videoPlayer.SetVideoTimestamp(seg[1]);
                    }
                };
            }
            SponsorSkipEvents.AddCategory("", false);
            SponsorSkipEvents.AddCategory("", false);
            SponsorSkipEvents.AddCategory("Uses SponsorBlock data from https://sponsor.ajay.app/");
        }

        private static void CreatePageSavedURLsPage()
        {
            if (savedURLsPage != null && savedURLsPage.IsGenerated)
                savedURLsPage.ClearChildren();
            if (savedURLs.Count == 0)
            {
                savedURLsPage.AddCategory("No saved URLs found");
                return;
            }

            foreach (var x in savedURLs.OrderBy(kv => kv.Value.Item2).Reverse())
            {
                var row = savedURLsPage.AddCategory("temp");
                row.CategoryName = x.Value.Item1 + "<p>" + x.Value.Item2.ToString("yyyy'-'MM'-'dd");

                var url = x.Key;
                var name = x.Value.Item1;
                var dt = x.Value.Item2;

                var play = row.AddButton("Play Video", "VideoPlayerModPastePlay", "Play the video: " + url + "<p>" + name);
                play.OnPress += () =>
                {
                    if (!Utils.IsVideoPlayerValid(VideoPlayerSelected)) { AlertNoPlayer(); return; }
                    QuickMenuAPI.ShowConfirm("Confirm", "Play Video?<p><p><p>" + url + "<p><p><p>" + name, () =>
                    {
                        VideoPlayerSelected.videoPlayer.SetVideoUrl(url);
                        MelonCoroutines.Start(Instance.SetCurrentVideoNameDelay());
                    }, () => { }, "Yes", "No");
                };
                var remove = row.AddButton("Remove from List", "VideoPlayerModRemoveLink", "Removes the video: " + url + "<p>" + name);
                remove.OnPress += () =>
                {
                    QuickMenuAPI.ShowConfirm("Confirm", "Remove video from saved list?<p><p><p>" + url + "<p><p><p>" + name, () =>
                    {
                        RemoveURL(url);
                        CreatePageSavedURLsPage();
                    }, () => { }, "Yes", "No");
                };
                row.AddButton("Copy URL to Clipboard", "VideoPlayerMod-copy", "Copies the URL to your system clipboard")
                    .OnPress += () => GUIUtility.systemCopyBuffer = url;
            }
        }

        private static void CreatePageURLHistoryPage()
        {
            if (URLHistoryPage != null && URLHistoryPage.IsGenerated)
                URLHistoryPage.ClearChildren();
            if (historyURLs.Count == 0)
            {
                URLHistoryPage.AddCategory("No history found");
                return;
            }

            foreach (var x in Enumerable.Reverse(historyURLs))
            {
                var row = URLHistoryPage.AddCategory("temp");
                row.CategoryName = x.Item2 + "<p>" + x.Item3.ToString("yyyy'-'MM'-'dd HH:mm");

                var url = x.Item1;
                var name = x.Item2;
                var dt = x.Item3;

                var play = row.AddButton("Play Video", "VideoPlayerModPastePlay", "Play the video: " + url + "<p>" + name);
                play.OnPress += () =>
                {
                    if (!Utils.IsVideoPlayerValid(VideoPlayerSelected)) { AlertNoPlayer(); return; }
                    QuickMenuAPI.ShowConfirm("Confirm", "Play Video?<p><p><p>" + url + "<p><p><p>" + name, () =>
                    {
                        VideoPlayerSelected.videoPlayer.SetVideoUrl(url);
                        MelonCoroutines.Start(Instance.SetCurrentVideoNameDelay());
                    }, () => { }, "Yes", "No");
                };
                var save = row.AddButton("Save URL", "VideoPlayerModSave",
                    $"Stores the current video URL into the ChilloutVR/{FolderRoot} Folder.");
                save.OnPress += () =>
                {
                    if (!savedURLs.ContainsKey(url))
                        savedURLs.Add(url, (name, dt));
                };
                row.AddButton("Copy URL to Clipboard", "VideoPlayerMod-copy", "Copies the URL to your system clipboard")
                    .OnPress += () => GUIUtility.systemCopyBuffer = url;
            }
        }

        private sealed class Contributor
        {
            internal string Key;
            internal string UserId;
            internal string Label;
            internal string Tooltip;
            internal string GitHubUrl;
            internal bool AvatarReady;
        }

        private static readonly Contributor[] Contributors =
        {
            new Contributor
            {
                Key = "Shin",
                UserId = "aeae3ecf-9826-4e03-830f-96e8240c19e3",
                Label = "DjShinter",
                Tooltip = "DjShinter (Shin) - Original author of Video Remote.<p>Click to open their GitHub.",
                GitHubUrl = "https://github.com/DjShinter",
            },
            new Contributor
            {
                Key = "Nirvash",
                UserId = "e3dc730b-6aab-6f2f-9ae5-4f33037beb20",
                Label = "Nirvash",
                Tooltip = "Nirvash - Added SponsorBlock, URL history, timestamp controls, debug page, audio-only mode and resolution selector.<p>Click to open their GitHub.",
                GitHubUrl = "https://github.com/Nirv-git",
            },
        };

        private static void CreatePageContributors()
        {
            if (ContributorsPage != null && ContributorsPage.IsGenerated)
                ContributorsPage.ClearChildren();

            var people = ContributorsPage.AddCategory("", false);
            foreach (var c in Contributors)
            {
                var icon = c.AvatarReady ? "ContribAvatar_" + c.Key : "VideoPlayerModContributors";
                var btn = people.AddButton(c.Label, icon, c.Tooltip);
                var captured = c;
                btn.OnPress += () => OpenContributorGitHub(captured);
            }
        }

        // Only opens the GitHub links hardcoded in Contributors, after the user confirms
        private static void OpenContributorGitHub(Contributor c)
        {
            if (c == null || Array.IndexOf(Contributors, c) < 0) return;
            var url = c.GitHubUrl;
            if (string.IsNullOrEmpty(url) || !url.StartsWith("https://github.com/", StringComparison.Ordinal)) return;

            QuickMenuAPI.ShowConfirm("Open GitHub", $"Open {c.Label}'s GitHub in your browser?<p><p>{url}",
                () => Application.OpenURL(url), () => { }, "Open", "Cancel");
        }

        private static void FetchContributorAvatars()
        {
            foreach (var c in Contributors)
                MelonCoroutines.Start(FetchContributorAvatar(c));
        }

        private static IEnumerator FetchContributorAvatar(Contributor c)
        {
            string imageUrl = null;
            Exception apiError = null;
            var task = ApiConnection.MakeRequest<UserDetailsResponse>(ApiConnection.ApiOperation.UserDetails, new { userID = c.UserId });
            while (!task.IsCompleted) yield return null;
            try
            {
                var resp = task.Result;
                imageUrl = resp?.Data?.ImageUrl;
            }
            catch (Exception ex) { apiError = ex; }

            if (apiError != null)
            {
                MelonLogger.Warning($"Avatar API failed for {c.Key}: {apiError.Message}");
                yield break;
            }
            if (string.IsNullOrEmpty(imageUrl))
            {
                MelonLogger.Warning($"Avatar API returned no ImageUrl for {c.Key}");
                yield break;
            }

            using var req = UnityWebRequest.Get(imageUrl);
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success || req.downloadHandler?.data == null)
            {
                MelonLogger.Warning($"Avatar download failed for {c.Key}: {req.error}");
                yield break;
            }

            Texture2D tex = null;
            byte[] pngBytes = null;
            try
            {
                tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(req.downloadHandler.data))
                    pngBytes = tex.EncodeToPNG();
                else
                    MelonLogger.Warning($"Could not decode avatar image for {c.Key}");
            }
            finally
            {
                if (tex != null) UnityEngine.Object.Destroy(tex);
            }
            if (pngBytes == null) yield break;

            QuickMenuAPI.PrepareIcon(ModCategory, "ContribAvatar_" + c.Key, new MemoryStream(pngBytes));
            c.AvatarReady = true;
            try { CreatePageContributors(); }
            catch (Exception ex) { MelonLogger.Warning("Failed to refresh Contributors page: " + ex.Message); }
        }

        private static void SetCurrentVideoName()
        {
            if (Utils.IsVideoPlayerValid(VideoPlayerSelected))
            {
                var t = VideoPlayerSelected.videoPlayer.VideoPlayer.Info.VideoMetaData.IsLivestream
                    ? "Livestream"
                    : Utils.FormatTime((float)VideoPlayerSelected.videoPlayer.VideoPlayer.Time) + " / " +
                      Utils.FormatTime((float)VideoPlayerSelected.videoPlayer.VideoPlayer.Info.VideoMetaData.GetDuration());
                videoName.CategoryName = Utils.VideoState(VideoPlayerSelected) + Utils.VideoNameFormat(VideoPlayerSelected) + "<p>" + t;

                if (!VideoPlayerSelected.videoPlayer.IsWatching)
                    videoName.CategoryName = "!! Video Player not Watched !!<p>Use toggle in VideoPlayer Options Page in mod -OR- Press watch on Videoplayer UI";
                if (VideoPlayerSelected.videoPlayer.UntrustedVideoPending)
                    videoName.CategoryName = "!! Untrusted Video Pending !!<p>Use toggle in VideoPlayer Options Page in mod -OR- Press watch on Videoplayer UI";
            }
            else
            {
                videoName.CategoryName = "No video player selected";
            }
        }

        private IEnumerator SetCurrentVideoNameDelay()
        {
            for (var i = 0; i <= 10; i++)
            {
                yield return new WaitForSecondsRealtime(1f);
                SetCurrentVideoName();
            }
        }

        internal static void AllURLhistory()
        {
            if (Time.time < historyLastCheck + 10f) return;
            try
            {
                historyLastCheck = Time.time;
                foreach (var cvrVp in UnityEngine.Object.FindObjectsByType<CVRVideoPlayer>(FindObjectsSortMode.None))
                {
                    var uis = Traverse.Create(cvrVp).Field("VideoPlayerUis").GetValue<List<IVideoPlayerUi>>();
                    foreach (var vp in uis.Cast<ViewManagerVideoPlayer>())
                    {
                        if (!Utils.IsVideoPlayerValid(vp) || string.IsNullOrWhiteSpace(vp.videoUrl.text)) continue;
                        var name = Utils.VideoNameFormat(vp);
                        var key = vp.videoUrl.text;
                        if (!Enumerable.Reverse(historyURLs).Take(10).Any(tuple => tuple.Item1 == key))
                        {
                            historyURLs.Add((vp.videoUrl.text, name, DateTime.Now));
                            SaveHistoryUrl(name, vp.videoUrl.text);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Error in AllURLhistory \n" + ex);
            }
        }

        private static void SingleURLhistory()
        {
            if (!Utils.IsVideoPlayerValid(VideoPlayerSelected) || string.IsNullOrWhiteSpace(VideoPlayerSelected.videoUrl.text)) return;
            var name = Utils.VideoNameFormat(VideoPlayerSelected);
            var key = VideoPlayerSelected.videoUrl.text;
            if (!Enumerable.Reverse(historyURLs).Take(10).Any(tuple => tuple.Item1 == key))
            {
                historyURLs.Add((VideoPlayerSelected.videoUrl.text, name, DateTime.Now));
                SaveHistoryUrl(name, VideoPlayerSelected.videoUrl.text);
            }
        }

        private static void SaveHistoryUrl(string vidname, string url)
        {
            const string dateFmt = "yyyy'-'MM'-'dd";
            if (string.IsNullOrWhiteSpace(url)) return;
            var path = FolderRoot + historyURLFileName;
            using var sw = File.Exists(path) ? File.AppendText(path) : File.CreateText(path);
            sw.WriteLine($"{DateTime.Now.ToString(dateFmt)} ␟ {vidname} ␟ {url}");
        }

        private IEnumerator ReloadVideo()
        {
            TryingToReplay = true;
            if (VideoPlayerSelected?.videoPlayer != null)
            {
                var existingName = Utils.VideoNameFormat(VideoPlayerSelected);
                var existingURL = VideoPlayerSelected.videoPlayer.lastNetworkVideoUrl;
                var existingTs = VideoPlayerSelected.videoPlayer.VideoPlayer.Time;
                var timeout = Time.time + 30f;
                MelonLogger.Msg("Replaying video at Timestamp - Name: " + existingName + "\n" +
                                $"URL: {existingURL} - TimeStamp: {existingTs}, TimeStamp: {Utils.FormatTime((float)existingTs)}");
                VideoPlayerSelected.videoPlayer.SetVideoUrl(existingURL);
                while (Time.time < timeout && VideoPlayerSelected?.videoPlayer != null)
                {
                    if (VideoPlayerSelected.videoPlayer.VideoPlayer.Time < existingTs &&
                        Utils.VideoNameFormat(VideoPlayerSelected) == existingName)
                    {
                        yield return new WaitForSecondsRealtime(2f);
                        MelonLogger.Msg($"Setting timestamp {existingTs}");
                        VideoPlayerSelected.videoPlayer.SetVideoTimestamp(existingTs);
                        TryingToReplay = false;
                        yield break;
                    }
                    yield return new WaitForSecondsRealtime(0.5f);
                }
            }
            TryingToReplay = false;
        }

        private static void RefreshMainPage()
        {
            if (lastQMPage != MainPageString) return;
            try
            {
                var players = UnityEngine.Object.FindObjectsByType<CVRVideoPlayer>(FindObjectsSortMode.None);
                if (players.Length == 1)
                {
                    var uis = Traverse.Create(players[0]).Field("VideoPlayerUis").GetValue<List<IVideoPlayerUi>>();
                    foreach (var vp in uis.Cast<ViewManagerVideoPlayer>())
                        VideoPlayerSelected = vp;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Error getting default videoplayer \n" + ex);
            }
            SetCurrentVideoName();
            SingleURLhistory();
            if (VideoPlayerSelected != null)
                volumeSlider.SetSliderValue(VideoPlayerSelected.videoPlayer.playbackVolume);
            // Keep the toggles in sync with the actual objects (they may have self-destructed).
            if (fullscreenToggle != null) fullscreenToggle.ToggleValue = IsFullscreenActive();
            if (vpCamToggle != null) vpCamToggle.ToggleValue = IsVideoPlayerCamActive();
        }

        private static void RefreshPage(bool laggyPages)
        {
            if (lastQMPage == VideoFolderString)        PopulateVideoList();
            if (lastQMPage == AdvOptionsString)         CreatePageAdvOptionsPage(init: false);
            if (lastQMPage == SponsorSkipEventsString)  CreatePageSponsorSkip();
            if (lastQMPage == savedURLsPageString && laggyPages) CreatePageSavedURLsPage();
            if (lastQMPage == TimeStampPageString)      CreatePageTimeStampPage();
            if (lastQMPage == LogPageString)            CreatePageLogPage();
            if (lastQMPage == DebugPageString)          CreatePageDebug();
            if (lastQMPage == URLHistoryPageString && laggyPages) { CreatePageURLHistoryPage(); SingleURLhistory(); }
        }

        private static void OnPageOpen(string targetPage, string lastPage)
        {
            lastQMPage = targetPage;
            RefreshPage(laggyPages: true);
            RefreshMainPage();
            // Pages built lazily by RefreshPage create their buttons with base icon names;
            // re-apply versioning so they pick up the tinted PNGs we already registered.
            if (iconTint_En.Value) RetintAllButtons();

            // Lazy-load avatars the first time the contributors page is opened.
            // Doing it earlier (in OnInitializeMelon / Init scene) fails because
            // the user isn't authenticated yet.
            if (targetPage == ContributorsPageString && !_avatarsRequested)
            {
                _avatarsRequested = true;
                FetchContributorAvatars();
            }
        }

        private static void OnPageBack(string targetPage, string lastPage)
        {
            lastQMPage = targetPage;
            RefreshPage(laggyPages: true);
            RefreshMainPage();
            if (iconTint_En.Value) RetintAllButtons();
        }

        internal static void QMtoggle(bool show)
        {
            if (!show) return;
            RefreshPage(laggyPages: false);
            RefreshMainPage();
        }

        private static void ToggleLocalScreen()
        {
            if (localScreen != null && !localScreen.Equals(null))
            {
                try { UnityEngine.Object.Destroy(localScreen); }
                catch (Exception ex) { MelonLogger.Msg(System.ConsoleColor.DarkRed, ex.ToString()); }
                localScreen = null;
            }
            else if (VideoPlayerSelected != null)
            {
                var cam = Camera.main.gameObject;
                var pos = cam.transform.position + cam.transform.forward * 2f;
                var obj = GameObject.CreatePrimitive(PrimitiveType.Plane);
                obj.transform.position = pos;
                obj.transform.LookAt(cam.transform);
                obj.transform.rotation *= Quaternion.AngleAxis(90f, Vector3.right);
                obj.name = "LocalVideoScreen-Mod";
                obj.transform.localScale = new Vector3(1.777f * sizeScale / 10f, sizeScale / 10f, sizeScale / 10f);

                var mc = obj.GetComponent<MeshCollider>();
                if (mc != null) UnityEngine.Object.DestroyImmediate(mc);

                var bc = obj.AddComponent<BoxCollider>();
                bc.size = new Vector3(10f, 0.05f, 10f);
                bc.isTrigger = true;
                bc.enabled = true;

                var mat = new Material(Shader.Find("Unlit/Texture"))
                {
                    mainTexture = VideoPlayerSelected.videoPlayer.ProjectionTexture
                };
                obj.GetComponent<MeshRenderer>().material = mat;

                var pickup = obj.AddComponent<CVRPickupObject>();
                pickup.maximumGrabDistance = 30f;
                pickup.enabled = pickupable;
                pickup.gripType = CVRPickupObject.GripType.Free;

                localScreen = obj;
            }
        }

        private static void UpdateLocalScreen()
        {
            if (localScreen == null || localScreen.Equals(null)) return;
            localScreen.transform.localScale = new Vector3(1.777f * sizeScale / 10f, sizeScale / 10f, sizeScale / 10f);
            localScreen.GetComponent<CVRPickupObject>().enabled = pickupable;
        }

        private static bool IsFullscreenActive()
        {
            return fullscreenObj != null && !fullscreenObj.Equals(null);
        }

        private static void EnableFullscreen()
        {
            if (IsFullscreenActive()) return;
            if (!Utils.IsVideoPlayerValid(VideoPlayerSelected) || Camera.main == null)
            {
                AlertNoPlayer();
                if (fullscreenToggle != null) fullscreenToggle.ToggleValue = false;
                return;
            }

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "VideoRemote-Fullscreen";
            var fs = quad.AddComponent<FullscreenScreen>();
            fs.VideoPlayer = VideoPlayerSelected.videoPlayer;
            fs.SizeMultiplier = fullscreenSize;
            fullscreenObj = quad;
        }

        private static void DestroyFullscreen()
        {
            if (fullscreenObj != null && !fullscreenObj.Equals(null))
            {
                try { UnityEngine.Object.Destroy(fullscreenObj); }
                catch (Exception ex) { MelonLogger.Msg(System.ConsoleColor.DarkRed, ex.ToString()); }
            }
            fullscreenObj = null;
        }

        private static bool IsVideoPlayerCamActive()
        {
            return vpCamObj != null && !vpCamObj.Equals(null);
        }

        private static void EnableVideoPlayerCam()
        {
            if (IsVideoPlayerCamActive()) return;
            if (!Utils.IsVideoPlayerValid(VideoPlayerSelected) || Camera.main == null)
            {
                AlertNoPlayer();
                if (vpCamToggle != null) vpCamToggle.ToggleValue = false;
                return;
            }

            // A head-locked window showing the video texture - like Fullscreen, but small and
            // offset to the side so it sits out of the way.
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "VideoRemote-VideoPlayerCam";
            var fs = quad.AddComponent<FullscreenScreen>();
            fs.VideoPlayer = VideoPlayerSelected.videoPlayer;
            fs.FillView = false;
            fs.AbsoluteHeight = vpCamSize;
            fs.Distance = vpCamDistance;
            fs.OffsetX = vpCamOffsetX;
            fs.OffsetY = vpCamOffsetY;
            vpCamObj = quad;
        }

        private static void ApplyVideoPlayerCam()
        {
            if (!IsVideoPlayerCamActive()) return;
            var fs = vpCamObj.GetComponent<FullscreenScreen>();
            if (fs == null) return;
            fs.AbsoluteHeight = vpCamSize;
            fs.OffsetX = vpCamOffsetX;
            fs.OffsetY = vpCamOffsetY;
        }

        private static void DestroyVideoPlayerCam()
        {
            if (vpCamObj != null && !vpCamObj.Equals(null))
            {
                try { UnityEngine.Object.Destroy(vpCamObj); }
                catch (Exception ex) { MelonLogger.Msg(System.ConsoleColor.DarkRed, ex.ToString()); }
            }
            vpCamObj = null;
        }

        private static void StartSponsorSkip()
        {
            if (sponsorSkipCheckCoroutine != null)
                MelonCoroutines.Stop(sponsorSkipCheckCoroutine);
            if (sponsorSkip)
                sponsorSkipCheckCoroutine = MelonCoroutines.Start(SponsorSkipCheck());
        }

        private static IEnumerator SponsorSkipCheck()
        {
            var startedSkips = false;
            while (sponsorSkip)
            {
                if (Utils.IsVideoPlayerValid(VideoPlayerSelected))
                {
                    if (VideoPlayerSelected.videoPlayer.lastNetworkVideoUrl != lastvideo &&
                        !string.IsNullOrWhiteSpace(VideoPlayerSelected.videoPlayer.lastNetworkVideoUrl))
                    {
                        startedSkips = false;
                        lastvideo = VideoPlayerSelected.videoPlayer.lastNetworkVideoUrl;
                        MelonLogger.Msg("|SponsorBlock| New video found: " + lastvideo);
                        _ = GetAsync(lastvideo);
                    }
                    else if (VideoPlayerSelected.videoPlayer.lastNetworkVideoUrl == sponsorskipVideo && !startedSkips)
                    {
                        MelonLogger.Msg("|SponsorBlock| Current video has skip info - " + sponsorskipVideo);
                        PrepSkipList();
                        startedSkips = true;
                        lastskip = new float[2];
                        if (sponsorSkipLiveCoroutine != null)
                            MelonCoroutines.Stop(sponsorSkipLiveCoroutine);
                        sponsorSkipLiveCoroutine = MelonCoroutines.Start(SponsorSkipping());
                        CreatePageSponsorSkip();
                    }
                }
                yield return new WaitForSeconds(1f);
            }
            sponsorSkip = false;
        }

        private static async Task GetAsync(string url)
        {
            if (!url.Contains("youtu")) return;
            var uri = new Uri(url);
            var query = Utils.ExtractVideoIdFromUri(uri);
            var videoId = string.IsNullOrEmpty(query) ? uri.Segments.Last() : query;
            var requestURL = "https://sponsor.ajay.app/api/skipSegments?videoID=" + videoId +
                             "&category=sponsor&category=selfpromo&category=interaction&category=intro&category=preview" +
                             "&category=music_offtopic&category=poi_highlight&category=chapter";
            var request = (HttpWebRequest)WebRequest.Create(requestURL);
            try
            {
                using var response = (HttpWebResponse)await request.GetResponseAsync();
                using var stream = response.GetResponseStream();
                using var reader = new StreamReader(stream);
                var data = await reader.ReadToEndAsync();
                sponsorskipVideo = url;
                sponsorskipResult = JsonConvert.DeserializeObject<SponsorSkipSegment[]>(data);
            }
            catch (Exception ex)
            {
                MelonLogger.Msg("|SponsorBlock| Couldn't get video from API: " + ex.Message);
                sponsorskipVideo = "API_Repsonse_Not_Found";
                CreatePageSponsorSkip();
            }
        }

        private static void PrepSkipList()
        {
            sponsorskips.Clear();
            foreach (var s in sponsorskipResult)
            {
                if ((s.category == "sponsor"     && sponsorSkip_sponsor.Value) ||
                    (s.category == "selfpromo"   && sponsorSkip_selfpromo.Value) ||
                    (s.category == "interaction" && sponsorSkip_interaction.Value) ||
                    (s.category == "intro"       && sponsorSkip_intro.Value))
                {
                    sponsorskips.Add((s.category, s.segment));
                }
            }
        }

        private static IEnumerator SponsorSkipping()
        {
            while (sponsorSkip && VideoPlayerSelected?.videoPlayer?.VideoPlayer != null &&
                   VideoPlayerSelected.videoPlayer.lastNetworkVideoUrl == sponsorskipVideo)
            {
                yield return new WaitForSeconds(0.25f);
                var dur = VideoPlayerSelected.videoPlayer.VideoPlayer.Info.VideoMetaData.GetDuration();
                foreach (var (cat, seg) in sponsorskips)
                {
                    var time = VideoPlayerSelected.videoPlayer.VideoPlayer.Time;
                    if (time > seg[0] && time < seg[1] - 5f &&
                        seg[0] != lastskip[0] && seg[1] != lastskip[1] &&
                        seg[0] < dur - 5.0 && seg[1] < dur - 5.0)
                    {
                        MelonLogger.Msg("|SponsorBlock| Skipping to: " + Utils.FormatTime(seg[1]) +
                            ", Was at: " + Utils.FormatTime((float)time) +
                            ", Due to: " + Utils.SkipCatSwitch(cat));
                        CohtmlHud.Instance.ViewDropText("|SponsorBlock|",
                            Utils.SkipCatSwitch(cat),
                            "Skipping to: " + Utils.FormatTime(seg[1]),
                            "videoremote-sponsorblock", parseHtml: false);
                        lastskip = seg;
                        VideoPlayerSelected.videoPlayer.SetVideoTimestamp(seg[1]);
                        break;
                    }
                }
            }
        }
    }
}
