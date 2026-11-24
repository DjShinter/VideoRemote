using System;
using System.Reflection;

using System.Runtime.InteropServices;
using MelonLoader;
using VideoRemote;


[assembly: AssemblyTitle(ModBuildInfo.Name)]
[assembly: AssemblyDescription(ModBuildInfo.Description)]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct(ModBuildInfo.Name)]
[assembly: AssemblyCopyright("Copyright © 2023")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion(ModBuildInfo.Version)]
[assembly: AssemblyFileVersion(ModBuildInfo.Version)]
[assembly: MelonInfo(typeof(VideoRemoteMod), ModBuildInfo.Name, ModBuildInfo.Version, ModBuildInfo.Author, ModBuildInfo.DownloadLink)]
[assembly: MelonGame("ChilloutVR", "ChilloutVR")]
[assembly: MelonColor(255, 139, 0, 139)]
[assembly: MelonAuthorColor(255, 139, 0, 139)]
[assembly: MelonPriority(0)]
[assembly: HarmonyDontPatchAll]