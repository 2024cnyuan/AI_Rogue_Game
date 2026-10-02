using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Starfall
{
    [Serializable] public sealed class PassiveSnapshot { public string id; public int layers; }
    [Serializable] public sealed class LoadoutSnapshot
    {
        public string special, weapon = "pistol", active;
        public float energy = 100, cooldown;
        public int charges;
        public List<PassiveSnapshot> passives = new List<PassiveSnapshot>();
    }
    [Serializable] public sealed class EntryCheckpoint
    {
        public int schema = 1, stage = 1, seed, coins;
        public string runId, contentVersion = "m3-v1";
        public bool active = true, eligible = true;
        public float health = 100;
        public LoadoutSnapshot loadout = new LoadoutSnapshot();
    }
    // A checkpoint is a complete entry snapshot, never a mid-room save.
    public sealed class CheckpointStore
    {
        readonly string path;
        readonly Func<string, string, bool> writer;
        public EntryCheckpoint Current { get; private set; }
        public bool ReadProblem { get; private set; }
        public bool WriteProblem { get; private set; }
        public bool HasEntry => Current != null && Current.active;
        public CheckpointStore(string directory, Func<string, string, bool> write = null)
        {
            path = Path.Combine(directory, "starfall-checkpoint.json"); writer = write ?? AtomicWrite;
            Current = Read(path);
            if (Current == null && ReadProblem) Current = Read(path + ".bak");
        }
        EntryCheckpoint Read(string file)
        {
            if (!File.Exists(file)) return null;
            try
            {
                var value = JsonUtility.FromJson<EntryCheckpoint>(File.ReadAllText(file));
                if (value == null || value.schema != 1 || value.contentVersion != "m3-v1" || value.active && !Valid(value)) throw new InvalidDataException();
                return value;
            }
            catch (Exception error) when (error is IOException || error is InvalidDataException || error is UnauthorizedAccessException || error is ArgumentException)
            {
                ReadProblem = true;
                try { File.Copy(file, file + ".corrupt-" + DateTime.UtcNow.Ticks, false); } catch (IOException) { } catch (UnauthorizedAccessException) { }
                return null;
            }
        }
        public static bool Valid(EntryCheckpoint value)
        {
            if (value.stage < 1 || value.stage > 3 || value.seed < 0 || value.coins < 0 || string.IsNullOrEmpty(value.runId) || value.loadout == null || value.loadout.passives == null || value.loadout.passives.Count > 6 ||
                float.IsNaN(value.health) || float.IsInfinity(value.health) || value.health <= 0 || value.health > 1000 || value.loadout.energy < 0 || value.loadout.energy > 100 || float.IsNaN(value.loadout.energy) || value.loadout.charges < 0 || value.loadout.charges > 20 ||
                float.IsNaN(value.loadout.cooldown) || float.IsInfinity(value.loadout.cooldown) || value.loadout.cooldown < 0 || value.loadout.cooldown > 300) return false;
            var ids = new HashSet<string>();
            foreach (var passive in value.loadout.passives) if (passive == null || string.IsNullOrEmpty(passive.id) || passive.layers < 1 || passive.layers > 2 || !ids.Add(passive.id)) return false;
            return true;
        }
        public bool Save(EntryCheckpoint entry)
        {
            if (entry == null || entry.active && !Valid(entry)) return false;
            var copy = JsonUtility.FromJson<EntryCheckpoint>(JsonUtility.ToJson(entry));
            if (!writer(path, JsonUtility.ToJson(copy, true))) { WriteProblem = true; return false; }
            Current = copy; WriteProblem = false; return true;
        }
        public bool Clear()
        {
            // The tombstone prevents a backup from reviving a dead/completed run.
            var tombstone = new EntryCheckpoint { active = false };
            bool saved = Save(tombstone); Current = tombstone;
            if (saved && writer == (Func<string, string, bool>)AtomicWrite)
            {
                try { File.Copy(path, path + ".bak", true); } catch (IOException) { WriteProblem = true; } catch (UnauthorizedAccessException) { WriteProblem = true; }
            }
            return saved && !WriteProblem;
        }
        static bool AtomicWrite(string file, string content)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file));
                using (var stream = new FileStream(file + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
                using (var output = new StreamWriter(stream)) { output.Write(content); output.Flush(); stream.Flush(true); }
                if (File.Exists(file)) File.Replace(file + ".tmp", file, file + ".bak"); else File.Move(file + ".tmp", file);
                return true;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException) { return false; }
        }
    }
}
