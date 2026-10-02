using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Starfall
{
    [Serializable] public sealed class TextEntry { public string key; public string value; }
    [Serializable] public sealed class TextTable { public TextEntry[] entries; }
    public sealed class LocalizationService
    {
        readonly Dictionary<string, string> chinese, english;
        public string Language { get; private set; }
        public event Action Changed;
        public LocalizationService(string language)
        {
            chinese = Load("zh-CN"); english = Load("en"); Language = language == "zh-CN" ? language : "en";
        }
        static Dictionary<string, string> Load(string language)
        {
            var result = new Dictionary<string, string>();
            var asset = Resources.Load<TextAsset>("Localization/" + language);
            if (asset == null) throw new InvalidOperationException("Missing localization table: " + language);
            foreach (var entry in JsonUtility.FromJson<TextTable>(asset.text).entries)
            {
                if (result.ContainsKey(entry.key)) throw new InvalidOperationException("Duplicate localization key: " + entry.key);
                result.Add(entry.key, entry.value);
            }
            return result;
        }
        public void SetLanguage(string language)
        {
            string supported = language == "zh-CN" ? "zh-CN" : "en";
            if (Language == supported) return;
            Language = supported; Changed?.Invoke();
        }
        public string Get(string key, params (string name, string value)[] values)
        {
            var primary = Language == "zh-CN" ? chinese : english;
            var fallback = Language == "zh-CN" ? english : chinese;
            if (!primary.TryGetValue(key, out string text))
            {
                Debug.LogError("Missing localization key: " + Language + "/" + key);
                if (!fallback.TryGetValue(key, out text)) return "[" + key + "]";
            }
            foreach (var value in values) text = text.Replace("{" + value.name + "}", value.value);
            return text;
        }
        public List<string> Validate()
        {
            var errors = new List<string>();
            foreach (var entry in chinese)
            {
                if (!english.TryGetValue(entry.Key, out string counterpart)) { errors.Add("en missing " + entry.Key); continue; }
                var a = Parameters(entry.Value); var b = Parameters(counterpart);
                if (!a.SetEquals(b)) errors.Add("Parameter mismatch " + entry.Key);
            }
            foreach (var key in english.Keys) if (!chinese.ContainsKey(key)) errors.Add("zh-CN missing " + key);
            return errors;
        }
        static HashSet<string> Parameters(string text)
        {
            var result = new HashSet<string>(); foreach (Match match in Regex.Matches(text, @"\{([A-Za-z0-9_]+)\}")) result.Add(match.Groups[1].Value); return result;
        }
    }
}
