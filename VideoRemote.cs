using ABI_RC.VideoPlayer.Scripts;
using BTKUILib;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Semver;
using ABI.CCK.Components;
using ABI_RC.Core.InteractionSystem;
using HarmonyLib;
using System.Net;
using System.Web;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ABI_RC.Core.UI;
using ABI_RC.VideoPlayer;
using ABI_RC.Core.Networking;
using ABI_RC.Core.Savior;
using System.Diagnostics;


namespace VideoRemote
{
    public static class ModBuildInfo
    {
        public const string Name = "Video Remote";
        public const string Author = "Shin, Nirvash";
        public const string Version = "1.7.1";
        public const string Description = "This allows you to use the video player with the menu.";
        public const string DownloadLink = "https://github.com/Nirv-git/VideoRemote/releases";
    }
    public class VideoRemoteMod : MelonMod
    {
        public static VideoRemoteMod Instance;

        public static MelonPreferences_Category cat;
        private const string catagory = "VideoRemoteMod";
        public static MelonPreferences_Entry<bool> sponsorSkip_sponsor;
        public static MelonPreferences_Entry<bool> sponsorSkip_selfpromo;
        public static MelonPreferences_Entry<bool> sponsorSkip_interaction;
        public static MelonPreferences_Entry<bool> sponsorSkip_intro;
        public static MelonPreferences_Entry<bool> videoHistory_En;

        private static Category VideoPlayerListMain, VideoPlayerList, AdvancedOptions, videoName;
        private static Page AdvOptionsPage, TimeStampPage, LogPage, DebugPage, SponsorSkipEvents, savedURLsPage, URLHistoryPage;
        private static SliderFloat volumeSilder;
        private static string VideoFolderString, MainPageString, SponsorSkipEventsString, AdvOptionsString, TimeStampPageString, LogPageString, DebugPageString, savedURLsPageString, URLHistoryPageString;
        private static string lastQMPage = "";

        public static bool _initalized = new();
        private static ViewManagerVideoPlayer VideoPlayerSelected = new();
        private const string FolderRoot = "UserData/VideoRemote/";
        private const string savedURLsFileName = "savedURLsList.txt";
        private const string histroyURLFileName = "historyURLsList.txt";
        public static Dictionary<string, (string, DateTime)> savedURLs = new Dictionary<string, (string, DateTime)>(); //URL,(Name,Date)
        public static List<(string, string, DateTime)> historyURLs = new List<(string, string, DateTime)>(); //URL,(Name,Date)


        private static GameObject localScreen;
        private static bool pickupable = false;
        private static float sizeScale = 1;

        private static bool sponsorSkip = false;
        private static object sponsorSkipCheckCoroutine, sponsorSkipLiveCoroutine;
        private static string lastvideo = "zzzzzzzzzzzzzzzzzzzzz09";
        private static string sponsorskipVideo = "xxxxxxxxxxxxxxxxxxxx08";
        private static SponsorSkipSegment[] sponsorskipResult;
        private static List<(string, float[])> sponsorskips = new List<(string, float[])>();
        private static float[] lastskip = new float[2];

        private static bool TryingToReplay = false;

        private static int timeStampHour = 0;
        private static int timeStampMin = 0;
        private static int timeStampSec = 0;

        private static float historyLastCheck = 0;

        public override void OnInitializeMelon()
        {
            Instance = this;

            cat = MelonPreferences.CreateCategory(catagory, "Video Remote", true);
            sponsorSkip_sponsor = MelonPreferences.CreateEntry(catagory, nameof(sponsorSkip_sponsor), true, "Skips Segment Sponsor");
            sponsorSkip_selfpromo = MelonPreferences.CreateEntry(catagory, nameof(sponsorSkip_selfpromo), false, "Skips Segment selfpromo");
            sponsorSkip_interaction = MelonPreferences.CreateEntry(catagory, nameof(sponsorSkip_interaction), false, "Skips Segment interaction");
            sponsorSkip_intro = MelonPreferences.CreateEntry(catagory, nameof(sponsorSkip_intro), false, "Skips Segment intro");
            videoHistory_En = MelonPreferences.CreateEntry(catagory, nameof(videoHistory_En), false, "Record history of played URLs for all Video Players. Checks every time a video is Played");

            if (!RegisteredMelons.Any(x => x.Info.Name.Equals("BTKUILib") && x.Info.SemanticVersion != null && x.Info.SemanticVersion.CompareTo(new SemVersion(1)) >= 0))
            {
                MelonLogger.Error("BTKUILib was not detected or it is outdated! VideoRemote cannot function without it!");
                MelonLogger.Error("Please download an updated copy for BTKUILib!");
                return;
            }
            _initalized = false;
            VideoPlayerSelected = null;

            if (!Directory.Exists(FolderRoot))
            {
                Directory.CreateDirectory(FolderRoot);
            }
            LoadURLs();
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (SceneManager.GetActiveScene().name == "Init")
            {
                if (!_initalized)
                {
                    _initalized = true;
                    SetupIcons();
                    //HarmonyInstance.Patch(typeof(CVR_MenuManager).GetMethod(nameof(CVR_MenuManager.ToggleQuickMenu)), null, new HarmonyMethod(typeof(VideoRemoteMod).GetMethod(nameof(QMtoggle), BindingFlags.NonPublic | BindingFlags.Static)));
                }
            }
            VideoPlayerSelected = null;
        }

        private void SetupIcons()
        {
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "VideoPlayerModLogo", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.VideoPlayerModLogo.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "VideoPlayerModPlay2", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.Play.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "VideoPlayerModPause2", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.Pause.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "VideoPlayerModNewScreen", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.NewScreen.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "VideoPlayerModWhite-Minus", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.White-Minus.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "VideoPlayerModWhite-Plus", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.White-Plus.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "VideoPlayerModSound", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.Sound.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "VideoPlayerModKeys", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.Keys.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "VideoPlayerModVideoPlayer", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.VideoPlayer.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "VideoPlayerModPastePlay", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.PastePlay.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "VideoPlayerModSave", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.Save.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "VideoPlayerModSponsorSkip", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.SponsorSkip.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "VideoPlayerModArrow-Left", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.Arrow-Left.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "VideoPlayerModArrow-Right", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.Arrow-Right.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "VideoPlayerModAdvSettings2", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.AdvSettings.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "VideoPlayerModReloadVideo", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.ReloadVideo.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "VideoPlayerModLoadURLs", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.LoadURLs.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "VideoPlayerModUp", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.Up.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "VideoPlayerModTimestamp", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.Timestamp.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "VideoPlayerModClock-Hours", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.Clock-Hours.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "VideoPlayerModClock-Minutes", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.Clock-Minutes.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "VideoPlayerModClock-Seconds", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.Clock-Seconds.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "VideoPlayerModPlayBlack", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.PlayBlack.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "VideoPlayerModReset", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.Reset.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "VideoPlayerModDebug", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.Debug.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "VideoPlayerModRes", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.Res.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "VideoPlayerModEventLog", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.EventLog.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "VideoPlayerModRemoveLink", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.RemoveLink.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "VideoPlayerModURLHistory", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.URLHistory.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "VideoPlayerMod-jog-5", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.Minus5.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "VideoPlayerMod-jog-1", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.Minus1.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "VideoPlayerMod-jog1", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.Plus1.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "VideoPlayerMod-jog5", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.Plus5.png"));

            SetupUI();
            QuickMenuAPI.OnOpenedPage += OnPageOpen;
            QuickMenuAPI.OnBackAction += OnPageBack;

            static void SetupUI()
            {
                var CustomPage = new Page("VideoRemoteMod", "VideoRemotePage", true, "VideoPlayerModLogo")
                {
                    //This sets the title that appears at the very top in the header bar
                    MenuTitle = ModBuildInfo.Name,
                    MenuSubtitle = ModBuildInfo.Description
                };
                MainPageString = CustomPage.ElementID;

                videoName = CustomPage.AddCategory("No Video Player Selected", true, false);
                var category = videoName;
                var Folder = category.AddPage("Select Video Player", "VideoPlayerModLogo", "List Video Players in the World", "VideoRemoteMod");
                VideoFolderString = Folder.ElementID;
                VideoPlayerListMain = Folder.AddCategory("", false, false);
                VideoPlayerList = Folder.AddCategory("Video Players In World", true, false);

                var button = category.AddButton("Play Video", "VideoPlayerModPlay2", "Play the Video");
                button.OnPress += () =>
                {

                    if (VideoPlayerSelected != null)
                    {
                        VideoPlayerSelected.Play();
                        MelonCoroutines.Start(Instance.SetCurrentVideoNameDelay());
                    }
                    else
                    {
                        QuickMenuAPI.ShowAlertToast("Video Player Not Selected or does not exist.", 2);
                        MelonLogger.Msg("Video Player Not Selected or does not exist.");
                    }

                };
                var button2 = category.AddButton("Pause Video", "VideoPlayerModPause2", "Pause the Video");
                button2.OnPress += () =>
                {
                    if (VideoPlayerSelected != null)
                    {
                        VideoPlayerSelected.Pause();
                        MelonCoroutines.Start(Instance.SetCurrentVideoNameDelay());
                    }
                    else
                    {
                        QuickMenuAPI.ShowAlertToast("Video Player Not Selected or does not exist.", 2);
                        MelonLogger.Msg("Video Player Not Selected or does not exist.");
                    }

                };
                var button3 = category.AddButton("Paste and Play Video", "VideoPlayerModPastePlay", "Paste and Play the Video");
                button3.OnPress += () =>
                {
                    if (VideoPlayerSelected != null)
                    {
                        QuickMenuAPI.ShowConfirm("Confirm", "Paste and Play Video?", () =>
                        {
                            VideoPlayerSelected.PasteAndPlay();
                            MelonCoroutines.Start(Instance.SetCurrentVideoNameDelay());
                        }, () => { }, "Yes", "No");
                    }
                    else
                    {
                        QuickMenuAPI.ShowAlertToast("Video Player Not Selected or does not exist.", 2);
                        MelonLogger.Msg("Video Player Not Selected or does not exist.");
                    }

                };

                volumeSilder = CustomPage.AddSlider("Volume", "Video Player Volume", .5f, 0f, 1f);
                volumeSilder.OnValueUpdated += action =>
                {
                    if (VideoPlayerSelected != null)
                    {
                        VideoPlayerSelected.SetLocalAudioVolume(volumeSilder.SliderValue);
                    }
                };

                AdvancedOptions = CustomPage.AddCategory("", false, false);
                PopulateAdvancedButtons(true);

                var catLocalVid = CustomPage.AddCategory("Local Video Player Screen", true, false);
                var buttSpawnScreen = catLocalVid.AddButton("Spawn/Toggle Local Screen", "VideoPlayerModNewScreen", "Creates a local copy of the video player screen in front of you.<p>You must select a video player first.").OnPress += () =>
                {
                    if (VideoPlayerSelected != null || (!localScreen?.Equals(null) ?? false))
                    {
                        ToggleLocalScreen();
                    }
                    else
                    {
                        QuickMenuAPI.ShowAlertToast("Can not create local screen. Video Player Not Selected or does not exist.", 3);
                        MelonLogger.Msg("Can not create local screen. Video Player Not Selected or does not exist.");
                    }
                };
                catLocalVid.AddButton("Smaller", "VideoPlayerModWhite-Minus", "Decreases the screen size").OnPress += () =>
                {
                    if (sizeScale > .15) sizeScale -= .15f;
                    UpdateLocalScreen();
                };
                catLocalVid.AddButton("Larger", "VideoPlayerModWhite-Plus", "Increases the screen size").OnPress += () =>
                {
                    sizeScale += .15f;
                    UpdateLocalScreen();
                };
                catLocalVid.AddToggle("Pickupable", "Toggles pickup of the local screen", pickupable).OnValueUpdated += action =>
                {
                    pickupable = action;
                    UpdateLocalScreen();
                };
            }
        }


