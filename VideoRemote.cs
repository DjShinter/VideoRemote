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
using Semver;
using ABI.CCK.Components;
using ABI_RC.Core.InteractionSystem;
using HarmonyLib;

namespace VideoRemote
{
    public static class ModBuildInfo
    {
        public const string Name = "Video Remote";
        public const string Author = "Shin, Nirvash";
        public const string Version = "1.1.1";
        public const string Description = "This allows you to use the video player with the menu.";
        public const string DownloadLink = "https://github.com/DjShinter/VideoRemote/releases";
    }
    public sealed class VideoRemoteMod : MelonMod
    {
        private static Category PageCategory, AdvancedOptions;
        private static SliderFloat volumeSilder;
        private static ToggleButton networkSyncButt;
        private static Button permissionButt, audioModeButt, button4;
        private static string VideoFolderString, MainPageString;
        private static readonly List<Button> SavedButtons = new();
        private static readonly List<ViewManagerVideoPlayer> SavedVP = new();

        public static bool _initalized = new();
        private static ViewManagerVideoPlayer VideoPlayerSelected = new();
        private const string FolderRoot = "UserData/VideoRemote/";
        private const string FolderConfig = "savedURLs.txt";

        private static GameObject localScreen;
        private static bool pickupable = false;
        private static float sizeScale = 1;

        public override void OnInitializeMelon()
        {
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
        }


        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (SceneManager.GetActiveScene().name == "Init")
            {
                if (!_initalized)
                    _initalized = true;
                SetupIcons();
            }
            DeleteAllButtons();
            VideoPlayerSelected = null;
            SavedButtons.Clear();
            SavedVP.Clear();
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

                var category = CustomPage.AddCategory("Video Remote Controls");
                var Folder = category.AddPage("Select Video Player", "VideoPlayerModLogo", "Video Players in the World List", "VideoRemoteMod");
                VideoFolderString = Folder.ElementID;
                var FolderCategory = Folder.AddCategory("Video Players In World");
                PageCategory = FolderCategory;
                var buttonVP1 = FolderCategory.AddButton("Load Video Players", "VideoPlayerModLogo", "Load the Video Players");
                buttonVP1.OnPress += () =>
                {
                    PopulateVideoList();
                };

                var button = category.AddButton("Play Video", "VideoPlayerModPlay2", "Play the Video");
                button.OnPress += () =>
                {

                    if (VideoPlayerSelected != null)
                    {
                        VideoPlayerSelected.Play();
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

                AdvancedOptions = CustomPage.AddCategory("");
                PopulateAdvancedButtons();

                var category2 = CustomPage.AddCategory("Local Video Player Screen");
                var buttSpawnScreen = category2.AddButton("Spawn/Toggle Local Screen", "VideoPlayerModNewScreen", "Creates a local copy of the video player screen in front of you.<p>You must select a video player first.");
                buttSpawnScreen.OnPress += () =>
                {
                    if (VideoPlayerSelected != null)
                    {
                        ToggleLocalScreen();
                    }
                    else
                    {
                        QuickMenuAPI.ShowAlertToast("Can not create local screen. Video Player Not Selected or does not exist.", 3);
                        MelonLogger.Msg("Can not create local screen. Video Player Not Selected or does not exist.");
                    }
                };
                var buttSmaller = category2.AddButton("Smaller", "VideoPlayerModWhite-Minus", "Decreases the screen size");
                buttSmaller.OnPress += () =>
                {
                    if (sizeScale > .15) sizeScale -= .15f;
                    UpdateLocalScreen();
                };
                var buttLarger = category2.AddButton("Larger", "VideoPlayerModWhite-Plus", "Increases the screen size");
                buttLarger.OnPress += () =>
                {
                    sizeScale += .15f;
                    UpdateLocalScreen();
                };
                var buttPickup = category2.AddToggle("Pickupable", "Toggles pickup of the local screen", pickupable);
                buttPickup.OnValueUpdated += action =>
                {
                    pickupable = action;
                    UpdateLocalScreen();
                };
            }
        }



        private static void SaveUrl(ViewManagerVideoPlayer vp)
        {

            if (vp.videoUrl.text != null)
            {
                if (!File.Exists(FolderRoot + FolderConfig))
                {
                    string vidname = vp.videoName.text;
                    vidname = vidname.Remove(0, 26);

                    using StreamWriter sw = File.CreateText(FolderRoot + FolderConfig);
                    sw.WriteLine(DateTime.Now + " " + vidname + " " + vp.videoUrl.text);
                }
                else
                {
                    if (File.Exists(FolderRoot + FolderConfig))
                    {
                        string vidname = vp.videoName.text;
                        vidname = vidname.Remove(0, 26);

                        using StreamWriter sw = File.AppendText(FolderRoot + FolderConfig);
                        sw.WriteLine(DateTime.Now + " " + vidname + " " + vp.videoUrl.text);
                    }
                }
            }
            else
            {
                MelonLogger.Msg("There was nothing to save.");
            }

        }


        private static void AddButton(CVRVideoPlayer CVRvp, ViewManagerVideoPlayer vp)
        {
            var dist = Math.Abs(Vector3.Distance(CVRvp.gameObject.transform.position, Camera.main.transform.position)).ToString("F2").TrimEnd('0');
            var button = PageCategory.AddButton("Video Player \n (Hover)", "VideoPlayerModVideoPlayer", $"{Utils.GetPath(CVRvp.gameObject.transform)}<p>Distance: {dist}");// | {CVRvp.playerId}");
            button.OnPress += () =>
            {
                VideoPlayerSelected = vp;

                MelonLogger.Msg(CVRvp.playerId + " has been selected");

            };
            SavedButtons.Add(button);
        }
        private static void DeleteAllButtons()
        {
            if (SavedButtons.Count > 0)
            {
                foreach (Button button in SavedButtons)
                {
                    button.Delete();
                }
            }

        }

