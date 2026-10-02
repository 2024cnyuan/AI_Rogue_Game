using System;
using System.IO;
using UnityEngine;

namespace Starfall
{
    [Serializable]
    public sealed class GameSettings
    {
        public int version = 1;
        public string language;
        public bool languageSelected;
        public bool fullscreen;
        public bool tutorialCompleted, tutorialSkipped, introductionSeen;
    }
    public sealed class SettingsStore
    {
        readonly string path;
        public bool ReadProblem { get; private set; }
        public SettingsStore(string directory) { path = Path.Combine(directory, "starfall-settings.json"); }
        public static string DefaultLanguage(SystemLanguage language) => language == SystemLanguage.ChineseSimplified || language == SystemLanguage.Chinese ? "zh-CN" : "en";
        public GameSettings Load(string defaultLanguage)
        {
            GameSettings result = null;
            if (File.Exists(path))
            {
                result = Read(path);
                if (result == null)
                {
                    ReadProblem = true;
                    // Preserve the original even if a later explicit settings save replaces it.
                    try { File.Copy(path, path + ".corrupt-" + DateTime.UtcNow.Ticks, false); } catch (IOException) { } catch (UnauthorizedAccessException) { }
                    result = Read(path + ".bak");
                }
            }
            if (result == null) result = new GameSettings { language = defaultLanguage };
            if (result.language != "zh-CN" && result.language != "en") { result.language = defaultLanguage; ReadProblem = true; }
            return result;
        }
        static GameSettings Read(string file)
        {
            try
            {
                if (!File.Exists(file)) return null;
                var value = JsonUtility.FromJson<GameSettings>(File.ReadAllText(file));
                return value != null && value.version == 1 ? value : null;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException) { return null; }
        }
        public bool Save(GameSettings settings)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (var stream = new FileStream(path + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream)) { writer.Write(JsonUtility.ToJson(settings, true)); writer.Flush(); stream.Flush(true); }
                if (File.Exists(path)) File.Replace(path + ".tmp", path, path + ".bak");
                else File.Move(path + ".tmp", path);
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is NotSupportedException)
            { Debug.LogWarning("Starfall settings write failed: " + e.Message); return false; }
        }
    }
}