        private static void SaveUrl(ViewManagerVideoPlayer vp)
        {
            var dateFormatOut = "yyyy'-'MM'-'dd";
            if (!String.IsNullOrWhiteSpace(vp.videoUrl.text))
            {
                string vidname = Utils.VideoNameFormat(vp);
                if (!File.Exists(FolderRoot + savedURLsFileName))
                {
                    using StreamWriter sw = File.CreateText(FolderRoot + savedURLsFileName);
                    sw.WriteLine(DateTime.Now.ToString(dateFormatOut) + " ␟ " + vidname + " ␟ " + vp.videoUrl.text);
                }
                else
                {
                    if (File.Exists(FolderRoot + savedURLsFileName))
                    {
                        using StreamWriter sw = File.AppendText(FolderRoot + savedURLsFileName);
                        sw.WriteLine(DateTime.Now.ToString(dateFormatOut) + " ␟ " + vidname + " ␟ " + vp.videoUrl.text);
                    }
                }
                if(!savedURLs.ContainsKey(vp.videoUrl.text)) savedURLs.Add(vp.videoUrl.text, (vidname, DateTime.Now));
                else
                {
                    savedURLs.Remove(vp.videoUrl.text);
                    savedURLs.Add(vp.videoUrl.text, (vidname, DateTime.Now));
                }
            }
            else
            {
                MelonLogger.Msg("There was nothing to save.");
            }
        }

        private static void RemoveURL(string videoUrl)
        {
            var dateFormatOut = "yyyy'-'MM'-'dd";
            if (savedURLs.ContainsKey(videoUrl))
            {
                savedURLs.Remove(videoUrl);
                try
                {
                     File.WriteAllLines(FolderRoot + savedURLsFileName, savedURLs.Select(p => string.Format("{0} ␟ {1} ␟ {2}", p.Value.Item2.ToString(dateFormatOut), p.Value.Item1, p.Key)), Encoding.UTF8);
                }
                catch (Exception ex) { MelonLogger.Error("Error writing video list after removing URL\n" + ex.ToString()); }
            }
        }

        public static void LoadURLs()
        {
            var dateFormatIn = "yyyy-MM-dd";
            if (File.Exists(FolderRoot + savedURLsFileName))
            {
                try
                {
                    savedURLs = new Dictionary<string, (string, DateTime)>(File.ReadAllLines(FolderRoot + savedURLsFileName).Select(s => s.Split(new char[] { '␟' }, StringSplitOptions.RemoveEmptyEntries))
                      .Where(x => x.Length == 3).ToLookup(p => p[2].Trim(), p => (p[1].Trim(), Utils.ParseDate(p[0].Trim(), dateFormatIn)))
                      .ToDictionary(p => p.Key, p => p.First()));
                }
                catch (Exception ex) { MelonLogger.Warning("Error reading previously saved URLs \n" + ex.ToString()); }
            }
        }

        private static void PopulateVideoList()
        {
            //MelonLogger.Msg("PopVideoList");

            if (VideoPlayerListMain.IsGenerated) VideoPlayerListMain.ClearChildren();
            if (VideoPlayerList.IsGenerated) VideoPlayerList.ClearChildren();

            VideoPlayerListMain.AddButton("Load Video Players", "VideoPlayerModLogo", "Reload the list of Video Players in the world").OnPress += () =>
            {
                PopulateVideoList();
            };

            if (GameObject.FindObjectOfType<CVRVideoPlayer>())
            {
                foreach (CVRVideoPlayer CVRvp in GameObject.FindObjectsOfType<CVRVideoPlayer>())
                {
                    List<IVideoPlayerUi> savedvpui = Traverse.Create(CVRvp).Field("VideoPlayerUis").GetValue<List<IVideoPlayerUi>>();
                    foreach (ViewManagerVideoPlayer vp in savedvpui.Cast<ViewManagerVideoPlayer>())
                    {
                        var dist = Math.Abs(Vector3.Distance(CVRvp.gameObject.transform.position, Camera.main.transform.position)).ToString("F2").TrimEnd('0');
                        var button = VideoPlayerList.AddButton($"Video Player\n{Utils.GetPlayerType(CVRvp.gameObject.transform)}", "VideoPlayerModVideoPlayer", $"Video:{Utils.VideoNameFormat(vp)}, Distance:{dist}<p>{Utils.GetPath(CVRvp.gameObject.transform)}");// | {CVRvp.playerId}");
                        button.OnPress += () =>
                        {
                            VideoPlayerSelected = vp;
                            MelonLogger.Msg(CVRvp.playerId + " has been selected");
                        };
                    }
                }
            }
        }

        private static void PopulateAdvancedButtons(bool init)
        { //Doing this way so the button states always show the current value from the video player. And a lot of hacky stuff to avoid using Bono's 'hack' for non-root pages.

            //MelonLogger.Msg("PopAdvButtons");

            if (AdvancedOptions.IsGenerated) AdvancedOptions.ClearChildren(); 

            {
                AdvOptionsPage = AdvancedOptions.AddPage("VideoPlayer Options", "VideoPlayerModAdvSettings2", "Set Permissions, Network Sync, Audio Mode, Reload current video, Timestamp Controls, Info and Debug", "VideoRemoteMod");
                AdvOptionsString = AdvOptionsPage.ElementID;
                CreatePageAdvOptionsPage(init);
            }
            //
            {
                SponsorSkipEvents = AdvancedOptions.AddPage("SponsorBlock", "VideoPlayerModSponsorSkip", "SponsorBlock can automatically skip sponsor segments in Youtube videos.", "VideoRemoteMod");
                SponsorSkipEventsString = SponsorSkipEvents.ElementID;
                CreatePageSponsorSkip();  
            }
            //
            AdvancedOptions.AddButton("Save URL", "VideoPlayerModSave", $"Stores the current video URL into the ChilloutVR/{FolderRoot} Folder.").OnPress += () =>
            {
                if (Utils.IsVideoPlayerValid(VideoPlayerSelected))
                {
                    SaveUrl(VideoPlayerSelected);
                    QuickMenuAPI.ShowAlertToast($"Saved URL! Located in ChilloutVR/{FolderRoot}{savedURLsFileName}", 3);
                    MelonLogger.Msg($"Saved URL! Located in ChilloutVR/{FolderRoot}{savedURLsFileName}");
                }
                else
                {
                    QuickMenuAPI.ShowAlertToast("Video Player Not Selected or does not exist.", 2);
                    MelonLogger.Msg("Video Player Not Selected or does not exist.");
                }
            };
            //
            {
                savedURLsPage = AdvancedOptions.AddPage("Load Saved URLs", "VideoPlayerModLoadURLs", "Load previously saved URLS", "VideoRemoteMod");
                savedURLsPageString = savedURLsPage.ElementID;
                CreatePagesavedURLsPage();           
            }
        }

