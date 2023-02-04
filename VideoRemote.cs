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

namespace VideoRemote
{
    public static class ModBuildInfo
    {
        public const string Name = "Video Remote";
        public const string Author = "Shin";
        public const string Version = "1.0.1";
        public const string Description = "This allows you to use the video player with the menu.";
        public const string DownloadLink = "https://github.com/DjShinter/VideoRemote/releases";
    }
    public sealed class VideoRemoteMod : MelonMod
    {

        private static Category PageCategory;
        private static List<Button> SavedButtons = new();
        private static List<ViewManagerVideoPlayer> SavedVP = new();
        public static bool _initalized = new();
        private static ViewManagerVideoPlayer VideoPlayerSelected = new();
        private const string FolderRoot = "UserData/VideoRemote/";
        private const string FolderConfig = "savedURLs.txt";

        public override void OnInitializeMelon()
        {
            if (!RegisteredMelons.Any(x => x.Info.Name.Equals("BTKUILib") && x.Info.SemanticVersion != null && x.Info.SemanticVersion.CompareTo(new SemVersion(1)) >= 0))
            {
                MelonLogger.Error("BTKUILib was not detected or it is outdated! VideoREmote cannot function without it!");
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
            SetupUI();


            void SetupUI()
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
                        MelonLogger.Msg("Video Player Not Selected or does not exist.");
                    }

                };
                var button3 = category.AddButton("Paste and Play Video", "VideoPlayerModButton", "Paste and Play the Video");
                button3.OnPress += () =>
                {
                    if (VideoPlayerSelected != null)
                    {
                        VideoPlayerSelected.PasteAndPlay();
                    }
                    else
                    {
                        MelonLogger.Msg("Video Player Not Selected or does not exist.");
                    }

                };
                var button4 = category.AddButton("Save URL", "VideoPlayerModButton", "Stores this into the Mod Folder, if you wanted the URL");
                button4.OnPress += () =>
                {
                    if (VideoPlayerSelected != null)
                    {
                        SaveUrl(VideoPlayerSelected);

                    }
                    else
                    {
                        MelonLogger.Msg("Video Player Not Selected or does not exist.");
                    }

                };

                var Folder = category.AddPage("Video Players", "VideoPlayerModLogo", "Video Players in the World List", "VideoRemoteMod");
                var FolderCategory = Folder.AddCategory("Video Players In World");
                PageCategory = FolderCategory;
                var buttonVP1 = FolderCategory.AddButton("Load Video Players", "VideoPlayerModLogo", "Load the Video Players");
                buttonVP1.OnPress += () =>
                {
                    DeleteAllButtons();
                    SavedButtons.Clear();
                    SavedVP.Clear();
                    foreach (ViewManagerVideoPlayer vp in GameObject.FindObjectsOfType<ViewManagerVideoPlayer>())
                    {
                        
                        AddButton(vp);
                    }
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

                    using (StreamWriter sw = File.CreateText(FolderRoot + FolderConfig))
                    {
                        sw.WriteLine(DateTime.Now + " " + vidname + " " + vp.videoUrl.text);
                    }
                }
                else
                {
                    if (File.Exists(FolderRoot + FolderConfig))
                    {
                        string vidname = vp.videoName.text;
                        vidname = vidname.Remove(0, 26);

                        using (StreamWriter sw = File.AppendText(FolderRoot + FolderConfig))
                        {
                            sw.WriteLine(DateTime.Now + " " + vidname + " " + vp.videoUrl.text);
                        }
                    }
                }
            }
            else
            {
                MelonLogger.Msg("There was nothing to save.");
            }
               
        }


        private static void AddButton(ViewManagerVideoPlayer vp)
        {

            var button = PageCategory.AddButton(vp.name, "VideoPlayerModButton", "Select " + vp.name + " to be used for remoting.");
            button.OnPress += () =>
            {
                VideoPlayerSelected = vp;

                MelonLogger.Msg(vp.name + " has been selected");

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
    }
}