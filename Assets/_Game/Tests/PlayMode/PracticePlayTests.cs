#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
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
    public sealed class PracticePlayTests
    {
        StarfallGame game;
        Keyboard keyboard;
        Mouse mouse;
        string temporary;
        InputSettings.BackgroundBehavior background;
        InputSettings.EditorInputBehaviorInPlayMode editorInput;
        [UnitySetUp] public IEnumerator Setup()
        {
            temporary = Path.Combine(Path.GetTempPath(), "StarfallM2a-" + Guid.NewGuid());
            background = InputSystem.settings.backgroundBehavior; editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            StarfallGame.EditorSettingsDirectory = temporary; keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
            SceneManager.LoadScene("Assets/_Game/Scenes/Boot.unity"); yield return null;
            game = UnityEngine.Object.FindFirstObjectByType<StarfallGame>(); game.ConfirmLanguage(); yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (game != null) UnityEngine.Object.Destroy(game.gameObject);
            InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(mouse); StarfallGame.EditorSettingsDirectory = null;
            InputSystem.settings.backgroundBehavior = background; InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
            Time.timeScale = 1; yield return null; if (Directory.Exists(temporary)) Directory.Delete(temporary, true);
        }
        void Place(Vector2 at) { game.Player.Body.position = at; game.Player.Flush(); Physics2D.SyncTransforms(); }
        IEnumerator KeyPress(Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); yield return null; yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null; yield return null;
        }
        IEnumerator Aim(Vector2 at, bool fire = false)
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { position = game.GameCamera.WorldToScreenPoint(at), buttons = fire ? (ushort)1 : (ushort)0 });
            yield return null; yield return null;
        }
        PracticeTarget Target(string id)
        {
            foreach (var target in UnityEngine.Object.FindObjectsByType<PracticeTarget>(FindObjectsSortMode.None)) if (target.Id == id) return target;
            Assert.Fail("Missing target " + id); return null;
        }
        IEnumerator ReachTargets()
        {
            Place(new Vector2(-8, 1)); yield return Aim(new Vector2(-7, 4)); yield return null;
            Assert.AreEqual(TutorialStep.Targets, game.Tutorial.Step);
            Place(new Vector2(-7, 2)); yield return Aim(new Vector2(-7, 4), true); yield return new WaitForSeconds(.4f); yield return Aim(new Vector2(-7, 4));
            Assert.AreEqual(TutorialStep.Targets, game.Tutorial.Step, "The second target must be hit separately");
            Place(new Vector2(-2, 2)); yield return Aim(new Vector2(-2, 4), true); yield return new WaitForSeconds(.4f); yield return Aim(new Vector2(-2, 4));
            Assert.AreEqual(TutorialStep.Dodge, game.Tutorial.Step);
        }
        [UnityTest] public IEnumerator TutorialCanFinishWithRealInputAndProjectilesThenReplayCleanly()
        {
            game.SetLanguage("en"); game.StartTutorial(); yield return null; Capture("tutorial-move-en", 1280, 720);
            yield return ReachTargets(); Capture("tutorial-dodge-en", 1280, 720);
            Place(new Vector2(-4, 0)); yield return Aim(new Vector2(-4, 3));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space)); yield return null;
            Assert.IsTrue(game.Player.IsDodging);
            game.Projectiles.Spawn(game.Player.Body.position + Vector2.right * .42f, Vector2.left, Faction.Enemy, 5, 3.5f);
            yield return new WaitForSeconds(.1f); InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
            Assert.AreEqual(TutorialStep.Equipment, game.Tutorial.Step, "A real intercepted attack during dodge is required");
            Place(new Vector2(-4, -4)); yield return new WaitForSeconds(.1f);
            Place(new Vector2(-1, -4)); yield return KeyPress(Key.E); Assert.AreEqual("shotgun", game.Loadout.SpecialWeapon);
            yield return KeyPress(Key.Digit1); Assert.AreEqual("pistol", game.Loadout.Weapon);
            yield return KeyPress(Key.Digit2); Assert.AreEqual("shotgun", game.Loadout.Weapon);
            Place(new Vector2(-1, -3.2f)); yield return Aim(new Vector2(-1, -1.5f), true);
            yield return new WaitForSeconds(6); yield return Aim(new Vector2(-1, -1.5f));
            Assert.Less(game.Loadout.Energy, 12); Assert.AreEqual(TutorialStep.Equipment, game.Tutorial.Step);
            yield return KeyPress(Key.Digit1); Assert.AreEqual(TutorialStep.Tools, game.Tutorial.Step);
            Assert.Less(game.Player.Health.State.Health, game.Player.Health.State.Maximum); Capture("tutorial-tools-en", 1280, 720);
            Place(new Vector2(2, -4)); yield return KeyPress(Key.E); Assert.AreEqual("medkit", game.Loadout.Active);
            float initial = game.Player.Health.State.Health; yield return KeyPress(Key.Q); Assert.Greater(game.Player.Health.State.Health, initial);
            Place(new Vector2(4, -4)); yield return KeyPress(Key.E); Assert.AreEqual(TutorialStep.MapBeacon, game.Tutorial.Step);
            Assert.IsNotNull(game.Tutorial.PassiveComparison); Capture("tutorial-map-en", 1280, 720);
            Place(new Vector2(7, 3)); yield return KeyPress(Key.F); Assert.IsFalse(game.Room.DoorOpen, "Beacon must wait for a completed map visit");
            yield return KeyPress(Key.Tab); Assert.IsTrue(game.Pause.Has(PauseReason.Map));
            yield return KeyPress(Key.Tab); Assert.IsFalse(game.Pause.IsPaused);
            yield return KeyPress(Key.F); Assert.IsTrue(game.Room.DoorOpen);
            Place(game.Room.Exit); yield return KeyPress(Key.F); Assert.AreEqual(TutorialStep.Combat, game.Tutorial.Step); Assert.AreEqual(3, game.LivingEnemies);
            Place(new Vector2(5, -5)); Capture("tutorial-combat-en", 1280, 720);
            // Defeat each real enemy with pooled projectiles; key-driven firing was exercised above.
            var enemies = new System.Collections.Generic.List<PrototypeEnemy>(game.Enemies);
            foreach (var enemy in enemies)
            {
                while (enemy.Alive)
                {
                    game.Projectiles.Spawn((Vector2)enemy.transform.position + Vector2.left * .55f, Vector2.right, Faction.Player, 20, 18);
                    yield return new WaitForSeconds(.06f);
                }
            }
            Assert.AreEqual(0, game.LivingEnemies); Place(game.Room.Exit); yield return KeyPress(Key.F);
            Assert.AreEqual(TutorialStep.Complete, game.Tutorial.Step); Assert.IsTrue(game.Settings.tutorialCompleted); Assert.IsFalse(game.Settings.tutorialSkipped);
            Assert.IsFalse(game.Context.RecordEligible); Assert.AreEqual(0, game.Projectiles.ActiveCount); Capture("tutorial-complete-en", 1280, 720);
            game.SetLanguage("zh-CN"); Capture("tutorial-complete-zh-CN", 1280, 720);
            var saved = new SettingsStore(temporary).Load("en"); Assert.IsTrue(saved.tutorialCompleted); Assert.IsFalse(saved.tutorialSkipped);
            game.ReturnToMenu(); game.StartTutorial(); yield return null;
            Assert.AreEqual(TutorialStep.MoveAim, game.Tutorial.Step); Assert.IsNull(game.Loadout.SpecialWeapon); Assert.AreEqual(0, game.Context.Coins);
        }
        [UnityTest] public IEnumerator TutorialRetriesCurrentStepAndSkipIsSavedSeparately()
        {
            game.StartTutorial(); yield return null;
            Place(new Vector2(-8, 1)); yield return Aim(new Vector2(8, 1)); Assert.AreEqual(TutorialStep.MoveAim, game.Tutorial.Step, "Arrival alone cannot advance");
            yield return ReachTargets(); var tutorial = game.Tutorial;
            tutorial.SkipDodge(); Assert.AreEqual(TutorialStep.Dodge, tutorial.Step);
            game.Player.Health.Receive(new DamageContext(1000, Faction.Enemy)); yield return null;
            Assert.AreEqual(1, tutorial.RetryCount); Assert.AreEqual(TutorialStep.Dodge, tutorial.Step); Assert.AreEqual(100, game.Player.Health.State.Health);
            Assert.IsTrue(game.Hazards.Running); Assert.AreEqual(0, game.LivingEnemies);
            tutorial.Tick(26); tutorial.SkipDodge(); Assert.AreEqual(TutorialStep.Equipment, tutorial.Step);
            game.Projectiles.Spawn(Vector2.zero, Vector2.right, Faction.Enemy, 5, 2); tutorial.Retry();
            Assert.AreEqual(0, game.Projectiles.ActiveCount); Assert.AreEqual(2, UnityEngine.Object.FindObjectsByType<SupplyPickup>(FindObjectsSortMode.None).Length);
            tutorial.SkipAll(); yield return null; Assert.IsNull(game.Context);
            var saved = new SettingsStore(temporary).Load("en"); Assert.IsFalse(saved.tutorialCompleted); Assert.IsTrue(saved.tutorialSkipped);
            game.StartTutorial(); yield return null; Assert.AreEqual(TutorialStep.MoveAim, game.Tutorial.Step);
        }
        [UnityTest] public IEnumerator TrainingLimitsDeathResetAndModeIsolationAreReal()
        {
            Directory.CreateDirectory(temporary); string checkpoint = Path.Combine(temporary, "starfall-checkpoint.json"); File.WriteAllText(checkpoint, "checkpoint-sentinel");
            game.StartTraining(); yield return new WaitForFixedUpdate(); Assert.IsTrue(game.Loadout.Invincible); Assert.IsTrue(game.Loadout.InfiniteEnergy); Assert.IsTrue(game.Loadout.InfiniteCharges);
            foreach (string id in new[] { "shotgun", "smg", "medkit", "shield", "rapid", "magnet", "vitality", "agile" }) Assert.IsTrue(game.EquipItem(id));
            Assert.IsTrue(game.EquipItem("rapid")); Assert.IsFalse(game.EquipItem("rapid")); Assert.AreEqual(125, game.Player.Health.State.Maximum);
            game.Training.SetCount(999); game.Training.Simulate("mix"); Assert.AreEqual(12, game.LivingEnemies);
            game.Training.Simulate("elite"); Assert.AreEqual(12, game.LivingEnemies);
            game.Enemies[0].Health.Receive(new DamageContext(1000, Faction.Player)); Assert.AreEqual(1, game.Context.Kills);
            game.Hazards.Begin(3); yield return new WaitForSeconds(.15f); Assert.Greater(game.Projectiles.ActiveCount, 0);
            game.Training.ToggleEnergy(); game.Training.ToggleCharges(); game.Training.ToggleInvincible();
            game.Player.Health.Receive(new DamageContext(1000, Faction.Enemy)); yield return null;
            Assert.AreEqual(RunPhase.Combat, game.Context.Phase); Assert.AreEqual(125, game.Player.Health.State.Health); Assert.Less(Vector2.Distance(game.Room.Spawn, game.Player.Body.position), .1f);
            Assert.AreEqual("smg", game.Loadout.SpecialWeapon); Assert.AreEqual(2, game.Loadout.Layers("rapid"));
            Assert.IsFalse(game.Loadout.Invincible); Assert.IsFalse(game.Loadout.InfiniteEnergy); Assert.IsFalse(game.Loadout.InfiniteCharges);
            Assert.AreEqual(0, game.LivingEnemies); Assert.AreEqual(0, game.Projectiles.ActiveCount); Assert.IsFalse(game.Hazards.Running);
            Assert.AreEqual(0, game.Context.Kills); Assert.AreEqual(0, game.Context.Coins);
            Assert.AreEqual(3, UnityEngine.Object.FindObjectsByType<PracticeTarget>(FindObjectsSortMode.None).Length);
            for (int index = 0; index < 25; index++) { game.EquipItem(index % 2 == 0 ? "shotgun" : "smg"); }
            yield return null; Assert.AreEqual(30, UnityEngine.Object.FindObjectsByType<PracticeInteractable>(FindObjectsSortMode.None).Length, "Every unequipped weapon remains independently pickable until reset");
            game.Training.Reset(true); yield return null; Assert.IsNull(game.Loadout.SpecialWeapon); Assert.IsNull(game.Loadout.Active); Assert.IsEmpty(game.Loadout.Passives);
            game.SetPause(PauseReason.Menu, true); game.RestartMode(); Assert.IsFalse(game.Pause.IsPaused);
            game.ReturnToMenu(); yield return null; Assert.IsNull(game.Loadout); Assert.IsNull(game.Training);
            Assert.AreEqual("checkpoint-sentinel", File.ReadAllText(checkpoint), "Training and reset must preserve the checkpoint");
            game.StartAdventure(); yield return new WaitForFixedUpdate(); Assert.IsFalse(game.Loadout.Invincible); Assert.IsFalse(game.Loadout.InfiniteEnergy); Assert.IsFalse(game.Player.Health.Invulnerable);
            Assert.IsEmpty(game.Loadout.Passives); Assert.AreEqual(100, game.Player.Health.State.Maximum); Assert.IsNotNull(game.Adventure); Assert.AreEqual("courtyard", game.Adventure.Current.Id);
            Assert.AreEqual(1, new CheckpointStore(temporary).Current.stage, "Starting a new adventure now writes its entry checkpoint");
        }
        [UnityTest] public IEnumerator TrainingDamageArmorAndPausesUseEffectiveTime()
        {
            game.StartTraining(); yield return null;
            Place(new Vector2(-9, 2)); yield return Aim(new Vector2(-9, 4), true); yield return new WaitForSeconds(.45f); yield return Aim(new Vector2(-9, 4));
            Assert.Greater(game.PracticeStats.Total, 0); Assert.AreEqual(game.Catalog.Find("pistol").damage, game.PracticeStats.Last);
            game.ResetPracticeStats(); Target("training.armored").Health.Receive(new DamageContext(20, Faction.Player)); Assert.AreEqual(10, game.PracticeStats.Last);
            double elapsed = game.EffectivePracticeTime; float dps = game.PracticeStats.Dps(elapsed);
            game.ModeUI.Open(PracticePanel.Range); yield return new WaitForSecondsRealtime(.25f);
            Assert.AreEqual(elapsed, game.EffectivePracticeTime); Assert.AreEqual(dps, game.PracticeStats.Dps(game.EffectivePracticeTime));
            game.SetPause(PauseReason.Map, true); game.ModeUI.Close(); Assert.AreEqual(0, Time.timeScale); game.SetPause(PauseReason.Map, false);
            game.EquipItem("shield"); yield return KeyPress(Key.Q); Assert.Greater(game.Loadout.ShieldLeft, 0);
            float shield = game.Loadout.ShieldLeft, cooldown = game.Loadout.ActiveCooldown; game.SetPause(PauseReason.Menu, true);
            yield return new WaitForSecondsRealtime(.15f); Assert.AreEqual(shield, game.Loadout.ShieldLeft); Assert.AreEqual(cooldown, game.Loadout.ActiveCooldown);
            game.Resume(); game.EquipItem("medkit"); Assert.AreEqual(0, game.Loadout.ShieldLeft);
            game.Training.ToggleInvincible(); Assert.IsFalse(game.Player.Health.Invulnerable);
        }
        [UnityTest] public IEnumerator PracticeScreensFitBothLanguagesAndFourResolutions()
        {
            foreach (string language in new[] { "zh-CN", "en" })
            {
                game.SetLanguage(language); game.StartTutorial(); yield return null; Capture("tutorial-" + language, 1280, 720);
                game.StartTraining(); yield return null;
                foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(2560, 1440), new Vector2Int(1280, 960) })
                {
                    Capture("training-" + language + "-" + size.x + "x" + size.y, size.x, size.y);
                    foreach (PracticePanel panel in new[] { PracticePanel.Equipment, PracticePanel.Range, PracticePanel.Dodge, PracticePanel.Simulation, PracticePanel.Environment })
                    {
                        game.ModeUI.Open(panel); yield return null; Capture(panel + "-" + language + "-" + size.x + "x" + size.y, size.x, size.y); game.ModeUI.Close(); yield return null;
                    }
                }
                game.SetPause(PauseReason.Map, true); yield return null; Capture("training-map-" + language, 1280, 720); game.SetPause(PauseReason.Map, false);
                game.Training.Simulate("mix"); game.Hazards.Begin(2); yield return new WaitForSeconds(.3f); Capture("training-action-" + language, 1920, 1080);
            }
        }
        void Capture(string name, int width, int height)
        {
            Assert.AreNotEqual(UnityEngine.Rendering.GraphicsDeviceType.Null, SystemInfo.graphicsDeviceType, "Screens require actual graphics");
            string directory = Path.Combine(Application.dataPath, "../Logs/M2b-Screens/M2a-regression"); Directory.CreateDirectory(directory);
            var camera = game.GameCamera; var canvas = game.Interface.Canvas; var target = new RenderTexture(width, height, 24);
            var scaler = canvas.GetComponent<CanvasScaler>(); float oldScale = canvas.scaleFactor, oldSize = camera.orthographicSize;
            var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active; var oldMode = canvas.renderMode;
            camera.targetTexture = target; canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 5;
            scaler.enabled = false; canvas.scaleFactor = Mathf.Min(width / 1280f, height / 720f);
            camera.orthographicSize = Mathf.Max(9.3f, 13.4f / ((float)width / height)); game.Interface.RefreshNow(); game.ModeUI.RefreshNow(); Canvas.ForceUpdateCanvases();
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