        public static void CreatePageAdvOptionsPage(bool init)
        {
            //MelonLogger.Msg($"AdvOpt {init}");

            if (AdvOptionsPage != null && AdvOptionsPage.IsGenerated) AdvOptionsPage.ClearChildren();

            var advSubPageCat = AdvOptionsPage.AddCategory("", false, false);
            {
                var current = Utils.IsVideoPlayerValid(VideoPlayerSelected) ? VideoPlayerSelected.videoPlayer.ControlPermission.ToString() : "None Selected";
                var butt = advSubPageCat.AddButton($"Permission: {current}", "VideoPlayerModKeys", $"Toggles the video player permission between<p>Everyone and InstanceModerators");
                butt.OnPress += () =>
                {
                    if (Utils.IsVideoPlayerValid(VideoPlayerSelected))
                    {
                        VideoPlayerUtils.ControlPermission setTo = VideoPlayerUtils.ControlPermission.Everyone;
                        if (VideoPlayerSelected.videoPlayer.ControlPermission == VideoPlayerUtils.ControlPermission.Everyone)
                        {
                            QuickMenuAPI.ShowConfirm("Confirm", "Switch permission to InstanceModerators?", () =>
                            {
                                setTo = VideoPlayerUtils.ControlPermission.InstanceModerators;
                                VideoPlayerSelected.videoPlayer.SetControlPermission(setTo);
                                butt.ButtonText = $"Permission: {setTo}";
                            }, () => { }, "Yes", "No");

                        }
                        else
                        {
                            setTo = VideoPlayerUtils.ControlPermission.Everyone;
                            VideoPlayerSelected.videoPlayer.SetControlPermission(setTo);
                            butt.ButtonText = $"Permission: {setTo}";
                        }


                    }
                    else
                    {
                        QuickMenuAPI.ShowAlertToast("Video Player Not Selected or does not exist.", 2);
                        MelonLogger.Msg("Video Player Not Selected or does not exist.");
                    }
                };
            }
            {
                var butt = advSubPageCat.AddToggle("Network Sync", "Toggles Network Sync to state", true);
                butt.OnValueUpdated += action =>
                {
                    if (Utils.IsVideoPlayerValid(VideoPlayerSelected))
                    {
                        VideoPlayerSelected.videoPlayer.SetNetworkSync(action);
                    }
                    else
                    {
                        QuickMenuAPI.ShowAlertToast("Video Player Not Selected or does not exist.", 3);
                        MelonLogger.Msg("Video Player Not Selected or does not exist.");
                    }
                };
            }
            {
                var current = (VideoPlayerSelected != null) ? VideoPlayerSelected.videoPlayer.audioPlaybackMode.ToString() : "None Selected";
                var butt = advSubPageCat.AddButton($"Audio Mode: {current}", "VideoPlayerModSound", $"Toggles audio mode between Direct, AudioSource, RoomScale<p>Currently: {current}");
                butt.OnPress += () =>
                {
                    if (Utils.IsVideoPlayerValid(VideoPlayerSelected))
                    {
                        try
                        {
                            switch (VideoPlayerSelected.videoPlayer.audioPlaybackMode)
                            {
                                case VideoPlayerUtils.AudioMode.Direct: VideoPlayerSelected.videoPlayer.SetAudioMode(VideoPlayerUtils.AudioMode.AudioSource); break;
                                case VideoPlayerUtils.AudioMode.AudioSource: VideoPlayerSelected.videoPlayer.SetAudioMode(VideoPlayerUtils.AudioMode.RoomScale); break;
                                case VideoPlayerUtils.AudioMode.RoomScale: VideoPlayerSelected.videoPlayer.SetAudioMode(VideoPlayerUtils.AudioMode.Direct); break;
                                default: VideoPlayerSelected.videoPlayer.SetAudioMode(VideoPlayerUtils.AudioMode.AudioSource); break;
                            }
                            butt.ButtonText = $"Audio Mode: {VideoPlayerSelected.videoPlayer.audioPlaybackMode}";
                            butt.ButtonTooltip = $"Toggles audio mode between Direct, AudioSource, RoomScale<p>Currently: {VideoPlayerSelected.videoPlayer.audioPlaybackMode}";
                        }
                        catch (System.Exception ex)
                        {
                            MelonLogger.Error($"Error when changing video audio source\n" + ex.ToString());
                            butt.ButtonText = $"Audio Mode: Error";
                            QuickMenuAPI.ShowAlertToast("Error when changing video audio source.", 3);
                        }
                    }
                    else
                    {
                        QuickMenuAPI.ShowAlertToast("Video Player Not Selected or does not exist.", 2);
                        MelonLogger.Msg("Video Player Not Selected or does not exist.");
                    }
                };
            }
            //
            {
                var current = (VideoPlayerSelected != null) ? ((int)VideoPlayerSelected.videoPlayer.maxResolution).ToString() : "None Selected";
                var butt = advSubPageCat.AddButton("Set Video Resolution", "VideoPlayerModRes", $"Set Video Max Resolution<p>Currently: {current}").OnPress += () =>
                {
                    if (Utils.IsVideoPlayerValid(VideoPlayerSelected))
                    {
                        string[] resolutionStrings = Enum.GetValues(typeof(VideoPlayerUtils.Resolution))
                                    .Cast<int>()
                                    .Select(value => value.ToString())
                                    .ToArray();


                        var selection = new BTKUILib.UIObjects.Objects.MultiSelection($"Video Resolution", resolutionStrings,
                             (Array.IndexOf(resolutionStrings, ((int)VideoPlayerSelected.videoPlayer.maxResolution).ToString())));
                        BTKUILib.QuickMenuAPI.OpenMultiSelect(selection);
                        selection.OnOptionUpdated += resInt => {
                            VideoPlayerSelected.videoPlayer.SetMaxResolution(Int32.Parse(resolutionStrings[resInt]));
                            //CreatePageAdvOptionsPage(false);
                        };
                    }
                    else
                    {
                        QuickMenuAPI.ShowAlertToast("Video Player Not Selected or does not exist.", 2);
                        MelonLogger.Msg("Video Player Not Selected or does not exist.");
                    }
                };
            }
            //
            {
                var curState = (VideoPlayerSelected?.videoPlayer?.VideoPlayer != null) ? VideoPlayerSelected.videoPlayer.ytAudioOnly : false;
                advSubPageCat.AddToggle("Audio Only", "Toggle Audio Only mode for Youtube<p>(Requires video reload)", curState).OnValueUpdated += action =>
                {
                    if (Utils.IsVideoPlayerValid(VideoPlayerSelected))
                    {
                        VideoPlayerSelected.videoPlayer.ytAudioOnly = action;
                    }
                    else
                    {
                        QuickMenuAPI.ShowAlertToast("Video Player Not Selected or does not exist.", 2);
                        MelonLogger.Msg("Video Player Not Selected or does not exist.");
                    }
                };
            }
            //
            {
                var butt = advSubPageCat.AddButton($"Reload video at timestamp", "VideoPlayerModReloadVideo", $"Reloads the current video and sets it back to the current timestamp");
                butt.OnPress += () =>
                {
                    if (Utils.IsVideoPlayerValid(VideoPlayerSelected))
                    {
                        if (TryingToReplay)
                            QuickMenuAPI.ShowAlertToast("Video reload already running", 2);
                        else
                            MelonCoroutines.Start(Instance.ReloadVideo());
                    }
                    else
                    {
                        QuickMenuAPI.ShowAlertToast("Video Player Not Selected or does not exist.", 2);
                        MelonLogger.Msg("Video Player Not Selected or does not exist.");
                    }
                };
            }
            //
            {
                TimeStampPage = advSubPageCat.AddPage("Set to TimeStamp", "VideoPlayerModTimestamp", "Set videoplayer to timestamp", "VideoRemoteMod");
                TimeStampPageString = TimeStampPage.ElementID;

                if (init) CreatePageTimeStampPage();
            }
            //
            var advSubPageCat_2 = AdvOptionsPage.AddCategory("", false, false);
            //    
            {
                LogPage = advSubPageCat_2.AddPage("Event Logs", "VideoPlayerModEventLog", "Video Player Event Logs", "VideoRemoteMod");
                LogPageString = LogPage.ElementID;

                if (init) CreatePageLogPage();
            }
            //
            {
                DebugPage = advSubPageCat_2.AddPage("DebugPage", "VideoPlayerModDebug", "DebugPage", "VideoRemoteMod");
                DebugPageString = DebugPage.ElementID;
                if (init) CreatePageDebug();
            }
            //
            {
                var butt = advSubPageCat_2.AddButton($"Load Blank Video", "VideoPlayerModPlayBlack", $"Load a blank video, just 480p black with no audio").OnPress += () =>
                {
                    if (Utils.IsVideoPlayerValid(VideoPlayerSelected))
                    {
                        QuickMenuAPI.ShowConfirm("Confirm", $"Play Video?<p><p><p>Blank Video<p><p><p>1min long, Black, 480p", () =>
                        {
                            VideoPlayerSelected.videoPlayer.SetVideoUrl("https://www.youtube.com/watch?v=k9NfB9-CR1k");
                            MelonCoroutines.Start(Instance.SetCurrentVideoNameDelay());
                        }, () => { }, "Yes", "No");
                    }
                    else
                    {
                        QuickMenuAPI.ShowAlertToast("Video Player Not Selected or does not exist.", 2);
                        MelonLogger.Msg("Video Player Not Selected or does not exist.");
                    }
                };
            }
            //
            var advSubPageCat_3 = AdvOptionsPage.AddCategory("", false, false);
            //    
            {
                URLHistoryPage = advSubPageCat_3.AddPage("URL History Page", "VideoPlayerModURLHistory", "Videos played on VideoPlayers since game start", "VideoRemoteMod");
                URLHistoryPageString = URLHistoryPage.ElementID;
                if (init) CreatePageURLsHistroyPage();
            }
            {
                var butt = advSubPageCat_3.AddToggle("URL History", "Record URL History for ALL VideoPlayers in the world - Checks every time a video is Played<p>With this off history is only saved when you have a VideoPlayer selected and interact with this menu.", videoHistory_En.Value);
                butt.OnValueUpdated += action =>
                {
                    videoHistory_En.Value = action;
                };
            }
        }

