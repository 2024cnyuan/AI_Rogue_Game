using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Starfall.Tests
{
    public sealed class AdventureRulesTests
    {
        string directory;
        [SetUp] public void Setup() { directory = Path.Combine(Path.GetTempPath(), "StarfallRecords-" + Guid.NewGuid()); }
        [TearDown] public void Cleanup() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        static BestRecord Attempt(long time, string id = null, string timing = "1") => new BestRecord { stageId = "gardens", attemptId = id ?? Guid.NewGuid().ToString(), milliseconds = time, timingVersion = timing, seed = 42, date = "2026-10-02 UTC", equipment = new[] { "pistol" } };
        [Test] public void TimerUsesMonotonicTimeAndOverlappingPauseReasonsOnlyOnce()
        {
            double clock = 10; var timer = new LevelTimer(() => clock); var pause = new PauseState(); timer.Start();
            clock += 1.000999; Assert.AreEqual(1000, timer.Milliseconds);
            pause.Set(PauseReason.Menu, true); timer.Exclude(pause.IsPaused); clock += 10;
            pause.Set(PauseReason.Map, true); timer.Exclude(pause.IsPaused); clock += 20;
            pause.Set(PauseReason.Menu, false); timer.Exclude(pause.IsPaused); clock += 30;
            Assert.AreEqual(1000, timer.Milliseconds); pause.Set(PauseReason.Map, false); timer.Exclude(pause.IsPaused);
            clock += 2; Assert.AreEqual(3000, timer.Stop()); clock += 500; Assert.AreEqual(3000, timer.Milliseconds);
            Assert.AreEqual("123:04.567", LevelTimer.Format(7384567));
        }
        [Test] public void OrdinaryDecisionsCountWhileExplicitLoadingDoesNot()
        {
            double clock = 0; var timer = new LevelTimer(() => clock); timer.Exclude(true); clock = 10; timer.Start(); clock = 20;
            Assert.AreEqual(0, timer.Milliseconds); timer.Exclude(false); clock = 25; Assert.AreEqual(5000, timer.Milliseconds);
            // A reward/shop's combat pause is not an excluded timing reason.
            clock = 35; Assert.AreEqual(15000, timer.Stop());
        }
        [TestCase(9999, ClearFeedback.Improved)] [TestCase(10000, ClearFeedback.Matched)]
        [TestCase(11000, ClearFeedback.Close)] [TestCase(11001, ClearFeedback.Cleared)]
        public void FeedbackUsesQuantizedValuesAndInclusiveCloseBoundary(long time, ClearFeedback expected) => Assert.AreEqual(expected, PersonalBestStore.Compare(time, 10000));
        [Test] public void CloseThresholdHasOneSecondFloorAndTenSecondCap()
        {
            Assert.AreEqual(ClearFeedback.First, PersonalBestStore.Compare(100, null));
            Assert.AreEqual(ClearFeedback.Close, PersonalBestStore.Compare(102000, 100000));
            Assert.AreEqual(ClearFeedback.Close, PersonalBestStore.Compare(1010000, 1000000));
            Assert.AreEqual(ClearFeedback.Cleared, PersonalBestStore.Compare(1010001, 1000000));
        }
        [Test] public void SavesPersistOldComparisonAndDeduplicateAttemptsAcrossReload()
        {
            var store = new PersonalBestStore(directory); var first = store.Prepare(Attempt(10000, "first"), true, true);
            Assert.IsTrue(store.Commit(first)); Assert.IsTrue(first.Saved); Assert.AreEqual(ClearFeedback.First, first.Feedback);
            var second = store.Prepare(Attempt(8000, "second"), true, true); Assert.AreEqual(10000, second.Previous); Assert.AreEqual(2000, second.Delta);
            Assert.IsTrue(store.Commit(second)); Assert.AreEqual(10000, second.Previous);
            var reload = new PersonalBestStore(directory); Assert.AreEqual(8000, reload.Find().milliseconds);
            Assert.IsTrue(reload.Commit(reload.Prepare(Attempt(1, "second"), true, true))); Assert.AreEqual(8000, reload.Find().milliseconds); Assert.AreEqual(2, reload.Book.receipts.Count);
        }
        [Test] public void FailedWritesRetainRetryableResultAndNeverClaimSaved()
        {
            bool writable = false; var store = new PersonalBestStore(directory, (file, body) => writable);
            var result = store.Prepare(Attempt(6000), true, true); Assert.IsFalse(store.Commit(result)); Assert.IsFalse(result.Saved); Assert.IsNull(store.Find());
            writable = true; Assert.IsTrue(store.Commit(result)); Assert.IsTrue(result.Saved); Assert.AreEqual(6000, store.Find().milliseconds);
        }
        [Test] public void FailuresAndPracticeCannotOverwriteAnyPersonalBest()
        {
            var store = new PersonalBestStore(directory); store.Commit(store.Prepare(Attempt(10000), true, true));
            var failed = store.Prepare(Attempt(1), false, true); Assert.AreEqual(ClearFeedback.Failed, failed.Feedback); Assert.IsFalse(store.Commit(failed));
            var practice = store.Prepare(Attempt(1), true, false); Assert.AreEqual(ClearFeedback.Practice, practice.Feedback); Assert.IsFalse(store.Commit(practice));
            Assert.AreEqual(10000, store.Find().milliseconds);
        }
        [Test] public void ChangedRulesKeepOldRecordsWithoutComparingThem()
        {
            var store = new PersonalBestStore(directory); store.Commit(store.Prepare(Attempt(5000, "v1"), true, true));
            var next = store.Prepare(Attempt(9000, "v2", "2"), true, true); Assert.IsNull(next.Previous); Assert.IsTrue(store.Commit(next));
            var reload = new PersonalBestStore(directory); Assert.AreEqual(5000, reload.Find().milliseconds); Assert.AreEqual(9000, reload.Find("gardens", "2").milliseconds); Assert.AreEqual(2, reload.Book.bests.Count);
        }
        [Test] public void DamagedRecordFileRecoversBackupAndPreservesOriginal()
        {
            var store = new PersonalBestStore(directory); store.Commit(store.Prepare(Attempt(12000), true, true)); store.Commit(store.Prepare(Attempt(9000), true, true));
            File.WriteAllText(Path.Combine(directory, "starfall-records.json"), "broken"); var recovery = new PersonalBestStore(directory);
            Assert.IsTrue(recovery.ReadProblem); Assert.AreEqual(12000, recovery.Find().milliseconds); Assert.AreEqual(1, Directory.GetFiles(directory, "*.corrupt-*").Length);
        }
        [Test] public void BeaconAndRewardLedgerRejectsPrematureAndDuplicateActions()
        {
            var state = new FirstLevelProgress(); Assert.IsFalse(state.Activate("north")); Assert.IsFalse(state.CanEnterBoss);
            Assert.IsTrue(state.Clear("north")); Assert.IsTrue(state.Activate("north")); Assert.IsFalse(state.Activate("north"));
            state.Clear("south"); state.Activate("south"); Assert.IsTrue(state.CanEnterBoss);
            Assert.IsTrue(state.Claim("supply")); Assert.IsFalse(state.Claim("supply")); Assert.IsFalse(state.Clear("north"));
        }
        [Test] public void LegacySettingsGainSafeAudioDefaultsWithoutLosingLanguage()
        {
            Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, "starfall-settings.json"), "{\"version\":1,\"language\":\"zh-CN\",\"languageSelected\":true,\"tutorialCompleted\":true}");
            var settings = new SettingsStore(directory).Load("en"); Assert.AreEqual("zh-CN", settings.language); Assert.IsTrue(settings.tutorialCompleted); Assert.Greater(settings.masterVolume, 0); Assert.Greater(settings.effectsVolume, 0);
        }
        [Test] public void OneHundredSeedsHaveConnectedLegalSpawnsAndReachableObjectives()
        {
            for (int seed = 0; seed < 100; seed++)
            {
                var plan = new FirstLevelPlan(seed); Assert.AreEqual(6, plan.Rooms.Count);
                foreach (var room in plan.Rooms)
                {
                    var reached = new HashSet<Vector2Int>(); var queue = new Queue<Vector2Int>(); var start = new Vector2Int(-8, -4); queue.Enqueue(start); reached.Add(start);
                    while (queue.Count > 0)
                    {
                        var at = queue.Dequeue(); foreach (var direction in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                        {
                            var next = at + direction; if (!FirstLevelPlan.IsClear(room, next, .45f) || !reached.Add(next)) continue; queue.Enqueue(next);
                        }
                    }
                    foreach (var point in room.Enemies) Assert.IsTrue(reached.Contains(Vector2Int.RoundToInt(point)), "Seed " + seed + "/" + room.Id + ": enemy unreachable");
                    Assert.IsTrue(reached.Contains(new Vector2Int(10, 0)), "Exit unreachable " + room.Id);
                    Assert.IsTrue(reached.Contains(new Vector2Int(7, 2)), "Beacon unreachable " + room.Id);
                    Assert.IsTrue(reached.Contains(new Vector2Int(7, 3)), "Relay unreachable " + room.Id);
                    if (room.Next != null) Assert.IsNotNull(plan.Find(room.Next));
                    foreach (var point in room.Enemies) Assert.Greater(Vector2.Distance(start, point), 1.2f);
                }
            }
        }
    }
}
