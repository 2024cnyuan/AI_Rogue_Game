using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Starfall.Tests
{
    public sealed class CampaignRulesTests
    {
        string directory;
        ItemCatalog catalog;
        [SetUp] public void Setup() { directory = Path.Combine(Path.GetTempPath(), "StarfallM3Rules-" + Guid.NewGuid()); catalog = ItemCatalog.Defaults(); }
        [TearDown] public void Cleanup() { UnityEngine.Object.DestroyImmediate(catalog); if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        EntryCheckpoint Entry(int stage = 2) => new EntryCheckpoint { stage = stage, seed = 31415, runId = "run", coins = 45, health = 73 };
        [Test] public void SnapshotRestoresExactResourcesWithoutAidsOrMidStageRewards()
        {
            var gear = new LoadoutState(catalog); gear.Equip("shotgun"); gear.Equip("medkit"); gear.Equip("rapid"); gear.SpendEnergy(31);
            var health = new VitalState(100); health.Damage(50, 0, false, 0); Assert.IsNull(gear.TryUse(health, false));
            gear.InfiniteEnergy = gear.Invincible = true;
            var entry = Entry(); entry.loadout = gear.Snapshot(); var store = new CheckpointStore(directory); Assert.IsTrue(store.Save(entry));
            gear.Equip("vitality"); gear.AddEnergy(30); entry.coins = 999;
            var reload = new CheckpointStore(directory); var restored = new LoadoutState(catalog); Assert.IsTrue(restored.RestoreSnapshot(reload.Current.loadout));
            Assert.AreEqual(69, restored.Energy); Assert.AreEqual("shotgun", restored.Weapon); Assert.AreEqual(0, restored.Layers("vitality"));
            Assert.AreEqual(1, restored.Charges); Assert.AreEqual(8, restored.ActiveCooldown);
            Assert.IsFalse(restored.Invincible); Assert.IsFalse(restored.InfiniteEnergy); Assert.AreEqual(45, reload.Current.coins); Assert.AreEqual(73, reload.Current.health);
            var bad = restored.Snapshot(); bad.special = "missing"; Assert.IsFalse(restored.RestoreSnapshot(bad)); Assert.AreEqual("shotgun", restored.Weapon);
        }
        [Test] public void FailedCheckpointWriteKeepsPreviousEntryAndRetryCommitsOnce()
        {
            bool writable = true; var store = new CheckpointStore(directory, (p, s) => writable); Assert.IsTrue(store.Save(Entry(1)));
            writable = false; Assert.IsFalse(store.Save(Entry(2))); Assert.AreEqual(1, store.Current.stage); Assert.IsTrue(store.WriteProblem);
            writable = true; Assert.IsTrue(store.Save(Entry(2))); Assert.AreEqual(2, store.Current.stage); Assert.IsFalse(store.WriteProblem);
        }
        [Test] public void CorruptionRecoversBackupAndDeathTombstoneCannotReviveRun()
        {
            var store = new CheckpointStore(directory); store.Save(Entry(1)); store.Save(Entry(2));
            string file = Path.Combine(directory, "starfall-checkpoint.json"); File.WriteAllText(file, "broken");
            var recovered = new CheckpointStore(directory); Assert.IsTrue(recovered.ReadProblem); Assert.AreEqual(1, recovered.Current.stage);
            Assert.IsTrue(recovered.Clear()); File.WriteAllText(file, "broken again"); var reload = new CheckpointStore(directory);
            Assert.IsFalse(reload.HasEntry); Assert.AreEqual(2, Directory.GetFiles(directory, "*.corrupt-*").Length);
        }
        [Test] public void IncompatibleCheckpointIsPreservedAndMalformedResourcesAreRejected()
        {
            Directory.CreateDirectory(directory); string file = Path.Combine(directory, "starfall-checkpoint.json");
            var entry = Entry(); entry.schema = 99; File.WriteAllText(file, JsonUtility.ToJson(entry)); var store = new CheckpointStore(directory);
            Assert.IsFalse(store.HasEntry); Assert.IsTrue(store.ReadProblem); Assert.IsTrue(File.Exists(file)); Assert.AreEqual(1, Directory.GetFiles(directory, "*.corrupt-*").Length);
            entry = Entry(); entry.health = float.NaN; Assert.IsFalse(CheckpointStore.Valid(entry)); entry.health = 10; entry.coins = -1; Assert.IsFalse(CheckpointStore.Valid(entry));
        }
        [Test] public void ShopRejectsFailedFullDuplicateAndReentrantTransactionsWithoutCharging()
        {
            var context = new RunContext(GameMode.Adventure); context.BeginCombat(); context.AddCoins(50); var shop = new ShopLedger();
            Assert.IsFalse(shop.Buy("full", 15, context, () => false)); Assert.AreEqual(50, context.Coins);
            Assert.IsFalse(shop.Buy("expensive", 51, context, () => true)); Assert.AreEqual(50, context.Coins);
            int grants = 0;
            Assert.IsTrue(shop.Buy("item", 20, context, () => { grants++; Assert.IsFalse(shop.Buy("other", 10, context, () => true)); return true; }));
            Assert.IsFalse(shop.Buy("item", 20, context, () => { grants++; return true; })); Assert.AreEqual(30, context.Coins); Assert.AreEqual(1, grants);
        }
        [Test] public void ShopDecisionCountsWhileExplicitOverlappingPauseDoesNot()
        {
            double now = 0; var timer = new LevelTimer(() => now); var pause = new PauseState(); timer.Start();
            pause.Set(PauseReason.Shop, true); now = 10; Assert.AreEqual(10000, timer.Milliseconds);
            pause.Set(PauseReason.Menu, true); timer.Exclude((pause.Reasons & ~PauseReason.Shop) != 0); now = 20;
            pause.Set(PauseReason.Focus, true); pause.Set(PauseReason.Menu, false); timer.Exclude((pause.Reasons & ~PauseReason.Shop) != 0); now = 30;
            Assert.AreEqual(10000, timer.Milliseconds); pause.Set(PauseReason.Focus, false); timer.Exclude(false); now = 35; Assert.AreEqual(15000, timer.Milliseconds);
            var retry = new LevelTimer(() => now); retry.Start(); Assert.AreEqual(0, retry.Milliseconds);
        }
        [Test] public void StageRecordsRemainIndependentAfterLaterDeath()
        {
            var records = new PersonalBestStore(directory);
            foreach (string stage in new[] { "gardens", "workshop", "reservoir" })
                Assert.IsTrue(records.Commit(records.Prepare(new BestRecord { stageId = stage, attemptId = stage, milliseconds = 12345, seed = 7 }, true, true)));
            var failed = records.Prepare(new BestRecord { stageId = "reservoir", attemptId = "dead", milliseconds = 1 }, false, true); Assert.IsFalse(records.Commit(failed));
            var reload = new PersonalBestStore(directory); Assert.AreEqual(3, reload.Book.bests.Count); Assert.AreEqual(12345, reload.Find("workshop").milliseconds);
        }
        [Test] public void TwoThemesHaveFourDistinctLayoutsAndHundredReachableSeedsEach()
        {
            for (int stage = 2; stage <= 3; stage++) for (int seed = 0; seed < 100; seed++)
            {
                var plan = new FirstLevelPlan(seed, stage); var layouts = new HashSet<string>();
                foreach (string id in new[] { "courtyard", "north", "crossing", "south" }) layouts.Add(string.Join("/", Array.ConvertAll(plan.Find(id).Cover, rect => rect.ToString())));
                Assert.AreEqual(4, layouts.Count);
                var graph = new HashSet<string>(); var rooms = new Queue<string>(); rooms.Enqueue("entry");
                while (rooms.Count > 0)
                {
                    string id = rooms.Dequeue(); if (!graph.Add(id)) continue; var room = plan.Find(id); Assert.IsNotNull(room);
                    foreach (string next in new[] { room.Next, room.Back, room.Branch }) if (next != null) rooms.Enqueue(next);
                }
                Assert.AreEqual(plan.Rooms.Count, graph.Count, "Stage " + stage + " seed " + seed);
                foreach (var room in plan.Rooms)
                {
                    var reached = new HashSet<Vector2Int>(); var queue = new Queue<Vector2Int>(); queue.Enqueue(new Vector2Int(-8, -4));
                    while (queue.Count > 0)
                    {
                        var at = queue.Dequeue(); if (!reached.Add(at)) continue;
                        foreach (var direction in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                        { var next = at + direction; if (!reached.Contains(next) && FirstLevelPlan.IsClear(room, next, .45f)) queue.Enqueue(next); }
                    }
                    foreach (var point in room.Enemies) Assert.IsTrue(reached.Contains(Vector2Int.RoundToInt(point)), "Spawn: " + stage + "/" + seed + "/" + room.Id);
                    foreach (var target in new[] { new Vector2Int(10, 0), new Vector2Int(7, 2), new Vector2Int(7, 3), new Vector2Int(0, -5) }) Assert.IsTrue(reached.Contains(target), "Goal: " + stage + "/" + seed + "/" + room.Id + "/" + target);
                    foreach (var point in room.Enemies) Assert.Greater(Vector2.Distance(new Vector2(-8, -4), point), 1.2f);
                }
            }
        }
    }
}