        public static void CreatePageTimeStampPage()
        {
            //MelonLogger.Msg("Timestamp");

            if (TimeStampPage != null && TimeStampPage.IsGenerated) TimeStampPage.ClearChildren();

            var timestampSum = timeStampHour * 60 * 60 + timeStampMin * 60 + timeStampSec;
            if (!Utils.IsVideoPlayerValid(VideoPlayerSelected))
            {
                TimeStampPage.AddCategory($"No Player Selected", true, false);
            }
            else
            {
                if (VideoPlayerSelected.videoPlayer.VideoPlayer.Info.VideoMetaData.IsLivestream)
                {
                    TimeStampPage.AddCategory($"Video is a livestream: No time controls allowed", true, false);
                }
                else if (!(VideoPlayerSelected.videoPlayer.VideoPlayer.Info.VideoMetaData.GetDuration() > 0))
                {
                    TimeStampPage.AddCategory($"No video playing", true, false);
                }
                else
                {
                    var currentString = "";

                    currentString += "Playing: " + Utils.VideoNameFormat(VideoPlayerSelected) + "<p>";
                    currentString += "Currently at: " + Utils.FormatTime((float)VideoPlayerSelected.videoPlayer.VideoPlayer.Time) + "<p>";
                    currentString += "End time: " + Utils.FormatTime((float)VideoPlayerSelected.videoPlayer.VideoPlayer.Info.VideoMetaData.GetDuration()) + "<p>";
                    var textCat = TimeStampPage.AddCategory("temp", true, false);
                    textCat.CategoryName = currentString;

                    var timeHeader = TimeStampPage.AddCategory($"Timestamp: {Utils.FormatTime(timestampSum)}", true, false);
                    timeHeader.AddButton($"Set to timestamp", "VideoPlayerModTimestamp", $"Set video to {Utils.FormatTime(timestampSum)}").OnPress += () =>
                    {
                        if (VideoPlayerSelected?.videoPlayer?.VideoPlayer != null && timestampSum < VideoPlayerSelected.videoPlayer.VideoPlayer.Info.VideoMetaData.GetDuration() - 5)
                        {//Confirm this video is still on
                            MelonLogger.Msg($"Manual set timestamp to: {Utils.FormatTime(timestampSum)}, Was at: {Utils.FormatTime((float)VideoPlayerSelected.videoPlayer.VideoPlayer.Time)}");
                            QuickMenuAPI.ShowAlertToast($"Manual set timestamp to: {Utils.FormatTime(timestampSum)}", 2);
                            VideoPlayerSelected.videoPlayer.SetVideoTimestamp(timestampSum);
                            CreatePageTimeStampPage();
                        }
                        else
                        {
                            QuickMenuAPI.ShowAlertToast("Video Player does not exist or timestamp beyond length of video", 3);
                            MelonLogger.Msg("Video Player does not exist or timestamp beyond length of video");
                        }
                    };
                    timeHeader.AddButton($"Set Hour", "VideoPlayerModClock-Hours", "Set the hour value").OnPress += () =>
                    {
                        QuickMenuAPI.OpenNumberInput("Hours", timeStampHour, (action) =>
                        {
                            timeStampHour = (int)action;
                            CreatePageTimeStampPage();
                        });
                    };
                    timeHeader.AddButton($"Set Minutes", "VideoPlayerModClock-Minutes", "Set the minute value").OnPress += () =>
                    {
                        QuickMenuAPI.OpenNumberInput("Minutes", timeStampMin, (action) =>
                        {
                            timeStampMin = (int)action;
                            CreatePageTimeStampPage();
                        });
                    };
                    timeHeader.AddButton($"Set Seconds", "VideoPlayerModClock-Seconds", "Set the second value").OnPress += () =>
                    {
                        QuickMenuAPI.OpenNumberInput("Seconds", timeStampSec, (action) =>
                        {
                            timeStampSec = (int)action;
                            CreatePageTimeStampPage();
                        });
                    };

                    var jogHeader = TimeStampPage.AddCategory($"Jog Controllers", true, false);
                    {
                        var jogList = new Dictionary<string, int> {
                            {"VideoPlayerMod-jog-5", -5 },
                            {"VideoPlayerMod-jog-1", -1 },
                            {"VideoPlayerMod-jog1", 1 },
                            {"VideoPlayerMod-jog5", 5 }
                        };

                        foreach (var jog in jogList)
                        {
                            float seconds = 60f * jog.Value;
                            jogHeader.AddButton($"{jog.Value} Min", jog.Key, $"Set video {jog.Value} minute(s)").OnPress += () =>
                            {
                                var timestampTo = (float)VideoPlayerSelected.videoPlayer.VideoPlayer.Time + seconds;
                                if (VideoPlayerSelected?.videoPlayer?.VideoPlayer != null && timestampTo < VideoPlayerSelected.videoPlayer.VideoPlayer.Info.VideoMetaData.GetDuration() - 5 &&
                                timestampTo > 0f)
                                {//Confirm this video is still on
                                    MelonLogger.Msg($"Jogged timestamp to: {Utils.FormatTime(timestampTo)}, Was at: {Utils.FormatTime((float)VideoPlayerSelected.videoPlayer.VideoPlayer.Time)}");
                                    QuickMenuAPI.ShowAlertToast($"Jogged timestamp to: {Utils.FormatTime(timestampTo)}", 2);
                                    VideoPlayerSelected.videoPlayer.SetVideoTimestamp(timestampTo);
                                    CreatePageTimeStampPage();
                                }
                                else
                                {
                                    QuickMenuAPI.ShowAlertToast("Video Player does not exist or timestamp beyond length of video", 3);
                                    MelonLogger.Msg("Video Player does not exist or timestamp beyond length of video");
                                }
                            };
                        }
                    }

                    var timeSegHeader = TimeStampPage.AddCategory($"Generated Timestamps", true, false);
                    if (VideoPlayerSelected.videoPlayer.VideoPlayer.Info.VideoMetaData.GetDuration() > 0)
                    {
                        var timeInterval = (float)VideoPlayerSelected.videoPlayer.VideoPlayer.Info.VideoMetaData.GetDuration() / 10;
                        for (int i = 0; i < 10; i += 2)
                        {
                            var timeSeg = timeInterval * i;
                            var timeSeg2 = timeInterval * (i + 1);
                            var timeStampText = $"&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; {Utils.FormatTime(timeSeg)} &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" +
                                $" {Utils.FormatTime(timeSeg2)} ";
                            var timeSegLoop = TimeStampPage.AddCategory("temp", true, false); //__________________
                            timeSegLoop.CategoryName = timeStampText;

                            for (int x = 0; x < 2; x++)
                            {
                                var timeSegCopy = x == 0 ? timeSeg : timeSeg2;
                                timeSegLoop.AddButton($"Set videoplayer to time", "VideoPlayerModTimeStamp", $"Set video to {Utils.FormatTime(timeSegCopy)}").OnPress += () =>
                                {
                                    if (VideoPlayerSelected?.videoPlayer?.VideoPlayer != null && timeSegCopy < VideoPlayerSelected.videoPlayer.VideoPlayer.Info.VideoMetaData.GetDuration() - 5)
                                    {//Confirm this video is still on
                                        MelonLogger.Msg($"Manual set segment timestamp to: {Utils.FormatTime(timeSeg)}, Was at: {Utils.FormatTime((float)VideoPlayerSelected.videoPlayer.VideoPlayer.Time)}");
                                        QuickMenuAPI.ShowAlertToast($"Manual set segment timestamp to: {Utils.FormatTime(timeSeg)}", 2);
                                        VideoPlayerSelected.videoPlayer.SetVideoTimestamp(timeSegCopy);
                                        CreatePageTimeStampPage();
                                    }
                                    else
                                    {
                                        QuickMenuAPI.ShowAlertToast("Video Player does not exist or segment timestamp beyond length of video", 3);
                                        MelonLogger.Msg("Video Player does not exist or segment timestamp beyond length of video");
                                        CreatePageTimeStampPage();
                                    }
                                };
                                timeSegLoop.AddButton($"Send time to custom input", "VideoPlayerModUp", $"Send this time value to custom input above").OnPress += () =>
                                {
                                    timeStampHour = Utils.GetTimeSeg(timeSegCopy, "hour");
                                    timeStampMin = Utils.GetTimeSeg(timeSegCopy, "min");
                                    timeStampSec = Utils.GetTimeSeg(timeSegCopy, "sec");
                                    CreatePageTimeStampPage();
                                };
                            }
                        }

                    }
                }


            }
        }

