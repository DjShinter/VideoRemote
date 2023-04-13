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
            if (current.name.Contains("_CVRSpawnable"))
                return "Prop:";
            return current.parent.GetPath() + "/ " + current.name;
        }

        public static string VideoNameFormat(ViewManagerVideoPlayer vidPlay)
        {
            var name = vidPlay.videoName.text.Remove(0, 26);
            return (name != "eo selected") ? name : "No video playing";
        }
    }
}
