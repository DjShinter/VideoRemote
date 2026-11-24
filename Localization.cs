using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ABI_RC.Core.Savior;
using MelonLoader;
using Newtonsoft.Json;

namespace VideoRemote
{
    // Menu translations. Keys are the English text, so anything missing falls back to English.
    // Languages/en.json is embedded as the example of the file format. Other languages are loaded
    // from UserData/VideoRemote/Languages/*.json and override an embedded one with the same code.
    // _template.json is written there on start with every menu string, to copy for a new language.
    internal static class Loc
    {
        internal const string AutoCode = "auto";
        private const string UserFolder = "UserData/VideoRemote/Languages/";
        private const string TemplateFile = "_template.json";

        private sealed class LanguageFile
        {
            [JsonProperty("code")] public string Code;
            [JsonProperty("name")] public string Name;
            [JsonProperty("strings")] public Dictionary<string, string> Strings;
        }

        private static readonly Dictionary<string, LanguageFile> Languages =
            new Dictionary<string, LanguageFile>(StringComparer.OrdinalIgnoreCase);

        private static Dictionary<string, string> _active;

        internal static string ActiveCode { get; private set; } = "en";

        // Code + display name of every language that can be picked (English first).
        internal static List<(string code, string name)> Available()
        {
            var list = new List<(string, string)> { ("en", "English") };
            list.AddRange(Languages.Values
                .Where(l => !string.Equals(l.Code, "en", StringComparison.OrdinalIgnoreCase))
                .OrderBy(l => l.Name, StringComparer.OrdinalIgnoreCase)
                .Select(l => (l.Code, l.Name)));
            return list;
        }

        internal static string NameOf(string code)
        {
            if (string.IsNullOrEmpty(code) || string.Equals(code, "en", StringComparison.OrdinalIgnoreCase)) return "English";
            return Languages.TryGetValue(code, out var l) ? l.Name : code;
        }

        internal static void Load()
        {
            Languages.Clear();
            var asm = typeof(Loc).Assembly;
            foreach (var res in asm.GetManifestResourceNames()
                         .Where(n => n.StartsWith("VideoRemote.Languages.", StringComparison.Ordinal) && n.EndsWith(".json", StringComparison.Ordinal)))
            {
                try
                {
                    using var s = asm.GetManifestResourceStream(res);
                    using var r = new StreamReader(s);
                    Add(JsonConvert.DeserializeObject<LanguageFile>(r.ReadToEnd()), res);
                }
                catch (Exception ex) { MelonLogger.Warning($"Could not read built-in language {res}: {ex.Message}"); }
            }

            try
            {
                if (!Directory.Exists(UserFolder)) Directory.CreateDirectory(UserFolder);
                WriteTemplate();
                foreach (var file in Directory.GetFiles(UserFolder, "*.json"))
                {
                    if (Path.GetFileName(file).StartsWith("_", StringComparison.Ordinal)) continue;
                    try { Add(JsonConvert.DeserializeObject<LanguageFile>(File.ReadAllText(file)), file); }
                    catch (Exception ex) { MelonLogger.Warning($"Could not read language file {file}: {ex.Message}"); }
                }
            }
            catch (Exception ex) { MelonLogger.Warning("Could not load user language files: " + ex.Message); }
        }

        private static void Add(LanguageFile lang, string source)
        {
            if (lang == null || string.IsNullOrWhiteSpace(lang.Code) || lang.Strings == null)
            {
                MelonLogger.Warning($"Skipping language file without a code or strings: {source}");
                return;
            }
            if (string.IsNullOrWhiteSpace(lang.Name)) lang.Name = lang.Code;
            Languages[lang.Code.Trim()] = lang;
        }

        // Writes every translatable string with its English text, as a starting point for new languages.
        private static void WriteTemplate()
        {
            var keys = Languages.Values.SelectMany(l => l.Strings.Keys).Distinct().OrderBy(k => k, StringComparer.Ordinal);
            var template = new LanguageFile
            {
                Code = "xx",
                Name = "Language name",
                Strings = keys.ToDictionary(k => k, k => k),
            };
            File.WriteAllText(UserFolder + TemplateFile, JsonConvert.SerializeObject(template, Formatting.Indented));
        }

        // Picks the language from the mod setting, or follows the game's language when set to auto.
        internal static void Apply(string setting)
        {
            var code = string.IsNullOrWhiteSpace(setting) || string.Equals(setting, AutoCode, StringComparison.OrdinalIgnoreCase)
                ? GameLanguage()
                : setting.Trim();
            if (Languages.TryGetValue(code, out var lang))
            {
                _active = lang.Strings;
                ActiveCode = lang.Code;
            }
            else
            {
                _active = null;
                ActiveCode = "en";
            }
            MelonLogger.Msg($"Menu language: {NameOf(ActiveCode)}");
        }

        internal static string GameLanguage()
        {
            try
            {
                var code = MetaPort.Instance?.settings?.GetSettingsString("GeneralLanguage");
                if (!string.IsNullOrWhiteSpace(code)) return code.Trim();
            }
            catch { /* settings not ready yet */ }
            return "en";
        }

        internal static string T(string english)
        {
            if (string.IsNullOrEmpty(english) || _active == null) return english;
            return _active.TryGetValue(english, out var t) && !string.IsNullOrEmpty(t) ? t : english;
        }

        internal static string F(string english, params object[] args)
        {
            var format = T(english);
            try { return string.Format(format, args); }
            catch (FormatException) { return string.Format(english, args); }
        }
    }
}
