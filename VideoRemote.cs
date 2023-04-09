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
        public const string Author = "Shin";
        public const string Version = "1.1.1";
        public const string Description = "This allows you to use the video player with the menu.";
        public const string DownloadLink = "https://github.com/DjShinter/VideoRemote/releases";
    }
    public sealed class VideoRemoteMod : MelonMod
    {

        private static Category PageCategory;
        private static readonly List<Button> SavedButtons = new();
        private static readonly List<ViewManagerVideoPlayer> SavedVP = new();

        public static bool _initalized = new();
        private static ViewManagerVideoPlayer VideoPlayerSelected = new();
        private const string FolderRoot = "UserData/VideoRemote/";
        private const string FolderConfig = "savedURLs.txt";

        private static string VideoFolderString;
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
                if(!_initalized)
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
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "VideoPlayerModPlay", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.Play.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "VideoPlayerModPause", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.Pause.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "VideoPlayerModButton", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.Button.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "NewScreen", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.NewScreen.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "White-Minus", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.White-Minus.png"));
            QuickMenuAPI.PrepareIcon("VideoRemoteMod", "White-Plus", Assembly.GetExecutingAssembly().GetManifestResourceStream("VideoRemote.UI.Images.White-Plus.png"));

            SetupUI();
            QuickMenuAPI.OnOpenedPage += OnVideoPlayersFolderOpen;

            static void SetupUI()
            {
                var CustomPage = new Page("VideoRemoteMod", "VideoRemotePage", true, "VideoPlayerModLogo")
                {
                    //This sets the title that appears at the very top in the header bar
                    MenuTitle = ModBuildInfo.Name,
                    MenuSubtitle = ModBuildInfo.Description
                };

                var category = CustomPage.AddCategory("Video Remote Controls");
                
                var button = category.AddButton("Play Video", "VideoPlayerModPlay", "Play the Video");
                button.OnPress += () =>
                {

                    if (VideoPlayerSelected != null)
                    {
                        VideoPlayerSelected.Play();
                    }
                    else
                    {
                        QuickMenuAPI.ShowAlertToast("Video Player Not Selected or does not exist.", 1);
                        MelonLogger.Msg("Video Player Not Selected or does not exist.");
                    }

                };
                var button2 = category.AddButton("Pause Video", "VideoPlayerModPause", "Pause the Video");
                button2.OnPress += () =>
                {
                    if (VideoPlayerSelected != null)
                    {
                        VideoPlayerSelected.Pause();
                    }
                    else
                    {
                        QuickMenuAPI.ShowAlertToast("Video Player Not Selected or does not exist.", 1);
                        MelonLogger.Msg("Video Player Not Selected or does not exist.");
                    }

                };
                var button3 = category.AddButton("Paste and Play Video", "VideoPlayerModButton", "Paste and Play the Video");
                button3.OnPress += () =>
                {
                    if (VideoPlayerSelected != null)
                    {
                        QuickMenuAPI.ShowConfirm("Confirm", "Paste and Play Video?", () => {
                            VideoPlayerSelected.PasteAndPlay();
                        }, () => { }, "Yes", "No");
                    }
                    else
                    {
                        QuickMenuAPI.ShowAlertToast("Video Player Not Selected or does not exist.", 1);
                        MelonLogger.Msg("Video Player Not Selected or does not exist.");
                    }

                };
                var button4 = category.AddButton("Save URL", "VideoPlayerModButton", @"Stores this into the ChilloutVR\UserData\VideoRemote Folder, to see saved URLs.");
                button4.OnPress += () =>
                {
                    if (VideoPlayerSelected != null)
                    {
                        SaveUrl(VideoPlayerSelected);
                        QuickMenuAPI.ShowAlertToast($"Saved URL! Located in ChilloutVR/{FolderRoot}{FolderConfig}", 1);
                        MelonLogger.Msg($"Saved URL! Located in ChilloutVR/{FolderRoot}{FolderConfig}");
                    }
                    else
                    {
                        QuickMenuAPI.ShowAlertToast("Video Player Not Selected or does not exist.", 1);
                        MelonLogger.Msg("Video Player Not Selected or does not exist.");
                    }

                };

                var Folder = category.AddPage("Video Players", "VideoPlayerModLogo", "Video Players in the World List", "VideoRemoteMod");
                VideoFolderString = Folder.ElementID;
                var FolderCategory = Folder.AddCategory("Video Players In World");
                PageCategory = FolderCategory;
                var buttonVP1 = FolderCategory.AddButton("Load Video Players", "VideoPlayerModLogo", "Load the Video Players");
                buttonVP1.OnPress += () =>
                {
                    PopulateVideoList();
                };

                var category2 = CustomPage.AddCategory("Local Video Player Screen");
                var buttSpawnScreen = category2.AddButton("Spawn/Toggle Local Screen", "NewScreen", "Creates a local copy of the video player screen in front of you.<p>You must select a video player first.");
                buttSpawnScreen.OnPress += () =>
                {
                    if (VideoPlayerSelected != null)
                    {
                        ToggleLocalScreen();
                    }
                    else
                    {
                        QuickMenuAPI.ShowAlertToast("Can not create local screen. Video Player Not Selected or does not exist.", 2);
                        MelonLogger.Msg("Can not create local screen. Video Player Not Selected or does not exist.");
                    }
                };
                var buttSmaller = category2.AddButton("Smaller", "White-Minus", "Decreases the screen size");
                buttSmaller.OnPress += () =>
                {
                    if (sizeScale > .25) sizeScale -= .25f;
                    UpdateLocalScreen();
                };
                var buttLarger = category2.AddButton("Larger", "White-Plus", "Increases the screen size");
                buttLarger.OnPress += () =>
                {
                    sizeScale += .25f;
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

            if(vp.videoUrl.text != null)
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

            var button = PageCategory.AddButton("Video Player \n (Hover)", "VideoPlayerModButton", CVRvp.playerId);
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

        public static void OnVideoPlayersFolderOpen(string targetPage, string lastPage)
        {
            if (targetPage == VideoFolderString)
            {
                PopulateVideoList();
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
}