        public static void CreatePageLogPage()
        {
            //MelonLogger.Msg("Log");

            if (LogPage != null && LogPage.IsGenerated) LogPage.ClearChildren();

            if (!Utils.IsVideoPlayerValid(VideoPlayerSelected))
            {
                LogPage.AddCategory($"No Player Selected", true, false);
            }
            else
            {
                var cat = LogPage.AddCategory($"", false, false);
                cat.AddButton($"Refresh", "VideoPlayerModReset", $"Refresh page").OnPress += () =>
                {
                    CreatePageLogPage();
                };
                LogPage.AddCategory($"Video player log entries:", true, false);
                if (VideoPlayerSelected.logEntries.Count == 0)
                    LogPage.AddCategory("None", true, false);
                else
                {
                    foreach (var entry in VideoPlayerSelected.logEntries)
                    {
                        var entryText = entry.Replace($"Unknown ({MetaPort.Instance.ownerId})", AuthManager.username);

                        Regex regex = new Regex(@"(\d+\.\d+)");
                        Match match = regex.Match(entryText);
                        if (match.Success)
                        {
                            string floatString = match.Groups[1].Value;
                            if (float.TryParse(floatString, out float floatValue))
                            {
                                entryText = entryText.Replace(floatString +" seconds", $"{floatString} seconds ({Utils.FormatTime(floatValue)})");
                            }
                        }

                        LogPage.AddCategory(entryText, true, false);
                    }
                }
            }
        }

        public static void CreatePageDebug()
        {
            //MelonLogger.Msg("Debug");

            if (DebugPage != null && DebugPage.IsGenerated) DebugPage.ClearChildren();

            if (!Utils.IsVideoPlayerValid(VideoPlayerSelected))
            {
                DebugPage.AddCategory($"No Player Selected", true, false);
            }
            else
            {
                var cat = DebugPage.AddCategory($"Debug Info:", true, false);
                cat.AddButton($"Refresh", "VideoPlayerModReset", $"Refresh page").OnPress += () =>
                {
                    CreatePageDebug();
                };
                string str1 = "";
                string str2 = "";
                if (VideoPlayerSelected.videoPlayer.currentlySelectedVideo != null)
                    str1 = VideoPlayerSelected.videoPlayer.currentlySelectedVideo.videoTitle;
                if (VideoPlayerSelected.videoPlayer.currentlySelectedPlaylist != null)
                    str2 = VideoPlayerSelected.videoPlayer.currentlySelectedPlaylist.playlistTitle;
                var videoPlayer = VideoPlayerSelected.videoPlayer.VideoPlayer;

                var debugString = "";

                debugString += $"Video title: {str1.Truncate(25)}<p>";
                debugString += $"Playlist title: {str2.Truncate(25)}<p>";
                debugString += $"URL: {videoPlayer.Info.VideoMetaData.GetUrl()}<p>";
                debugString += $"Video FPS: {videoPlayer.Info.VideoMetaData.GetVideoFps()} Player FPS: {videoPlayer.Info.GetPlayerFps()}<p>";
                debugString += $"Connection: {(videoPlayer.Info.IsConnectionLost() ? "Lost" : "Connected")}<p>";
                debugString += $"Video Player Time: {videoPlayer.Info.Time}<p>";
                debugString += $"Audio Player Time: {videoPlayer.Info.AudioTime}<p>";
                debugString += $"Video Texture Res: {videoPlayer.Info.VideoMetaData.GetVideoWidth()} x {videoPlayer.Info.VideoMetaData.GetVideoHeight()}<p>";
                debugString += $"Render Texture Res: {VideoPlayerSelected.videoPlayer.ProjectionTexture.width} x {VideoPlayerSelected.videoPlayer.ProjectionTexture.height}<p>";
                debugString += $"Video Codec: {GetVideoCodec()}<p>";
                debugString += $"Audio Mode: {VideoPlayerSelected.videoPlayer.audioPlaybackMode}<p>";
                debugString += $"Audio Channels: {videoPlayer.Info.VideoMetaData.GetAudioChannels()}";

                var textcat = DebugPage.AddCategory("temp", true, false);
                textcat.CategoryName = debugString;

                string GetVideoCodec()
                {
                    YoutubeDl.ProcessResult? processResult = VideoPlayerSelected.videoPlayer.VideoPlayer.Info.VideoMetaData.ProcessResult;
                    if (!processResult.HasValue)
                        return "Unknown";
                    YoutubeDlVideoMetaData output = processResult.Value.Output;
                    if (output == null)
                        return "Unknown";
                    if (!string.IsNullOrEmpty(output.VideoCodec))
                        return output.VideoCodec;
                    // This can get stuck returning nothing, which due to the way this mod is made, breaks the menu generation 
                    //   foreach (YoutubeDlVideoFormat requestedFormat in output.requestedFormats)
                    //   {
                    //       MelonLogger.Msg("60");
                    //       if (requestedFormat.Format != null && !requestedFormat.Format.Contains("audio only") && requestedFormat.VideoCodec != null)
                    //           return requestedFormat.VideoCodec;
                    //       MelonLogger.Msg("70");
                    //   }
                    return "Unknown";
                }
            }
        }

