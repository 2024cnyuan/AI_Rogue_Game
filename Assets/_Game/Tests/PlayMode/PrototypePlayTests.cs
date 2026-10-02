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

namespace Starfall.Tests
{
    public sealed class PrototypePlayTests
    {
        StarfallGame game;
        Keyboard keyboard;
        Mouse mouse;
        string temporary;
        InputSettings.BackgroundBehavior backgroundBehavior;
        InputSettings.EditorInputBehaviorInPlayMode editorInputBehavior;
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            temporary = Path.Combine(Path.GetTempPath(), "StarfallPlay-" + Guid.NewGuid());
            backgroundBehavior = InputSystem.settings.backgroundBehavior;
            editorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            StarfallGame.EditorSettingsDirectory = temporary;
            keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
            SceneManager.LoadScene("Assets/_Game/Scenes/Boot.unity"); yield return null;
            game = UnityEngine.Object.FindFirstObjectByType<StarfallGame>(); Assert.IsNotNull(game);
            Assert.IsTrue(game.Interface.FirstLaunch);
            game.ConfirmLanguage(); yield return null;
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (game != null) UnityEngine.Object.Destroy(game.gameObject);
            InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(mouse); StarfallGame.EditorSettingsDirectory = null;
            InputSystem.settings.backgroundBehavior = backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = editorInputBehavior;
            Time.timeScale = 1; yield return null;
            if (Directory.Exists(temporary)) Directory.Delete(temporary, true);
        }
        [UnityTest]
        public IEnumerator EntryInputAimDodgeAndPauseArePlayable()
        {
            Assert.IsNull(game.Context); game.StartAdventure(); yield return new WaitForFixedUpdate();
            Assert.AreEqual(5, game.LivingEnemies); Assert.IsFalse(game.Context.RecordEligible);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = game.GameCamera.WorldToScreenPoint(new Vector3(8, -4, 0)) });
            yield return null; yield return new WaitForSeconds(.06f);
            Assert.IsTrue(game.CanAct, "Gameplay unexpectedly paused: " + game.Pause.Reasons);
            Assert.Greater(game.Input.Move.magnitude, .9f, "Injected WASD did not reach the Input System action");
            Assert.That(game.Player.Body.linearVelocity.magnitude, Is.EqualTo(5).Within(.1));
            Assert.Greater(game.Player.Aim.x, .9f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D, Key.Space)); yield return null; yield return new WaitForFixedUpdate();
            Assert.IsTrue(game.Player.IsDodging); Assert.IsTrue(game.Player.Health.Invulnerable);
            var at = game.Player.Body.position; float cooldown = game.Player.DodgeCooldown;
            game.SetPause(PauseReason.Map, true); game.SetPause(PauseReason.Focus, true);
            yield return new WaitForSecondsRealtime(.12f); Assert.That(Vector2.Distance(at, game.Player.Body.position), Is.LessThan(.01));
            Assert.AreEqual(cooldown, game.Player.DodgeCooldown);
            game.SetPause(PauseReason.Map, false); Assert.AreEqual(0, Time.timeScale);
            game.SetPause(PauseReason.Focus, false); Assert.AreEqual(1, Time.timeScale);
        }
        [UnityTest]
        public IEnumerator MouseAttackUsesRealPistolToDamageEnemies()
        {
            game.StartAdventure(); yield return null; game.Player.Body.position = new Vector2(8, -4);
            Physics2D.SyncTransforms(); var enemy = game.Enemies[4]; float initial = enemy.Health.State.Health;
            var pointer = (Vector2)game.GameCamera.WorldToScreenPoint(enemy.transform.position);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer }); yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer, buttons = 1 });
            yield return new WaitForSeconds(.45f);
            Assert.Less(enemy.Health.State.Health, initial, "Mouse attack did not damage the target through the real projectile path");
        }
        [UnityTest]
        public IEnumerator DodgeAndMuzzleCannotCrossSolidWalls()
        {
            game.StartAdventure(); yield return new WaitForFixedUpdate();
            game.Player.Body.position = new Vector2(-10.6f, -4); Physics2D.SyncTransforms();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A, Key.Space));
            yield return null; yield return new WaitForSeconds(.25f);
            Assert.GreaterOrEqual(game.Player.Body.position.x, -10.71f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.QueueStateEvent(mouse, new MouseState { position = game.GameCamera.WorldToScreenPoint(new Vector3(-14, -4, 0)) });
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = game.GameCamera.WorldToScreenPoint(new Vector3(-14, -4, 0)), buttons = 1 });
            yield return null; yield return new WaitForFixedUpdate(); Assert.AreEqual(0, game.Projectiles.ActiveCount);
        }
        [UnityTest]
        public IEnumerator RoomClearDeathAndRestartCleanEveryRun()
        {
            game.StartAdventure(); yield return null;
            foreach (var enemy in game.Enemies) enemy.Health.Receive(new DamageContext(1000, Faction.Player));
            yield return null; Assert.AreEqual(RunPhase.RoomClear, game.Context.Phase); Assert.IsTrue(game.Room.DoorOpen);
            Assert.AreEqual(10, game.Context.Coins); Assert.IsFalse(game.Context.GrantRoomReward());
            game.Player.Health.Receive(new DamageContext(1000, Faction.Enemy)); yield return null;
            Assert.AreEqual(RunPhase.Dead, game.Context.Phase); Assert.AreEqual(0, game.Projectiles.ActiveCount);
            game.StartAdventure(); yield return null; Assert.AreEqual(5, game.LivingEnemies); Assert.AreEqual(0, game.Context.Coins);
            Assert.AreEqual(100, game.Player.Health.State.Health); Assert.IsFalse(game.Room.DoorOpen);
            game.ReturnToMenu(); yield return null; Assert.IsNull(game.Context); Assert.AreEqual(1, Time.timeScale);
            Assert.IsNull(UnityEngine.Object.FindFirstObjectByType<ExplorerController>());
        }
        [UnityTest]
        public IEnumerator OpenExitRequiresInteractionAndCompletesOnlyOnce()
        {
            game.StartAdventure(); yield return null;
            foreach (var enemy in game.Enemies) enemy.Health.Receive(new DamageContext(1000, Faction.Player));
            yield return null; game.Player.Body.position = game.Room.Exit; Physics2D.SyncTransforms();
            yield return null; Assert.AreEqual(RunPhase.RoomClear, game.Context.Phase);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); yield return null; yield return null;
            Assert.AreEqual(RunPhase.Complete, game.Context.Phase); Assert.IsFalse(game.Context.Complete(true));
            Assert.AreEqual(0, game.Projectiles.ActiveCount);
        }
        [UnityTest]
        public IEnumerator FullHealthPickupStaysAndPoolReuseCannotKeepOldFaction()
        {
            game.StartAdventure(); yield return null;
            game.Player.Body.position = new Vector2(-7, -5); yield return new WaitForSeconds(.12f);
            Assert.AreEqual(3, UnityEngine.Object.FindObjectsByType<SupplyPickup>(FindObjectsSortMode.None).Length, "Full-health supply was consumed");
            game.Player.Health.Receive(new DamageContext(30, Faction.Enemy)); yield return new WaitForSeconds(.2f);
            Assert.AreEqual(95, game.Player.Health.State.Health);
            game.Projectiles.Spawn(Vector2.zero, Vector2.right, Faction.Enemy, 1, 2); game.Projectiles.Clear();
            Assert.AreEqual(0, game.Projectiles.ActiveCount);
            Assert.IsTrue(game.Projectiles.Spawn(Vector2.zero, Vector2.right, Faction.Player, 20, 18)); Assert.AreEqual(1, game.Projectiles.ActiveCount);
            game.Projectiles.Clear(); game.Projectiles.Clear(); Assert.AreEqual(0, game.Projectiles.ActiveCount);
        }
        [UnityTest]
        public IEnumerator LanguageSwitchKeepsRunAndRefreshesOpenMapAndSettings()
        {
            game.StartAdventure(); yield return null; var run = game.Context; var player = game.Player;
            game.SetPause(PauseReason.Map, true); game.Interface.OpenSettings();
            game.SetLanguage("zh-CN"); yield return null; Assert.AreSame(run, game.Context); Assert.AreSame(player, game.Player);
            Assert.IsTrue(game.Pause.Has(PauseReason.Map)); Assert.IsTrue(game.Pause.Has(PauseReason.Settings));
            game.Interface.CloseSettings(); Assert.AreEqual(0, Time.timeScale);
            game.SetLanguage("en"); yield return null;
            Assert.AreEqual("en", new SettingsStore(temporary).Load("zh-CN").language);
        }
        [UnityTest]
        public IEnumerator RealRuntimeScreensAndBilingualLayout()
        {
            foreach (var language in new[] { "zh-CN", "en" })
            {
                game.SetLanguage(language); game.ReturnToMenu(); yield return null;
                Capture("menu-" + language, 1280, 720); CheckTextFits();
                game.StartAdventure(); yield return null; game.SetPause(PauseReason.Menu, true);
                game.Resume(); yield return null; Capture("room-" + language, 1920, 1080); CheckTextFits();
                game.SetPause(PauseReason.Map, true); yield return null; Capture("map-" + language, 1280, 720); CheckTextFits();
                game.Interface.OpenSettings(); yield return null; Capture("settings-" + language, 1280, 720); CheckTextFits();
                game.Interface.CloseSettings(); game.SetPause(PauseReason.Map, false);
                game.Player.Health.Receive(new DamageContext(1000, Faction.Enemy)); yield return null; Capture("death-" + language, 1280, 720); CheckTextFits();
                game.StartAdventure(); yield return null;
                foreach (var enemy in game.Enemies) enemy.Health.Receive(new DamageContext(1000, Faction.Player));
                yield return null; game.Context.Complete(true); yield return null; Capture("clear-" + language, 1280, 720);
                foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(2560, 1440), new Vector2Int(1280, 960) })
                {
                    game.ReturnToMenu(); yield return null; Capture("menu-" + language + "-" + size.x + "x" + size.y, size.x, size.y);
                    game.StartAdventure(); yield return null; Capture("room-" + language + "-" + size.x + "x" + size.y, size.x, size.y);
                    game.SetPause(PauseReason.Menu, true); game.Interface.OpenSettings(); yield return null;
                    Capture("settings-" + language + "-" + size.x + "x" + size.y, size.x, size.y);
                    game.Interface.CloseSettings();
                }
            }
            game.ReturnToMenu(); yield return null; Capture("menu-en-4x3", 1280, 960); CheckTextFits();
            Capture("menu-en-1440p", 2560, 1440); CheckTextFits();
        }
        void CheckTextFits()
        {
            Canvas.ForceUpdateCanvases();
            foreach (var view in game.Interface.GetComponentsInChildren<Text>())
            {
                if (!view.gameObject.activeInHierarchy || string.IsNullOrEmpty(view.text)) continue;
                Assert.LessOrEqual(view.preferredHeight, view.rectTransform.rect.height + 3, "Text overflow: " + view.name + " / " + view.text);
                Assert.IsFalse(view.text.Contains("{"), "Unformatted parameter: " + view.text);
                foreach (char character in view.text) if (character > 127 && !char.IsWhiteSpace(character))
                    Assert.IsTrue(view.font.HasCharacter(character), "Font lacks character: " + character);
            }
        }
        void Capture(string name, int width, int height)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            string directory = Path.Combine(Application.dataPath, "../Logs/M2a-Screens/M1-regression"); Directory.CreateDirectory(directory);
            var camera = game.GameCamera; var canvas = game.Interface.Canvas; var target = new RenderTexture(width, height, 24);
            var scaler = canvas.GetComponent<CanvasScaler>(); float oldScale = canvas.scaleFactor, oldSize = camera.orthographicSize;
            var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active; var oldMode = canvas.renderMode;
            camera.targetTexture = target; canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 5;
            scaler.enabled = false; canvas.scaleFactor = Mathf.Min(width / 1280f, height / 720f);
            camera.orthographicSize = Mathf.Max(9.3f, 13.4f / ((float)width / height)); game.Interface.RefreshNow(); game.ModeUI.RefreshNow();
            Canvas.ForceUpdateCanvases();
            var request = new UnityEngine.Rendering.RenderPipeline.StandardRequest { destination = target };
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera, request); RenderTexture.active = target;
            CheckTextFits();
            var image = new Texture2D(width, height, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
            File.WriteAllBytes(Path.Combine(directory, name + ".png"), image.EncodeToPNG());
            camera.targetTexture = oldTarget; RenderTexture.active = oldActive; canvas.renderMode = oldMode;
            canvas.scaleFactor = oldScale; scaler.enabled = true; camera.orthographicSize = oldSize;
            target.Release(); UnityEngine.Object.Destroy(target); UnityEngine.Object.Destroy(image); Canvas.ForceUpdateCanvases();
        }
    }
}
#endif
