using MelonLoader;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using ABI.CCK.Components;
using HarmonyLib;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using DarkRift;
using DarkRift.Client;
using ABI_RC.Core.Networking;
using ABI_RC.Core.Networking.IO.UserGeneratedContent;




namespace VideoRemote
{
    [HarmonyPatch]
    internal class HarmonyPatches
    {
        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(ABI_RC.Core.Networking.IO.UserGeneratedContent.VideoPlayer), nameof(VideoPlayer.HandleVideoPlayerCommand))]
        //internal static void OnHandleVideoPlayerCommand(ABI_RC.Core.Networking.IO.UserGeneratedContent.VideoPlayer.VideoPlayerCommandTypes_t t, Message message)
        //{
        //    if (VideoRemoteMod.worldLastJoin + 30 > Time.time && message.Tag == (ushort)Tags.VideoPlayerSetUrl)
        //    {
        //        using DarkRiftReader reader = message.GetReader();
        //        string vidPlayerID = reader.ReadString();
        //        //MelonLogger.Msg(ConsoleColor.Green, $"m:{message.Tag} - {vidPlayerID}");
        //        string playerName = CVRPlayerManager.Instance.TryGetPlayerName(reader.ReadString());
        //        string url = reader.ReadString();
        //        string objPath = reader.ReadString();
        //        bool isPaused = reader.ReadBoolean();
        //        //var currentPlayers = GameObject.FindObjectsOfType<CVRVideoPlayer>(false);
        //        if (CVRWorld.Instance._registeredVideoPlayers.Find((Predicate<CVRVideoPlayer>)(match => match.playerId == vidPlayerID)))
        //        { //If the video player exists in world, this doesn't need to run
        //            if (VideoRemoteMod.vidPlayerJoinBuffer.ContainsKey(vidPlayerID) && VideoRemoteMod.vidPlayerJoinBuffer[vidPlayerID].Item1 != url)
        //                VideoRemoteMod.vidJoinBuffer_toRemove.Add(vidPlayerID); //If another command comes in while we have one in the buffer, drop past one from buffer
        //            return;
        //        }
        //        //MelonLogger.Msg(ConsoleColor.Green, $"Player not found in world, adding to buffer");
            
        //        (string, bool, string, string, float) data = (url, isPaused, playerName, objPath, Time.time);
        //        VideoRemoteMod.vidPlayerJoinBuffer.TryAdd(vidPlayerID, data);
        //        //MelonLogger.Msg(ConsoleColor.Green, $"Added: {vidPlayerID} -- {data}");

        //        if (!VideoRemoteMod.vidPlayerJoinCoroutine_Run)
        //        {
        //            //MelonLogger.Msg(ConsoleColor.Green, $"Starting ProccessJoinBufferInit");
        //            MelonCoroutines.Start(VideoRemoteMod.Instance.ProccessJoinBufferInit());
        //        }
        //    }
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

