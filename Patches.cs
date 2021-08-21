using HarmonyLib;
using ABI_RC.Core.InteractionSystem;
using System;
using VideoRemote;
[HarmonyPatch(typeof(ViewManager), "RegisterEvents")] // Patch ViewManager.RegisterEvents
public class VideoRemotePatch
{
    [HarmonyPostfix] // We are doing a PostFix
    public static void Postfix(ViewManager __instance) // I'm referencing ViewManager as  __instance to replace "this.gameMenuView"
    {
        __instance.gameMenuView.View.BindCall("CVRAppShinModInstalled", new Action(VideoRemoteMod.ShinModInit)); // we have it direct to ShinModInit to trigger the event for the UI Mod check, thanks Neradon! https://github.com/Neradon/Dasui
        __instance.gameMenuView.View.RegisterForEvent("CVRAppVideoPastePlay", new Action(VideoRemoteMod.PastePlayVideoPlayer)); // Paste the Video
        __instance.gameMenuView.View.RegisterForEvent("CVRAppVideoPlay", new Action(VideoRemoteMod.PlayTheVideo)); // Play the Video
        __instance.gameMenuView.View.RegisterForEvent("CVRAppVideoPause", new Action(VideoRemoteMod.PauseTheVideo)); // Pause the Video
        // code here will execute after the original method. You could use this if you want to do something with the result of the method.
    }
}