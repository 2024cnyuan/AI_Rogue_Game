#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Text = TMPro.TextMeshProUGUI;

namespace Starfall.Tests
{
    public sealed class AdventurePlayTests
    {
        StarfallGame game;
        Keyboard keyboard;
        Mouse mouse;
        string temporary;
        InputSettings.BackgroundBehavior background;
        InputSettings.EditorInputBehaviorInPlayMode editorInput;
        [UnitySetUp] public IEnumerator Setup()
        {
            temporary = Path.Combine(Path.GetTempPath(), "StarfallM2b-" + Guid.NewGuid());
            background = InputSystem.settings.backgroundBehavior; editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            StarfallGame.EditorSettingsDirectory = temporary; keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
            SceneManager.LoadScene("Assets/_Game/Scenes/Boot.unity"); yield return null;
            game = UnityEngine.Object.FindFirstObjectByType<StarfallGame>(); game.ConfirmLanguage(); game.SetLanguage("en"); yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (game != null) UnityEngine.Object.Destroy(game.gameObject);
            InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(mouse); StarfallGame.EditorSettingsDirectory = null;
            InputSystem.settings.backgroundBehavior = background; InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
            Time.timeScale = 1; yield return null; if (Directory.Exists(temporary)) Directory.Delete(temporary, true);
            StarfallGame.EditorAdventureClock = null;
        }
        void Place(Vector2 at) { game.Player.Body.position = at; game.Player.Flush(); Physics2D.SyncTransforms(); }
        IEnumerator Press(Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); yield return null; yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null; yield return null;
        }
        IEnumerator Interact(string id)
        {
            PracticeInteractable station = null;
            foreach (var candidate in UnityEngine.Object.FindObjectsByType<PracticeInteractable>(FindObjectsSortMode.None)) if (candidate.Id == id) station = candidate;
            Assert.IsNotNull(station, id); Place(station.transform.position); yield return Press(station.IsPickup ? Key.E : Key.F);
        }
        IEnumerator ClearRoom()
        {
            foreach (var enemy in game.Enemies) if (enemy.Alive) enemy.Health.Receive(new DamageContext(1000, Faction.Player));
            yield return null; yield return null;
        }
        IEnumerator ReachBoss(bool practice = true)
        {
            if(practice) game.Context.InvalidateRecord(); Place(new Vector2(-5.7f,-4)); yield return null; yield return null;
            foreach(string id in new[]{"courtyard","north","crossing","south"}) {
                Assert.AreEqual(id,game.Adventure.Current.Id); yield return ClearRoom();
                if(id=="north" || id=="south") yield return Interact("beacon."+id);
                yield return Interact("route.next");
            }
            Assert.AreEqual("boss",game.Adventure.Current.Id); Assert.IsNotNull(game.Adventure.Boss);
        }
        IEnumerator WalkTo(Vector2 destination)
        {
            float deadline = Time.realtimeSinceStartup + 15;
            while (Vector2.Distance(game.Player.Body.position, destination) > .28f && Time.realtimeSinceStartup < deadline)
            {
                Assert.IsTrue(game.Player.Health.State.Alive, "Bot died while travelling");
                game.Room.RefreshNavigation(destination); QueueMovement(game.Room.PathDirection(game.Player.Body.position, destination), false, false);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = game.GameCamera.WorldToScreenPoint(destination) }); yield return null;
            }
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
            Assert.Less(Vector2.Distance(game.Player.Body.position, destination), .4f, "Normal input could not reach " + destination + " in " + game.Adventure.Current.Id);
        }
        void QueueMovement(Vector2 direction, bool dodge, bool active)
        {
            var keys = new List<Key>(); if (direction.x > .2f) keys.Add(Key.D); if (direction.x < -.2f) keys.Add(Key.A);
            if (direction.y > .2f) keys.Add(Key.W); if (direction.y < -.2f) keys.Add(Key.S); if (dodge) keys.Add(Key.Space); if (active) keys.Add(Key.Q);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys.ToArray()));
        }
        IEnumerator FightUsingInput()
        {
            Vector2[] circuit = { new Vector2(8, -5.5f), new Vector2(8, 5.5f), new Vector2(-8, 5.5f), new Vector2(-8, -5.5f) };
            int waypoint = 0; float started = Time.realtimeSinceStartup, dodgeAt = -10;
            while (game.LivingEnemies > 0 || game.Adventure.Boss != null && game.Adventure.Boss.Health.State.Alive)
            {
                Assert.IsTrue(game.Player.Health.State.Alive, "Normal equipment bot died in " + game.Adventure.Current.Id);
                Assert.Less(Time.realtimeSinceStartup - started, 50, "Normal input fight stalled in " + game.Adventure.Current.Id);
                Vector2 target = game.Adventure.Boss != null ? game.Adventure.Boss.transform.position : Vector2.zero; float score = float.MaxValue;
                foreach (var enemy in game.Enemies)
                {
                    if (!enemy.Alive) continue; Vector2 delta = (Vector2)enemy.transform.position - game.Player.Body.position;
                    float candidate = delta.magnitude + (Physics2D.Raycast(game.Player.Body.position, delta.normalized, delta.magnitude, 1) ? 30 : 0);
                    if (candidate < score) { score = candidate; target = enemy.transform.position; }
                }
                if (Vector2.Distance(game.Player.Body.position, circuit[waypoint]) < .65f) waypoint = (waypoint + 1) % circuit.Length;
                game.Room.RefreshNavigation(circuit[waypoint]); Vector2 direction = game.Room.PathDirection(game.Player.Body.position, circuit[waypoint]);
                bool dodge = Time.realtimeSinceStartup - dodgeAt > 1.4f; if (dodge) dodgeAt = Time.realtimeSinceStartup;
                QueueMovement(direction, dodge, game.Player.Health.State.Health <= 65 && game.Loadout.ActiveCooldown <= 0);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = game.GameCamera.WorldToScreenPoint(target), buttons = 1 }); yield return null;
            }
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.QueueStateEvent(mouse, new MouseState()); yield return null; yield return null;
        }
        [UnityTest] [Timeout(240000)] public IEnumerator NormalEquipmentInputRunCompletesStageAndPersistsEligibleRecord()
        {
            game.StartStage(1); yield return null; Assert.AreEqual("medkit",game.Loadout.Active); yield return WalkTo(new Vector2(-5.7f,-4));
            foreach(string id in new[]{"courtyard","north","crossing","south"}) {
                Assert.AreEqual(id,game.Adventure.Current.Id); yield return FightUsingInput();
                if(id=="north" || id=="south") { yield return WalkTo(new Vector2(7,2)); yield return Press(Key.F); }
                if(id=="south") { yield return WalkTo(new Vector2(-6,-4)); yield return Press(Key.F); }
                yield return WalkTo(game.Room.Exit); yield return Press(Key.F);
            }
            yield return FightUsingInput(); Assert.IsTrue(game.Adventure.Result.Success); Assert.IsTrue(game.Context.RecordEligible); Assert.IsTrue(game.Adventure.Result.Saved); Assert.IsTrue(game.Profile.Owns("shotgun"));
            Assert.IsNotNull(new PersonalBestStore(temporary).Find("gardens",game.LevelConfig.timingVersion,game.LevelConfig.balanceVersion,"stage_select")); Capture("m5-basic-first-clear-en",1920,1080);
        }
        [UnityTest] public IEnumerator FirstLevelTasksRoomTransitionsAndSettlementAreAtomic()
        {
            game.StartStage(1); yield return null; Assert.AreEqual(0,game.LivingEnemies); Assert.IsFalse(game.Adventure.Progress.IsClear("courtyard")); Assert.IsFalse(game.Adventure.Travel("north"));
            Place(new Vector2(-5.7f,-4)); yield return null; yield return null; Assert.Greater(game.LivingEnemies,0); yield return ClearRoom(); yield return Interact("courtyard.chest");
            var chest=UnityEngine.Object.FindFirstObjectByType<PracticeInteractable>(); yield return Interact("supply.shotgun"); Assert.AreEqual("shotgun",game.Loadout.SpecialWeapon); Assert.IsTrue(game.Adventure.Progress.IsClaimed("courtyard.weapon"));
            yield return Interact("route.next"); Assert.AreEqual("north",game.Adventure.Current.Id); yield return ClearRoom(); yield return Interact("beacon.north"); Assert.AreEqual(1,game.Adventure.Progress.Beacons); yield return Interact("route.back");
            Assert.AreEqual("courtyard",game.Adventure.Current.Id); Assert.AreEqual(0,game.LivingEnemies); Assert.Greater(game.Player.Body.position.x,8); Assert.IsTrue(game.Adventure.Progress.IsClaimed("courtyard.weapon"));
            game.RestartMode(); yield return null; Assert.AreEqual("courtyard",game.Adventure.Current.Id); Assert.IsNull(game.Loadout.SpecialWeapon); Assert.AreEqual(0,game.Context.Coins); Assert.AreEqual(2,game.Profile.Data.unlocked.Count);
        }
        [UnityTest] public IEnumerator BossExecutesThreeMovesFreezesOnPauseAndDeathWinsSameFrame()
        {
            game.StartAdventure(); yield return null; yield return ReachBoss(); Place(new Vector2(-8, -5));
            var boss = game.Adventure.Boss; var moves = new HashSet<CaptainMove>();
            for (float time = 0; time < 8.5f; time += .1f) { moves.Add(boss.Move); yield return new WaitForSeconds(.1f); }
            Assert.AreEqual(3, moves.Count); Assert.Greater(game.Projectiles.ActiveCount, 0);
            var position = boss.transform.position; var move = boss.Move; long elapsed = game.Adventure.Timer.Milliseconds;
            game.SetPause(PauseReason.Menu, true); game.SetPause(PauseReason.Map, true); yield return new WaitForSecondsRealtime(.2f);
            Assert.AreEqual(position, boss.transform.position); Assert.AreEqual(move, boss.Move); Assert.That(game.Adventure.Timer.Milliseconds, Is.InRange(elapsed, elapsed + 5));
            game.SetPause(PauseReason.Menu, false); Assert.AreEqual(0, Time.timeScale); game.SetPause(PauseReason.Map, false);
            game.Player.Health.State.Restore(); game.Player.Health.Receive(new DamageContext(1000, Faction.Enemy)); boss.Health.Receive(new DamageContext(1000, Faction.Player)); yield return null;
            Assert.AreEqual(RunPhase.Dead, game.Context.Phase); Assert.AreEqual(ClearFeedback.Failed, game.Adventure.Result.Feedback); Assert.IsNull(game.Records.Find()); Assert.IsEmpty(game.Adventure.Rewards);
            Assert.AreEqual(0, game.Projectiles.ActiveCount); Capture("failure-en", 1280, 720);
        }
        [UnityTest] public IEnumerator ChallengeCanBeAbandonedAndDoesNotBlockMainRoute()
        {
            game.StartStage(1); yield return null; Place(new Vector2(-5.7f,-4)); yield return null; yield return ClearRoom(); yield return Interact("route.branch");
            Assert.AreEqual("challenge",game.Adventure.Current.Id); Assert.IsTrue(game.Adventure.Travel("courtyard")); yield return null; Assert.IsTrue(game.Adventure.ChallengeFailed); Assert.Less(Vector2.Distance(game.Player.Body.position,new Vector2(0,-4)),.1f);
            yield return Interact("route.branch"); Assert.AreEqual(0,game.LivingEnemies); Assert.IsFalse(game.Adventure.Progress.IsClaimed("challenge.reward")); Assert.IsTrue(game.Adventure.Travel("courtyard")); Assert.IsTrue(game.Adventure.Travel("north"));
        }
        [UnityTest] public IEnumerator FeedbackAndRecordsScreensAreBilingualAcrossResolutions()
        {
            game.StartAdventure(); yield return null; yield return ReachBoss(); game.Adventure.Boss.Health.Receive(new DamageContext(1000, Faction.Player)); yield return null;
            var result = game.Adventure.Result;
            foreach (string language in new[] { "zh-CN", "en" })
            {
                game.SetLanguage(language);
                foreach (ClearFeedback feedback in new[] { ClearFeedback.First, ClearFeedback.Improved, ClearFeedback.Matched, ClearFeedback.Close, ClearFeedback.Cleared, ClearFeedback.Failed })
                {
                    // UI branch fixture only; never committed as a real personal best.
                    result.Feedback = feedback; result.Success = feedback != ClearFeedback.Failed; result.Previous = feedback == ClearFeedback.First ? (long?)null : 120000;
                    result.Attempt.milliseconds = feedback == ClearFeedback.Improved ? 118000 : feedback == ClearFeedback.Matched ? 120000 : 121000;
                    result.Eligible = true; result.Saved = true;
                    Capture("feedback-" + feedback + "-" + language, 1280, 720);
                }
                result.Success = true; result.Saved = false; Capture("save-failed-" + language, 1280, 720); result.Saved = true;
                game.AdventureUI.OpenRecords(); yield return null;
                foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(2560, 1440), new Vector2Int(1280, 960) }) Capture("records-empty-" + language + "-" + size.x + "x" + size.y, size.x, size.y);
                game.AdventureUI.CloseRecords(); result.Success = true; result.Feedback = ClearFeedback.First;
                foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(2560, 1440), new Vector2Int(1280, 960) }) Capture("result-" + language + "-" + size.x + "x" + size.y, size.x, size.y);
                game.ReturnToMenu(); game.StartAdventure(); yield return null;
                Capture("entry-" + language, 1280, 720); yield return Press(Key.Tab); Capture("route-map-" + language, 1280, 720); yield return Press(Key.Tab);
                game.Interface.OpenSettings(); yield return null; Capture("settings-" + language, 1280, 720); game.Interface.CloseSettings();
                yield return ReachBoss(); game.Adventure.Boss.Health.Receive(new DamageContext(1000, Faction.Player)); yield return null; result = game.Adventure.Result;
            }
        }
        [UnityTest] public IEnumerator SettingsAndPracticeDoNotAlterSavedRecordsOrFormalQualification()
        {
            var fixture = new BestRecord { stageId = "gardens", attemptId = "test-fixture", milliseconds = 123456, seed = 100, date = "2026-10-02 UTC", equipment = new[] { "shotgun", "medkit", "rapid:2" } };
            game.Records.Commit(game.Records.Prepare(fixture, true, true));
            game.AdventureUI.OpenRecords(); yield return null; Capture("records-filled-en", 1280, 720); game.SetLanguage("zh-CN"); Capture("records-filled-zh-CN", 1280, 720); game.AdventureUI.CloseRecords();
            game.StartTutorial(); yield return null; game.Tutorial.SkipAll(); game.StartTraining(); yield return null; game.Training.Reset(); game.ReturnToMenu();
            Assert.AreEqual(123456, new PersonalBestStore(temporary).Find().milliseconds);
            game.StartAdventure(); yield return null; long elapsed = game.Adventure.Timer.Milliseconds;
            game.Interface.OpenSettings(); yield return new WaitForSecondsRealtime(.15f); game.SetLanguage("en"); game.ToggleTimer(); game.ToggleFlash(); game.SetVolume("effects", 0); game.SetVolume("shake", 0);
            Assert.That(game.Adventure.Timer.Milliseconds, Is.InRange(elapsed, elapsed + 5)); Assert.IsTrue(game.Context.RecordEligible);
            game.Interface.CloseSettings(); game.EquipItem("smg"); Assert.IsFalse(game.Context.RecordEligible); game.Loadout.Invincible = false; Assert.IsFalse(game.Context.RecordEligible);
            var saved = new SettingsStore(temporary).Load("en"); Assert.IsTrue(saved.hideTimer); Assert.IsTrue(saved.reduceFlash); Assert.AreEqual(0, saved.effectsVolume); Assert.AreEqual(0, saved.shake);
        }
        [UnityTest] public IEnumerator UnsavedResultSurvivesMenuAndRetriesExactlyOnce()
        {
            game.StartAdventure(); yield return null;
            // Persistence branch fixture: explicit damage/position injection, isolated temporary scores.
            yield return ReachBoss(false);
            Directory.CreateDirectory(Path.Combine(temporary, "starfall-records.json.tmp"));
            game.Adventure.Boss.Health.Receive(new DamageContext(1000, Faction.Player)); yield return null;
            var result = game.Adventure.Result; Assert.IsFalse(result.Saved); Assert.IsNull(game.Records.Find()); Assert.AreEqual(1, game.PendingRecords.Count);
            Capture("actual-save-failure-en", 1280, 720); game.ReturnToMenu(); game.AdventureUI.OpenRecords(); yield return null;
            Capture("pending-record-en", 1280, 720); Assert.AreSame(result, game.PendingRecords[0]);
            Directory.Delete(Path.Combine(temporary, "starfall-records.json.tmp")); game.RetryRecordSaves();
            Assert.IsTrue(result.Saved); Assert.IsEmpty(game.PendingRecords); Assert.AreEqual(result.Attempt.milliseconds, new PersonalBestStore(temporary).Find("gardens",game.LevelConfig.timingVersion,game.LevelConfig.balanceVersion,"stage_select").milliseconds);
            game.RetryRecordSaves(); Assert.AreEqual(1, game.Records.Book.receipts.Count);
        }
        void Capture(string name, int width, int height)
        {
            Assert.AreNotEqual(UnityEngine.Rendering.GraphicsDeviceType.Null, SystemInfo.graphicsDeviceType);
            string directory = Path.Combine(Application.dataPath, "../../appendix/M5/screens"); Directory.CreateDirectory(directory);
            var camera = game.GameCamera; var canvas = game.Interface.Canvas; var target = new RenderTexture(width, height, 24);
            var scaler = canvas.GetComponent<CanvasScaler>(); float oldScale = canvas.scaleFactor, oldSize = camera.orthographicSize;
            var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active; var oldMode = canvas.renderMode;
            camera.targetTexture = target; canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 5;
            scaler.enabled = false; canvas.scaleFactor = Mathf.Min(width / 1280f, height / 720f); camera.orthographicSize = Mathf.Max(9.3f, 13.4f / ((float)width / height));
            game.Interface.RefreshNow(); game.ModeUI.RefreshNow(); game.AdventureUI.RefreshNow(); Canvas.ForceUpdateCanvases();
            foreach (var view in game.Interface.GetComponentsInChildren<Text>())
            {
                if (!view.gameObject.activeInHierarchy || string.IsNullOrEmpty(view.text)) continue;
                Assert.LessOrEqual(view.preferredHeight, view.rectTransform.rect.height + 3, "Text overflow: " + view.name + " / " + view.text);
                Assert.IsFalse(view.text.Contains("{"), "Unformatted: " + view.text);
                foreach (char character in view.text) if (character > 127 && !char.IsWhiteSpace(character)) Assert.IsTrue(view.font.HasCharacter(character,true,true), "Missing glyph: " + character);
            }
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera, new UnityEngine.Rendering.RenderPipeline.StandardRequest { destination = target }); RenderTexture.active = target;
            var screenshot = new Texture2D(width, height, TextureFormat.RGB24, false); screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0); screenshot.Apply();
            File.WriteAllBytes(Path.Combine(directory, name + ".png"), screenshot.EncodeToPNG());
            camera.targetTexture = oldTarget; RenderTexture.active = oldActive; canvas.renderMode = oldMode; canvas.scaleFactor = oldScale; scaler.enabled = true; camera.orthographicSize = oldSize;
            target.Release(); UnityEngine.Object.Destroy(target); UnityEngine.Object.Destroy(screenshot); Canvas.ForceUpdateCanvases();
        }
    }
}
#endif
