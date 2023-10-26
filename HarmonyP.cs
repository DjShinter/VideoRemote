using MelonLoader;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using ABI.CCK.Components;
using HarmonyLib;
using ABI_RC.Core.InteractionSystem;



namespace VideoRemote
{
    [HarmonyPatch]
    internal class HarmonyPatches
    {

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(MovementSystem), nameof(MovementSystem.UpdateAnimatorManager))]
        //internal static void AfterUpdateAnimatorManager(CVRAnimatorManager manager)
        //{
        //    MelonLogger.Msg($"UpdateAnimatorManager");
        //}

        //HarmonyInstance.Patch(typeof(CVR_MenuManager).GetMethod(nameof(CVR_MenuManager.ToggleQuickMenu)), null, new HarmonyMethod(typeof(VideoRemoteMod).GetMethod(nameof(QMtoggle), BindingFlags.NonPublic | BindingFlags.Static)));


        [HarmonyPostfix]
        [HarmonyPatch(typeof(CVR_MenuManager), nameof(CVR_MenuManager.ToggleQuickMenu))]
        internal static void OnToggleQuickMenu(bool show)
        {
            try
            {
                VideoRemoteMod.QMtoggle(show);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Error in OnToggleQuickMenu \n" + ex.ToString());
            }
        }


        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(CVRVideoPlayer), nameof(CVRVideoPlayer.AddLogEntry))]
        //internal static void OnAddLogEntry(string username, string text)
        //{
        //    try
        //    {
        //        MelonLogger.Msg($"OnAddLogEntry: {username}, {text}");
        //    }
        //    catch (Exception ex)
        //    {
        //        MelonLogger.Warning("Error in OnAddLogEntry \n" + ex.ToString());
        //    }
        //}


        [HarmonyPostfix]
        [HarmonyPatch(typeof(CVRVideoPlayer), nameof(CVRVideoPlayer.StartedPlaying))]
        internal static void OnStartedPlaying()
        {
            try
            {
                //MelonLogger.Msg($"OnStartedPlaying");
                if(VideoRemoteMod.videoHistory_En.Value) VideoRemoteMod.AllURLhistory();
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Error in OnStartedPlaying \n" + ex.ToString());
            }
        }


        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(CVRVideoPlayer), nameof(CVRVideoPlayer.FinishedPlaying))]
        //internal static void OnFinishedPlaying()
        //{
        //    try
        //    {
        //        MelonLogger.Msg($"OnFinishedPlaying");
        //    }
        //    catch (Exception ex)
        //    {
        //        MelonLogger.Warning("Error in OnFinishedPlaying \n" + ex.ToString());
        //    }
        //}


        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(ABI_RC.VideoPlayer.YoutubeDl), nameof(ABI_RC.VideoPlayer.YoutubeDl.GetVideoMetaDataAsync))]
        //internal static void OnGetVideoMetaDataAsync()
        //{
        //    try
        //    {
        //        MelonLogger.Msg($"OnGetVideoMetaDataAsync");
        //    }
        //    catch (Exception ex)
        //    {
        //        MelonLogger.Warning("Error in OnGetVideoMetaDataAsync \n" + ex.ToString());
        //    }
        //}
    }
}

