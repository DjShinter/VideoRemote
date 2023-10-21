using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ABI_RC.VideoPlayer.Scripts;
using MelonLoader;
using UnityEngine;

namespace VideoRemote
{
    public static class Utils
    {
        public static string GetPath(this Transform current)
        { //http://answers.unity.com/answers/261847/view.html
            if (current.parent == null)
                return "World:" + current.name;
            if (current.name.Contains("CVRSpawnable_"))
                return "Prop:";
            return current.parent.GetPath() + "/ " + current.name;
        }
        public static string GetPlayerType(this Transform current)
        { //http://answers.unity.com/answers/261847/view.html
            if (current.parent == null)
                return "World";
            if (current.name.Contains("CVRSpawnable_"))
                return "Prop";
            if (current.name.Contains("_CVRSpawnable"))
                return "Prop";
            return current.parent.GetPlayerType();
        }

        public static bool IsVideoPlayerValid(ViewManagerVideoPlayer vidPlay)
        {
            return (vidPlay != null) && !(vidPlay?.videoPlayer?.VideoPlayer.Equals(null) ?? true)  ; 
        }

        public static string VideoState(ViewManagerVideoPlayer vidPlay)
        {
            if (vidPlay?.videoName?.text.Equals(null) ?? true)
                return "";

            if (vidPlay.videoName.text.Contains("Currently Playing:"))
                return "Playing: ";
            if (vidPlay.videoName.text.Contains("Currently Paused:"))
                return "Paused: ";

            return "";
        }

        public static string VideoNameFormat(ViewManagerVideoPlayer vidPlay)
        {
            //"Currently Paused:"
            //"Currently Playing:"
            if (vidPlay?.videoName?.text.Equals(null) ?? true)
                return "No video player";

            if (vidPlay.videoName.text.Length < 25)
                return "No video playing";

            if (vidPlay.videoName.text.Contains("Currently Playing:"))
                return vidPlay.videoName.text.Remove(0, 26);
            if (vidPlay.videoName.text.Contains("Currently Paused:"))
                return vidPlay.videoName.text.Remove(0, 25);

            var name = vidPlay.videoName.text.Remove(0, 26);
            return (name != "eo selected") ? name : "No video playing";
        }


        public static string SkipCatSwitch(string value)
        {
            switch (value)
            {
                case "sponsor": return "Sponsor";
                case "selfpromo": return "Selfpromo";
                case "interaction": return "Interaction";
                case "intro": return "Intro";
                case "outro": return "Outro";
                case "preview": return "Preview";
                case "music_offtopic": return "Music Offtopic";
                case "poi_highlight": return "Highlight";
                case "chapter": return "Chapter";
                default: return $"Unknown: {value}";
            }
        }

        public static string FormatTime(float seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(seconds);
            return time.ToString(@"hh\:mm\:ss");
        }

        public static int GetTimeSeg(float seconds, string type)
        {
            TimeSpan time = TimeSpan.FromSeconds(seconds);
            switch (type)
            {
                case "hour": return int.Parse(time.ToString(@"hh"));
                case "min": return int.Parse(time.ToString(@"mm"));
                case "sec": return int.Parse(time.ToString(@"ss"));
                default: return 0;
            }
        }

        public static DateTime ParseDate(string value, string format)
        {
            if (DateTime.TryParseExact(value, format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dt))
            {
                return dt;
            }
            MelonLogger.Msg(ConsoleColor.Red, $"Date Error: {value}");
            return DateTime.MinValue;
        }
    }
}
