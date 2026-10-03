using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Starfall
{
    [Serializable] public sealed class StageClearReceipt
    {
        public string id;
        public int stage;
        public bool chosen;
        public string selected;
    }
    [Serializable] public sealed class PlayerProfile
    {
        public int version = 1;
        public List<string> unlocked = new List<string> { "pistol", "medkit" };
        public List<int> cleared = new List<int>();
        public List<StageClearReceipt> receipts = new List<StageClearReceipt>();
        public LoadoutSnapshot preparation = new LoadoutSnapshot { active = "medkit", charges = 2 };
    }
    public static class StageRewards
    {
        public static readonly string[] StageIds = { "gardens", "workshop", "reservoir", "greenhouse", "hub", "sanctum" };
        public static readonly string[] Signature = { "shotgun", "smg", "crossbow", "launcher", "shock", "arc" };
        public static readonly string[][] Pools = {
            new[] { "rapid", "magnet", "vitality" }, new[] { "shield", "blast", "workshop_smg" },
            new[] { "slow", "pierce", "agile" }, new[] { "grenade", "controlled", "flawless" },
            new[] { "bounce", "critical", "lowhealth" }, new[] { "decoy", "recharge" }
        };
        public static int Source(string id)
        {
            if (id == "pistol" || id == "medkit") return 0;
            for (int i = 0; i < 6; i++) if (Signature[i] == id || Array.IndexOf(Pools[i], id) >= 0) return i + 1;
            return -1;
        }
    }
    // Permanent ownership and pending choices commit together. Training never calls this store.
    public sealed class PlayerProfileStore
    {
        readonly string path;
        readonly string journalPath;
        readonly ItemCatalog catalog;
        readonly Func<string, string, bool> writer;
        public PlayerProfile Data { get; private set; }
        public bool ReadProblem { get; private set; }
        public bool WriteProblem { get; private set; }
        public PlayerProfileStore(string directory, ItemCatalog items, Func<string, string, bool> write = null)
        {
            path = Path.Combine(directory, "starfall-profile.json"); journalPath = Path.Combine(directory,"starfall-reward-journal.json"); catalog = items; writer = write ?? AtomicWrite;
            Data = Read(path);
            if (Data == null && ReadProblem) Data = Read(path + ".bak");
            Data = Data ?? new PlayerProfile();
            if(File.Exists(journalPath)) { try { var receipt=JsonUtility.FromJson<StageClearReceipt>(File.ReadAllText(journalPath)); if(receipt!=null && receipt.stage>=1 && receipt.stage<=6 && !string.IsNullOrEmpty(receipt.id)) Complete(receipt.id,receipt.stage); } catch(Exception e) when(e is IOException || e is UnauthorizedAccessException || e is ArgumentException) { ReadProblem=true; } }
        }
        PlayerProfile Read(string file)
        {
            if (!File.Exists(file)) return null;
            try
            {
                var p = JsonUtility.FromJson<PlayerProfile>(File.ReadAllText(file));
                if (p == null || p.version != 1 || p.unlocked == null || p.cleared == null || p.receipts == null || p.preparation == null) throw new InvalidDataException();
                var ids = new HashSet<string>();
                p.unlocked.RemoveAll(id => catalog.Find(id) == null || !ids.Add(id));
                foreach(string id in new[] { "pistol", "medkit" }) if(ids.Add(id)) p.unlocked.Add(id);
                if (!ValidPreparation(p.preparation, ids)) { p.preparation = new LoadoutSnapshot { active="medkit", charges=catalog.Find("medkit").maxCharges }; ReadProblem=true; }
                ids.Clear(); foreach (var receipt in p.receipts) if (receipt == null || receipt.stage < 1 || receipt.stage > 6 || string.IsNullOrEmpty(receipt.id) || !ids.Add(receipt.id)) throw new InvalidDataException();
                foreach (int stage in p.cleared) if (stage < 1 || stage > 6) throw new InvalidDataException();
                return p;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            {
                ReadProblem = true;
                try { File.Copy(file, file + ".corrupt-" + DateTime.UtcNow.Ticks, false); } catch (IOException) { } catch (UnauthorizedAccessException) { }
                return null;
            }
        }
        bool ValidPreparation(LoadoutSnapshot s, HashSet<string> owned)
        {
            if (s == null || s.passives == null || s.passives.Count > 6) return false;
            if (!string.IsNullOrEmpty(s.special) && (!owned.Contains(s.special) || s.special == "pistol" || catalog.Find(s.special)?.kind != ItemKind.Weapon)) return false;
            if (!string.IsNullOrEmpty(s.active) && (!owned.Contains(s.active) || catalog.Find(s.active)?.kind != ItemKind.Active)) return false;
            var seen = new HashSet<string>();
            foreach (var p in s.passives) if (p == null || p.layers != 1 || !owned.Contains(p.id) || catalog.Find(p.id)?.kind != ItemKind.Passive || !seen.Add(p.id)) return false;
            return new LoadoutState(catalog).RestoreSnapshot(s);
        }
        public bool Owns(string id) => Data.unlocked.Contains(id);
        public bool IsClear(int stage) => Data.cleared.Contains(stage);
        public bool HasPending(int stage) => Data.receipts.Exists(r => r.stage == stage && !r.chosen);
        public StageClearReceipt Pending(int stage) => Data.receipts.Find(r => r.stage == stage && !r.chosen);
        public bool AllCollected(int stage)
        {
            if (!Owns(StageRewards.Signature[stage - 1])) return false;
            foreach (var id in StageRewards.Pools[stage - 1]) if (!Owns(id)) return false;
            return true;
        }
        PlayerProfile Clone() => JsonUtility.FromJson<PlayerProfile>(JsonUtility.ToJson(Data));
        bool Commit(PlayerProfile copy)
        {
            if (!writer(path, JsonUtility.ToJson(copy, true))) { WriteProblem = true; return false; }
            Data = copy; WriteProblem = false; return true;
        }
        public LoadoutState Prepared()
        {
            var gear = new LoadoutState(catalog);
            if (!gear.RestoreSnapshot(Data.preparation)) gear.Equip("medkit");
            gear.Restore(); return gear;
        }
        public bool SavePreparation(LoadoutState gear)
        {
            if (gear == null) return false;
            var s = gear.Snapshot(); s.energy = 100; s.cooldown = 0;
            s.charges = string.IsNullOrEmpty(s.active) ? 0 : catalog.Find(s.active).maxCharges;
            if (!ValidPreparation(s, new HashSet<string>(Data.unlocked))) return false;
            var copy = Clone(); copy.preparation = s; return Commit(copy);
        }
        public bool Complete(string clearId, int stage)
        {
            if (string.IsNullOrEmpty(clearId) || stage < 1 || stage > 6) return false;
            var existing = Data.receipts.Find(r => r.id == clearId);
            if (existing != null) { if(existing.stage==stage) { try { File.Delete(journalPath); } catch(IOException) { } catch(UnauthorizedAccessException) { } return true; } return false; }
            // A separate, recoverable receipt survives a failed profile replacement.
            AtomicWrite(journalPath,JsonUtility.ToJson(new StageClearReceipt { id=clearId,stage=stage }));
            var copy = Clone(); if (!copy.cleared.Contains(stage)) copy.cleared.Add(stage);
            string signature = StageRewards.Signature[stage - 1];
            if (!copy.unlocked.Contains(signature)) copy.unlocked.Add(signature);
            bool done = true; foreach (var id in StageRewards.Pools[stage - 1]) if (!copy.unlocked.Contains(id)) done = false;
            copy.receipts.Add(new StageClearReceipt { id = clearId, stage = stage, chosen = done });
            bool saved=Commit(copy); if(saved) { try { File.Delete(journalPath); } catch(IOException) { } catch(UnauthorizedAccessException) { } } return saved;
        }
        public List<string> Choices(string clearId)
        {
            var result = new List<string>(); var r = Data.receipts.Find(x => x.id == clearId);
            if (r == null || r.chosen) return result;
            foreach (var id in StageRewards.Pools[r.stage - 1]) if (!Owns(id) && result.Count < 3) result.Add(id);
            return result;
        }
        public bool Choose(string clearId, string itemId)
        {
            var r = Data.receipts.Find(x => x.id == clearId);
            if (r == null) return false; if(r.chosen) return r.selected == itemId;
            var choices = Choices(clearId);
            if (choices.Count > 0 && !choices.Contains(itemId) || choices.Count == 0 && itemId != null) return false;
            var copy = Clone(); var receipt = copy.receipts.Find(x => x.id == clearId);
            if (itemId != null && !copy.unlocked.Contains(itemId)) copy.unlocked.Add(itemId);
            receipt.chosen = true; receipt.selected = itemId; return Commit(copy);
        }
        static bool AtomicWrite(string file, string json)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file));
                using (var stream = new FileStream(file + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
                using (var output = new StreamWriter(stream)) { output.Write(json); output.Flush(); stream.Flush(true); }
                if (File.Exists(file)) File.Replace(file + ".tmp", file, file + ".bak"); else File.Move(file + ".tmp", file);
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException) { return false; }
        }
    }
}