        public static void CreatePageSponsorSkip()
        {
            //MelonLogger.Msg("SponsorSkip");

            if (SponsorSkipEvents != null && SponsorSkipEvents.IsGenerated) SponsorSkipEvents.ClearChildren();

            var skipHeaderCat = SponsorSkipEvents.AddCategory($"", false, false);
            skipHeaderCat.AddToggle("Enable", "Enable SponsorBlock for this VideoPlayer", sponsorSkip).OnValueUpdated += action =>
            {
                sponsorSkip = action;
                StartSponsorSkip();
            };

            var skipSettings = SponsorSkipEvents.AddCategory($"Segments types to skip", true, false);
            skipSettings.AddToggle("Sponsor", "Skip sponsor segments.<p>Doesn't skip if <5 seconds.", sponsorSkip_sponsor.Value).OnValueUpdated += action =>
            {
                sponsorSkip_sponsor.Value = action;

                if (Utils.IsVideoPlayerValid(VideoPlayerSelected) && VideoPlayerSelected.videoPlayer.lastNetworkVideoUrl == sponsorskipVideo)
                    PrepSkipList();
            };
            skipSettings.AddToggle("Self Promo", "Skip self promo segments.<p>Doesn't skip if <5 seconds.", sponsorSkip_selfpromo.Value).OnValueUpdated += action =>
            {
                sponsorSkip_selfpromo.Value = action;
                if (Utils.IsVideoPlayerValid(VideoPlayerSelected) && VideoPlayerSelected.videoPlayer.lastNetworkVideoUrl == sponsorskipVideo)
                    PrepSkipList();
            };
            skipSettings.AddToggle("Interaction", "Skip interaction segments.<p>Doesn't skip if <5 seconds.", sponsorSkip_interaction.Value).OnValueUpdated += action =>
            {
                sponsorSkip_interaction.Value = action;
                if (Utils.IsVideoPlayerValid(VideoPlayerSelected) && VideoPlayerSelected.videoPlayer.lastNetworkVideoUrl == sponsorskipVideo)
                    PrepSkipList();
            };
            skipSettings.AddToggle("Intro", "Skip intro segments.<p>Doesn't skip if <5 seconds.", sponsorSkip_intro.Value).OnValueUpdated += action =>
            {
                sponsorSkip_intro.Value = action;
                if (Utils.IsVideoPlayerValid(VideoPlayerSelected) && VideoPlayerSelected.videoPlayer.lastNetworkVideoUrl == sponsorskipVideo)
                    PrepSkipList();
            };

            if (!sponsorSkip)
            {
                SponsorSkipEvents.AddCategory($"SponsorBlock not enabled", true, false);
            }
            else if (!Utils.IsVideoPlayerValid(VideoPlayerSelected))
            {
                SponsorSkipEvents.AddCategory($"No video player selected", true, false);
            }
            else if (sponsorskipVideo == "API_Repsonse_Not_Found")
            {
                SponsorSkipEvents.AddCategory($"Video not found in SponsorBlock API", true, false);
            }
            else if (VideoPlayerSelected.videoPlayer.lastNetworkVideoUrl != sponsorskipVideo)
            {
                SponsorSkipEvents.AddCategory($"SponsorBlock hasn't grabbed current video yet, or no video is playing", true, false);
                SponsorSkipEvents.AddCategory($"Go back a page to refresh this menu", true, false);
            }
            else if (sponsorskipResult == null || sponsorskipResult.Length == 0)
            {
                SponsorSkipEvents.AddCategory($"SponsorBlock didn't find any results");
            }
            else
            {
                var skipHeader = SponsorSkipEvents.AddCategory($"------------ All events for video ------------", true, false);

                foreach (var x in sponsorskipResult.OrderBy(x => x.segment[0]))
                {
                    var stringText = $"Type: {Utils.SkipCatSwitch(x.category)}{((x.category == "poi_highlight") ? "   <<<<<<<<<<<<<<" : "")}<p>";
                    if (x.description != "") stringText += $"Desc: {x.description}<p>";
                     stringText += $"{Utils.FormatTime(x.segment[0])} - {Utils.FormatTime(x.segment[1])}<p>";

                    var skipEvent = SponsorSkipEvents.AddCategory($"temp", true, false);
                    skipEvent.CategoryName = stringText;

                    skipEvent.AddButton($"Jump to Start", "VideoPlayerModArrow-Left", $"Set video to start time of event {Utils.FormatTime(x.segment[0])}").OnPress += () =>
                    {
                        if (Utils.IsVideoPlayerValid(VideoPlayerSelected) && VideoPlayerSelected.videoPlayer.lastNetworkVideoUrl == sponsorskipVideo)
                        {//Confirm this video is still on
                            MelonLogger.Msg($"|SponsorBlock| Manual skip to: {Utils.FormatTime(x.segment[0])}, Was at: {Utils.FormatTime((float)VideoPlayerSelected.videoPlayer.VideoPlayer.Time)}");
                            QuickMenuAPI.ShowAlertToast($"Manual skip to: { Utils.FormatTime(x.segment[0])}", 2);
                            lastskip = x.segment;
                            VideoPlayerSelected.videoPlayer.SetVideoTimestamp(x.segment[0]);
                        }
                        else
                        {
                            QuickMenuAPI.ShowAlertToast("Video Player does not exist or wrong video playing.", 3);
                            MelonLogger.Msg("Video Player Not Selected or does not exist or wrong video playing - skipEvent");
                        }
                    };

                    skipEvent.AddButton($"Jump to End", "VideoPlayerModArrow-Right", $"Set video to end time of event {Utils.FormatTime(x.segment[1])}").OnPress += () =>
                    {
                        if (Utils.IsVideoPlayerValid(VideoPlayerSelected) && VideoPlayerSelected.videoPlayer.lastNetworkVideoUrl == sponsorskipVideo)
                        {//Confirm this video is still on
                            if (x.segment[1] > VideoPlayerSelected.videoPlayer.VideoPlayer.Info.VideoMetaData.GetDuration() - 5)
                            { //Skip if <5 seconds from end of video
                                MelonLogger.Msg($"|SponsorBlock| Not skipping to {Utils.FormatTime(x.segment[1])}, less than 5 seconds till end of video");
                                QuickMenuAPI.ShowAlertToast($"Not skipping, less than 5 seconds till end of video!", 3);
                            }
                            else
                            {
                                MelonLogger.Msg($"|SponsorBlock| Manual skip to: {Utils.FormatTime(x.segment[1])}, Was at: {Utils.FormatTime((float)VideoPlayerSelected.videoPlayer.VideoPlayer.Time)}");
                                QuickMenuAPI.ShowAlertToast($"Manual skip to: { Utils.FormatTime(x.segment[1])}", 2);
                                lastskip = x.segment;
                                VideoPlayerSelected.videoPlayer.SetVideoTimestamp(x.segment[1]);
                            }
                        }
                        else
                        {
                            QuickMenuAPI.ShowAlertToast("Video Player does not exist or wrong video playing.", 3);
                            MelonLogger.Msg("Video Player Not Selected or does not exist or wrong video playing - skipEvent");
                        }
                    };
                }
                SponsorSkipEvents.AddCategory($"", false, false);
                SponsorSkipEvents.AddCategory($"", false, false);
                SponsorSkipEvents.AddCategory($"Uses SponsorBlock data from https://sponsor.ajay.app/", true, false);
            }
        }

        public static void CreatePagesavedURLsPage()
        {
            //MelonLogger.Msg("SavedUrls");
            if (savedURLsPage != null && savedURLsPage.IsGenerated) savedURLsPage.ClearChildren();
            if (savedURLs.Count == 0)
            {
                savedURLsPage.AddCategory($"No saved URLs found", true, false);
            }
            else
            {
                foreach (var x in savedURLs.OrderBy(x => x.Value.Item2).Reverse())
                {
                    var urlCat = savedURLsPage.AddCategory("temp", true, false);
                    urlCat.CategoryName = $"{x.Value.Item1}<p>{x.Value.Item2.ToString("yyyy'-'MM'-'dd")}";
                    urlCat.AddButton($"Play Video", "VideoPlayerModPastePlay", $"Play the video: {x.Key}<p>{x.Value.Item1}").OnPress += () =>
                    {
                        if (Utils.IsVideoPlayerValid(VideoPlayerSelected))
                        {
                            QuickMenuAPI.ShowConfirm("Confirm", $"Play Video?<p><p><p>{x.Key}<p><p><p>{x.Value.Item1}", () =>
                            {
                                VideoPlayerSelected.videoPlayer.SetVideoUrl(x.Key);
                                MelonCoroutines.Start(Instance.SetCurrentVideoNameDelay());
                            }, () => { }, "Yes", "No");
                        }
                        else
                        {
                            QuickMenuAPI.ShowAlertToast("Video Player Not Selected or does not exist.", 2);
                            MelonLogger.Msg("Video Player Not Selected or does not exist.");
                        }
                    };

                    urlCat.AddButton($"Remove from List", "VideoPlayerModRemoveLink", $"Removes the video: {x.Key}<p>{x.Value.Item1}").OnPress += () =>
                    {
                        QuickMenuAPI.ShowConfirm("Confirm", $"Remove video from saved list?<p><p><p>{x.Key}<p><p><p>{x.Value.Item1}", () =>
                        {
                            RemoveURL(x.Key);
                            CreatePagesavedURLsPage();
                        }, () => { }, "Yes", "No");

                    };
                }
            }
        }

        public static void CreatePageURLsHistroyPage()
        {
            //MelonLogger.Msg("SavedUrls");
            if (URLHistoryPage != null && URLHistoryPage.IsGenerated) URLHistoryPage.ClearChildren();
            if (historyURLs.Count == 0)
            {
                URLHistoryPage.AddCategory($"No history found", true, false);
            }
            else
            { //historyURLs
                foreach (var x in historyURLs.Reverse<(string, string, DateTime)>())
                {
                    var urlCat = URLHistoryPage.AddCategory("temp", true, false);
                    urlCat.CategoryName = $"{x.Item2}<p>{x.Item3.ToString("yyyy'-'MM'-'dd HH:mm")}";
                    urlCat.AddButton($"Play Video", "VideoPlayerModPastePlay", $"Play the video: {x.Item1}<p>{x.Item2}").OnPress += () =>
                    {
                        if (Utils.IsVideoPlayerValid(VideoPlayerSelected))
                        {
                            QuickMenuAPI.ShowConfirm("Confirm", $"Play Video?<p><p><p>{x.Item1}<p><p><p>{x.Item2}", () =>
                            {
                                VideoPlayerSelected.videoPlayer.SetVideoUrl(x.Item1);
                                MelonCoroutines.Start(Instance.SetCurrentVideoNameDelay());
                            }, () => { }, "Yes", "No");
                        }
                        else
                        {
                            QuickMenuAPI.ShowAlertToast("Video Player Not Selected or does not exist.", 2);
                            MelonLogger.Msg("Video Player Not Selected or does not exist.");
                        }
                    };

                    urlCat.AddButton($"Save URL", "VideoPlayerModSave", $"Stores the current video URL into the ChilloutVR/{FolderRoot} Folder.").OnPress += () =>
                    {
                        if (!savedURLs.ContainsKey(x.Item1)) savedURLs.Add(x.Item1, (x.Item2, x.Item3));
                    };
                }
            }
        }