        private static void PopulateVideoList()
        {
            DeleteAllButtons();
            SavedButtons.Clear();
            SavedVP.Clear();
            if (GameObject.FindObjectOfType<CVRVideoPlayer>())
            {
                foreach (CVRVideoPlayer vp in GameObject.FindObjectsOfType<CVRVideoPlayer>())
                {
                    List<IVideoPlayerUi> savedvpui = Traverse.Create(vp).Field("VideoPlayerUis").GetValue<List<IVideoPlayerUi>>();
                    foreach (ViewManagerVideoPlayer vp2 in savedvpui.Cast<ViewManagerVideoPlayer>())
                    {
                        AddButton(vp, vp2);
                    }
                }
            }
        }

        private static void PopulateAdvancedButtons()
        { //Doing this way so the button states always show the current value from the video player
            if (permissionButt != null) permissionButt.Delete();
            if (networkSyncButt != null) networkSyncButt.Delete();
            if (audioModeButt != null) audioModeButt.Delete();
            if (button4 != null) button4.Delete();
            {
                var current = (VideoPlayerSelected != null) ? VideoPlayerSelected.videoPlayer.ControlPermission.ToString() : "None Selected";
                var butt = AdvancedOptions.AddButton($"Permission: {current}", "VideoPlayerModKeys", $"Toggles the video player permission between Everyone and InstanceModerators");
                butt.OnPress += () =>
                {
                    if (VideoPlayerSelected != null)
                    {
                        VideoPlayerUtils.ControlPermission setTo = VideoPlayerUtils.ControlPermission.InstanceOwner;
                        if (VideoPlayerSelected.videoPlayer.ControlPermission == VideoPlayerUtils.ControlPermission.Everyone)
                            setTo = VideoPlayerUtils.ControlPermission.InstanceModerators;
                        else
                            setTo = VideoPlayerUtils.ControlPermission.Everyone;

                        VideoPlayerSelected.videoPlayer.SetControlPermission(setTo);
                        butt.ButtonText = $"Permission: {setTo}";
                    }
                    else
                    {
                        QuickMenuAPI.ShowAlertToast("Video Player Not Selected or does not exist.", 2);
                        MelonLogger.Msg("Video Player Not Selected or does not exist.");
                    }
                };
                permissionButt = butt;
            }
            {
                var butt = AdvancedOptions.AddToggle("Network Sync", "Toggles Network Sync to state", true);
                butt.OnValueUpdated += action =>
                {
                    if (VideoPlayerSelected != null)
                    {
                        VideoPlayerSelected.videoPlayer.SetNetworkSync(action);
                    }
                    else
                    {
                        QuickMenuAPI.ShowAlertToast("Video Player Not Selected or does not exist.", 3);
                        MelonLogger.Msg("Video Player Not Selected or does not exist.");
                    }
                };
                networkSyncButt = butt;
            }
            {
                var current = (VideoPlayerSelected != null) ? VideoPlayerSelected.videoPlayer.audioPlaybackMode.ToString() : "None Selected";
                var butt = AdvancedOptions.AddButton($"Audio Mode: {current}", "VideoPlayerModSound", $"Toggles audio mode between Direct, AudioSource, RoomScale<p>Currently: {current}");
                butt.OnPress += () =>
                {
                    if (VideoPlayerSelected != null)
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
                        catch (System.Exception ex) { 
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
                audioModeButt = butt;
            }

            button4 = AdvancedOptions.AddButton("Save URL", "VideoPlayerModSave", $"Stores the current video URL into the ChilloutVR/{FolderRoot} Folder.");
            button4.OnPress += () =>
            {
                if (VideoPlayerSelected != null)
                {
                    SaveUrl(VideoPlayerSelected);
                    QuickMenuAPI.ShowAlertToast($"Saved URL! Located in ChilloutVR/{FolderRoot}{FolderConfig}", 3);
                    MelonLogger.Msg($"Saved URL! Located in ChilloutVR/{FolderRoot}{FolderConfig}");
                }
                else
                {
                    QuickMenuAPI.ShowAlertToast("Video Player Not Selected or does not exist.", 2);
                    MelonLogger.Msg("Video Player Not Selected or does not exist.");
                }

            };
        }
        public static void OnPageOpen(string targetPage, string lastPage)
        {
            if (targetPage == VideoFolderString)
            {
                PopulateVideoList();
            }
            if (targetPage == MainPageString)
            {
                PopulateAdvancedButtons();
                if (VideoPlayerSelected != null)
                {
                    volumeSilder.SetSliderValue(VideoPlayerSelected.videoPlayer.playbackVolume);
                }
            }
        }
        public static void OnPageBack(string targetPage, string lastPage)
        {
            if (targetPage == MainPageString)
            {
                PopulateAdvancedButtons();
                if (VideoPlayerSelected != null)
                {
                    volumeSilder.SetSliderValue(VideoPlayerSelected.videoPlayer.playbackVolume);
                }
            }
        }

        private static void ToggleLocalScreen()
        {
            if (!localScreen?.Equals(null) ?? false)
            {
                try { UnityEngine.Object.Destroy(localScreen); } catch (System.Exception ex) { MelonLogger.Msg(ConsoleColor.DarkRed, ex.ToString()); }
                localScreen = null;
            }
            else
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
    }

    public static class Utils
    {
        public static string GetPath(this Transform current)
        { //http://answers.unity.com/answers/261847/view.html
            if (current.parent == null)
                return "World:" + current.name;
            if (current.name.Contains("_CVRSpawnable"))
                return "Prop:";
            return current.parent.GetPath() + "/ " + current.name;
        }
    }
}