using ABI_RC.Core.InteractionSystem;
using ABI_RC.VideoPlayer.Scripts;
using MelonLoader;
using UnityEngine;
namespace VideoRemote
{
    public sealed class VideoRemoteMod : MelonMod
    {
        
        public override void OnApplicationStart()
        {
            MelonLogger.Msg("Video Remote UI requires DasUi");
        }

        public static void ShinModInit()
        {
            ViewManager.Instance.gameMenuView.View.TriggerEvent("CVRAppShinModInstalled");
        }
        public static void PlayTheVideo()
        {
            ViewManagerVideoPlayer vp = Component.FindObjectOfType<ViewManagerVideoPlayer>();

            vp.Play();

        }
        public static void PastePlayVideoPlayer()
        {
            ViewManagerVideoPlayer vp = Component.FindObjectOfType<ViewManagerVideoPlayer>();
            vp.PasteAndPlay();
        }
        public static void PauseTheVideo()
        {
            ViewManagerVideoPlayer vp = Component.FindObjectOfType<ViewManagerVideoPlayer>();
            vp.Pause();
        }
    }
}