        private static void SetCurrentVideoName()
        {
            //"No Video Player Selected"
            if (Utils.IsVideoPlayerValid(VideoPlayerSelected))
            {
                string time = VideoPlayerSelected.videoPlayer.VideoPlayer.Info.VideoMetaData.IsLivestream ? "Livestream" :
                Utils.FormatTime((float)VideoPlayerSelected.videoPlayer.VideoPlayer.Time) + " / " + Utils.FormatTime((float)VideoPlayerSelected.videoPlayer.VideoPlayer.Info.VideoMetaData.GetDuration());

                videoName.CategoryName = Utils.VideoState(VideoPlayerSelected) + Utils.VideoNameFormat(VideoPlayerSelected) + "<p>" + time;
            }
            else
                videoName.CategoryName = "No video player selected";
        }
        System.Collections.IEnumerator SetCurrentVideoNameDelay()
        {//Lazy way to set the name after letting the video load
            int i = 0;
            while (i <= 10)
            {
                i++;
                yield return new WaitForSecondsRealtime(1);
                SetCurrentVideoName();
            }
        }

        public static void AllURLhistory()
        {
            if (Time.time < historyLastCheck + 10)
                return;
            try
            {
                //var sw = Stopwatch.StartNew();
                historyLastCheck = Time.time;
                foreach (var CVRvp in GameObject.FindObjectsOfType<CVRVideoPlayer>())
                {
                    List<IVideoPlayerUi> savedvpui = Traverse.Create(CVRvp).Field("VideoPlayerUis").GetValue<List<IVideoPlayerUi>>();
                    foreach (ViewManagerVideoPlayer vp in savedvpui.Cast<ViewManagerVideoPlayer>())
                    {
                        if (!Utils.IsVideoPlayerValid(vp))
                            continue;
                        if (!String.IsNullOrWhiteSpace(vp.videoUrl.text))
                        {
                            string vidname = Utils.VideoNameFormat(vp);
                            var keyToCheck = vp.videoUrl.text;
                            if (!historyURLs.Reverse<(string, string, DateTime)>().Take(10).Any(tuple => tuple.Item1 == keyToCheck))
                            {
                                historyURLs.Add((vp.videoUrl.text, vidname, DateTime.Now));
                                SaveHistoryUrl(vidname, vp.videoUrl.text);
                                //MelonLogger.Msg($"Adding to history: {vidname} - {vp.videoUrl.text}");
                            }
                        }
                    }
                }
                //var endtime = sw.ElapsedMilliseconds;
                //MelonLogger.Msg($"Time {endtime}");
            }
            catch (Exception ex) { MelonLogger.Error("Error in AllURLhistory \n" + ex.ToString()); }
        }

        public static void SingleURLhistory()
        {
            if (!Utils.IsVideoPlayerValid(VideoPlayerSelected))
                return;
            if (String.IsNullOrWhiteSpace(VideoPlayerSelected.videoUrl.text))
                return;
            string vidname = Utils.VideoNameFormat(VideoPlayerSelected);
            var keyToCheck = VideoPlayerSelected.videoUrl.text;

            if (!historyURLs.Reverse<(string, string, DateTime)>().Take(10).Any(tuple => tuple.Item1 == keyToCheck))
            {
                historyURLs.Add((VideoPlayerSelected.videoUrl.text, vidname, DateTime.Now));
                SaveHistoryUrl(vidname, VideoPlayerSelected.videoUrl.text);
                //MelonLogger.Msg($"Adding {vidname} {VideoPlayerSelected.videoUrl.text}");
            }
        }

        private static void SaveHistoryUrl(string vidname, string url)
        {
            var dateFormatOut = "yyyy'-'MM'-'dd";
            if (!String.IsNullOrWhiteSpace(url))
            {
                if (!File.Exists(FolderRoot + histroyURLFileName))
                {
                    using StreamWriter sw = File.CreateText(FolderRoot + histroyURLFileName);
                    sw.WriteLine(DateTime.Now.ToString(dateFormatOut) + " ␟ " + vidname + " ␟ " + url);
                }
                else
                {
                    if (File.Exists(FolderRoot + histroyURLFileName))
                    {
                        using StreamWriter sw = File.AppendText(FolderRoot + histroyURLFileName);
                        sw.WriteLine(DateTime.Now.ToString(dateFormatOut) + " ␟ " + vidname + " ␟ " + url);
                    }
                }
            }
        }

        System.Collections.IEnumerator ReloadVideo()
        {
            TryingToReplay = true;
            if (VideoPlayerSelected?.videoPlayer != null) {
                var existingVideoName = Utils.VideoNameFormat(VideoPlayerSelected);
                var existingURL = VideoPlayerSelected.videoPlayer.lastNetworkVideoUrl;
                var existingTimeStamp = VideoPlayerSelected.videoPlayer.VideoPlayer.Time;
                var timeout = Time.time + 30f;

                MelonLogger.Msg($"Replaying video at Timestamp - Name: {existingVideoName}\n" +
                    $"URL: {existingURL} - TimeStamp: {existingTimeStamp}, TimeStamp: {Utils.FormatTime((float)existingTimeStamp)}");

                VideoPlayerSelected.videoPlayer.SetVideoUrl(existingURL);

                while (Time.time < timeout && VideoPlayerSelected?.videoPlayer != null)
                {
                    if (VideoPlayerSelected.videoPlayer.VideoPlayer.Time < existingTimeStamp && Utils.VideoNameFormat(VideoPlayerSelected) == existingVideoName)
                    {
                        yield return new WaitForSecondsRealtime(2f);//Just to give some buffer
                        MelonLogger.Msg($"Setting timestamp {existingTimeStamp}");
                        VideoPlayerSelected.videoPlayer.SetVideoTimestamp(existingTimeStamp);
                        TryingToReplay = false;
                        break;
                    }
                    yield return new WaitForSecondsRealtime(.5f);
                }
            }
            TryingToReplay = false;
        }

        private static void RefreshMainPage()
        {
            //MelonLogger.Msg(lastQMPage);
            if (lastQMPage == MainPageString)
            {
                try 
                { 
                    var CVRvpS = GameObject.FindObjectsOfType<CVRVideoPlayer>();
                    if (CVRvpS.Length == 1)
                    {//If only one player in the world, find and use it
                        var CVRvp = CVRvpS[0];
                        List<IVideoPlayerUi> savedvpui = Traverse.Create(CVRvp).Field("VideoPlayerUis").GetValue<List<IVideoPlayerUi>>();
                        foreach (ViewManagerVideoPlayer vp in savedvpui.Cast<ViewManagerVideoPlayer>())
                        {
                                VideoPlayerSelected = vp;
                        }
                    }
                }
                catch (Exception ex) { MelonLogger.Error("Error getting default videoplayer \n" + ex.ToString()); }
                SetCurrentVideoName();
                SingleURLhistory();
                if (VideoPlayerSelected != null)
                {

                    volumeSilder.SetSliderValue(VideoPlayerSelected.videoPlayer.playbackVolume);
                }
            }
        }

        public static void RefreshPage(bool laggyPages)
        {
            if (lastQMPage == VideoFolderString)
            {
                PopulateVideoList();
            }
            if (lastQMPage == AdvOptionsString)
            {
                CreatePageAdvOptionsPage(false);
            }
            if (lastQMPage == SponsorSkipEventsString)
            {
                CreatePageSponsorSkip();
            }
            if (lastQMPage == savedURLsPageString && laggyPages)
            {
                CreatePagesavedURLsPage();
            }
            if (lastQMPage == TimeStampPageString)
            {
                CreatePageTimeStampPage();
            }
            if (lastQMPage == LogPageString)
            {
                CreatePageLogPage();
            }
            if (lastQMPage == DebugPageString)
            {
                CreatePageDebug();
            }
            if (lastQMPage == URLHistoryPageString && laggyPages)
            {
                CreatePageURLsHistroyPage();
                SingleURLhistory();
            }
        }

        //So many methods to make sure this refreshes on change
        public static void OnPageOpen(string targetPage, string lastPage)
        {
            lastQMPage = targetPage;
            RefreshPage(true);
            RefreshMainPage();
        }
        public static void OnPageBack(string targetPage, string lastPage)
        {
            lastQMPage = targetPage;
            RefreshPage(true);
            RefreshMainPage();
        }
        public static void QMtoggle(bool __0)
        {
            if (__0)
            {
                RefreshPage(false);
                RefreshMainPage();
            }
        }

