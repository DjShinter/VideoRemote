using System;
using ABI.CCK.Components;
using ABI_RC.Core.InteractionSystem;
using HarmonyLib;
using MelonLoader;

namespace VideoRemote
{
    [HarmonyPatch]
    internal class HarmonyPatches
    {
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

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CVRVideoPlayer), "StartedPlaying")]
        internal static void OnStartedPlaying()
        {
            try
            {
                if (VideoRemoteMod.videoHistory_En.Value)
                {
                    VideoRemoteMod.AllURLhistory();
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Error in OnStartedPlaying \n" + ex.ToString());
            }
        }
    }
}
