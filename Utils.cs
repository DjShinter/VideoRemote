using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using ABI_RC.VideoPlayer.Scripts;
using MelonLoader;
using UnityEngine;
using static VideoRemote.Loc;

namespace VideoRemote
{
    internal static class Utils
    {
        private const string YoutubeLinkRegex = @"(?:.+?)?(?:\/v\/|watch\/|\?v=|\&v=|youtu\.be\/|\/v=|^youtu\.be\/)([a-zA-Z0-9_-]{11})+";

        private static readonly Regex regexExtractId = new Regex(YoutubeLinkRegex, RegexOptions.Compiled);

        private static readonly string[] validAuthorities = { "youtube.com", "www.youtube.com", "youtu.be", "www.youtu.be" };

        // http://answers.unity.com/answers/261847/view.html
        internal static string GetPath(this Transform current)
        {
            if (current.parent == null)
                return "World:" + current.name;
            if (current.name.Contains("CVRSpawnable_"))
                return "Prop:";
            return current.parent.GetPath() + "/ " + current.name;
        }

        internal static string GetPlayerType(this Transform current)
        {
            if (current.parent == null)
                return "World";
            if (current.name.Contains("CVRSpawnable_"))
                return "Prop";
            if (current.name.Contains("_CVRSpawnable"))
                return "Prop";
            return current.parent.GetPlayerType();
        }

        internal static bool IsVideoPlayerValid(ViewManagerVideoPlayer vidPlay)
        {
            return vidPlay != null && !(vidPlay?.videoPlayer?.VideoPlayer?.Equals(null) ?? true);
        }

        internal static string VideoState(ViewManagerVideoPlayer vidPlay)
        {
            if (vidPlay?.videoName?.text == null) return "";
            if (vidPlay.videoName.text.Contains("Currently Playing:")) return T("Playing: ");
            if (vidPlay.videoName.text.Contains("Currently Paused:")) return T("Paused: ");
            return "";
        }

        internal static string VideoNameFormat(ViewManagerVideoPlayer vidPlay)
        {
            if (vidPlay?.videoName?.text == null) return T("No video player");
            if (vidPlay.videoName.text.Length < 25) return T("No video playing");
            if (vidPlay.videoName.text.Contains("Currently Playing:"))
                return vidPlay.videoName.text.Remove(0, 26);
            if (vidPlay.videoName.text.Contains("Currently Paused:"))
                return vidPlay.videoName.text.Remove(0, 25);
            var text = vidPlay.videoName.text.Remove(0, 26);
            return (text != "eo selected") ? text : T("No video playing");
        }

        internal static string SkipCatSwitch(string value)
        {
            switch (value)
            {
                case "sponsor":         return T("Sponsor");
                case "selfpromo":       return T("Self Promo");
                case "interaction":     return T("Interaction");
                case "intro":           return T("Intro");
                case "outro":           return T("Outro");
                case "preview":         return T("Preview");
                case "music_offtopic":  return T("Music Offtopic");
                case "poi_highlight":   return T("Highlight");
                case "chapter":         return T("Chapter");
                default:                return F("Unknown: {0}", value);
            }
        }

        internal static string FormatTime(float seconds)
        {
            return TimeSpan.FromSeconds(seconds).ToString("hh\\:mm\\:ss");
        }

        internal static int GetTimeSeg(float seconds, string type)
        {
            var t = TimeSpan.FromSeconds(seconds);
            switch (type)
            {
                case "hour": return int.Parse(t.ToString("hh"));
                case "min":  return int.Parse(t.ToString("mm"));
                case "sec":  return int.Parse(t.ToString("ss"));
                default:     return 0;
            }
        }

        internal static DateTime ParseDate(string value, string format)
        {
            if (DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
                return result;
            MelonLogger.Msg(ConsoleColor.Red, "Date Error: " + value);
            return DateTime.MinValue;
        }

        internal static string ExtractVideoIdFromUri(Uri uri)
        {
            try
            {
                var authority = new UriBuilder(uri).Uri.Authority.ToLower();
                if (validAuthorities.Contains(authority))
                {
                    var match = regexExtractId.Match(uri.ToString());
                    if (match.Success) return match.Groups[1].Value;
                }
            }
            catch { }
            return null;
        }
    }
}