        private static void ToggleLocalScreen()
        {
            if (!localScreen?.Equals(null) ?? false)
            {
                try { UnityEngine.Object.Destroy(localScreen); } catch (System.Exception ex) { MelonLogger.Msg(System.ConsoleColor.DarkRed, ex.ToString()); }
                localScreen = null;
            }
            else if (VideoPlayerSelected != null)
            {
                GameObject cam = Camera.main.gameObject;
                Vector3 pos = cam.transform.position + (cam.transform.forward * 2f); // Gets position of Head 
                GameObject _obj = GameObject.CreatePrimitive(PrimitiveType.Plane);
                _obj.transform.position = pos;
                _obj.transform.LookAt(cam.transform);
                _obj.transform.rotation = _obj.transform.rotation * Quaternion.AngleAxis(90, Vector3.right); //Flip so screen is visible 
                _obj.name = "LocalVideoScreen-Mod";
                _obj.transform.localScale = new Vector3(1.777f * sizeScale / 10f, 1f * sizeScale / 10f, 1f * sizeScale / 10f);

                _obj.GetComponent<MeshCollider>().enabled = false;
                _obj.AddComponent<BoxCollider>();
                _obj.GetComponent<BoxCollider>().size = new Vector3(10f, .05f, 10f);
                _obj.GetComponent<BoxCollider>().isTrigger = true;

                Material mat = new Material(Shader.Find("Unlit/Texture"));
                mat.mainTexture = VideoPlayerSelected.videoPlayer.ProjectionTexture;
                _obj.GetComponent<MeshRenderer>().material = mat;

                _obj.AddComponent<CVRPickupObject>();
                _obj.GetComponent<CVRPickupObject>().maximumGrabDistance = 30f;
                _obj.GetComponent<CVRPickupObject>().enabled = pickupable;
                _obj.GetComponent<CVRPickupObject>().gripType = CVRPickupObject.GripType.Free;

                localScreen = _obj;
            }
        }
        private static void UpdateLocalScreen()
        {
            if (!localScreen?.Equals(null) ?? false)
            {
                localScreen.transform.localScale = new Vector3(1.777f * sizeScale / 10f, 1f * sizeScale / 10f, 1f * sizeScale / 10f);
                localScreen.GetComponent<CVRPickupObject>().enabled = pickupable;
            }
        }

        private static void StartSponsorSkip()
        {
            if (sponsorSkipCheckCoroutine != null) MelonCoroutines.Stop(sponsorSkipCheckCoroutine);
            if (sponsorSkip) sponsorSkipCheckCoroutine = MelonCoroutines.Start(SponsorSkipCheck());
        }

        public static System.Collections.IEnumerator SponsorSkipCheck()
        {
            bool startedSkips = false;
            while (sponsorSkip)
            {
                if (Utils.IsVideoPlayerValid(VideoPlayerSelected))
                {
                    yield return new WaitForSeconds(1f);
                    if (VideoPlayerSelected.videoPlayer.lastNetworkVideoUrl != lastvideo && !String.IsNullOrWhiteSpace(VideoPlayerSelected.videoPlayer.lastNetworkVideoUrl))
                    {
                        startedSkips = false;
                        string newVid = VideoPlayerSelected.videoPlayer.lastNetworkVideoUrl;
                        lastvideo = newVid;
                        MelonLogger.Msg($"|SponsorBlock| New video found: {newVid}");
                        GetAsync(newVid);
                    }
                    else if (VideoPlayerSelected.videoPlayer.lastNetworkVideoUrl == sponsorskipVideo && !startedSkips)
                    {
                        MelonLogger.Msg($"|SponsorBlock| Current video has skip info - {sponsorskipVideo}");
                        //MelonLogger.Msg($"|SponsorBlock| Raw");
                        //foreach (var x in sponsorskipResult)
                        //{
                        //    MelonLogger.Msg($"|SponsorBlock| {x.category} Segment: {Utils.FormatTime(x.segment[0])} - {Utils.FormatTime(x.segment[1])} Seconds:{x.segment[0]}-{x.segment[1]}");
                        //}

                        PrepSkipList();
                        startedSkips = true;
                        if (sponsorSkipLiveCoroutine != null) MelonCoroutines.Stop(sponsorSkipLiveCoroutine);
                        sponsorSkipLiveCoroutine = MelonCoroutines.Start(SponsorSkipping());

                        CreatePageSponsorSkip();
                    }
                }
            }
            sponsorSkip = false;
        }

        //https://www.youtube.com/watch?v=FxNiVwMQw1E
        //https://sponsor.ajay.app/api/skipSegments?videoID=FxNiVwMQw1E&category=sponsor //Example
        //https://wiki.sponsor.ajay.app/w/API_Docs
        public static async Task GetAsync(string url)
        {
            if (!url.Contains("youtu"))
                return;
            //https://stackoverflow.com/a/39777772
            var uri = new Uri(url);
            var query = HttpUtility.ParseQueryString(uri.Query);
            var videoId = string.Empty;
            if (query.AllKeys.Contains("v"))
            {
                videoId = query["v"];
            }
            else
            {
                videoId = uri.Segments.Last();
            }

            string requestURL = $"https://sponsor.ajay.app/api/skipSegments?videoID={videoId}&category=sponsor&category=selfpromo&category=interaction&category=intro&category=preview&category=music_offtopic&category=poi_highlight&category=chapter";
            //MelonLogger.Msg($"|SponsorBlock| Request URL {requestURL}");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestURL);
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    var data = await reader.ReadToEndAsync();
                    //MelonLogger.Msg($"|SponsorBlock| Raw data {data}");
                    sponsorskipVideo = url;
                    sponsorskipResult = JsonConvert.DeserializeObject<SponsorSkipSegment[]>(data);
                }  
            }
            catch (Exception ex) {
                MelonLogger.Msg("|SponsorBlock| Couldn't get video from API: " + ex.Message); 
                sponsorskipVideo = "API_Repsonse_Not_Found";

                CreatePageSponsorSkip();
            }
        }

        public static void PrepSkipList()
        {
            sponsorskips.Clear();
            foreach (var x in sponsorskipResult)
            { 
                if((x.category == "sponsor" && sponsorSkip_sponsor.Value)
                   || (x.category == "selfpromo" && sponsorSkip_selfpromo.Value)
                   || (x.category == "interaction" && sponsorSkip_interaction.Value)
                   || (x.category == "intro" && sponsorSkip_intro.Value))
                {
                    sponsorskips.Add((x.category, x.segment));
                }
            }
            //MelonLogger.Msg($"|SponsorBlock| Skips Only");
            //foreach (var x in sponsorskips)
            //{
            //    MelonLogger.Msg($"|SponsorBlock| {x.Item1} Segment: {Utils.FormatTime(x.Item2[0])} - {Utils.FormatTime(x.Item2[1])} Seconds:{x.Item2[0]}-{x.Item2[1]}");
            //}
        }

        public static System.Collections.IEnumerator SponsorSkipping()
        {
            var vidDur = VideoPlayerSelected.videoPlayer.VideoPlayer.Info.VideoMetaData.GetDuration();
            while (sponsorSkip && VideoPlayerSelected?.videoPlayer?.VideoPlayer != null && VideoPlayerSelected.videoPlayer.lastNetworkVideoUrl == sponsorskipVideo)
            {
                yield return new WaitForSeconds(.25f);
                foreach (var x in sponsorskips)
                { //Check every x seconds if within a skip, if so, jump to end of skip
                    if (VideoPlayerSelected.videoPlayer.VideoPlayer.Time > x.Item2[0] && VideoPlayerSelected.videoPlayer.VideoPlayer.Time < x.Item2[1] - 5f  //Don't skip if close to end
                        && x.Item2[0] != lastskip[0] && x.Item2[1] != lastskip[1]//Don't repeat skips
                        && x.Item2[0] < vidDur - 5 && x.Item2[1] < vidDur - 5)//Do not skip to very end
                    {
                        MelonLogger.Msg($"|SponsorBlock| Skipping to: {Utils.FormatTime(x.Item2[1])}, Was at: {Utils.FormatTime((float)VideoPlayerSelected.videoPlayer.VideoPlayer.Time)}, Due to: {Utils.SkipCatSwitch(x.Item1)}");
                        CohtmlHud.Instance.ViewDropText($"|SponsorBlock|", $"{Utils.SkipCatSwitch(x.Item1)} - Skipping to: { Utils.FormatTime(x.Item2[1])}");
                        lastskip = x.Item2;
                        VideoPlayerSelected.videoPlayer.SetVideoTimestamp(x.Item2[1]);
                        break;
                    }
                }
            }
        }
    }

    public class SponsorSkipSegment
    {
        public float[] segment;
        public string UUID;
        public string category;
        public float videoDuration;
        public string actionType;
        public int locked;
        public int votes;
        public string description;
    }
}