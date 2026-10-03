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
    public sealed partial class CampaignPlayTests
    {
        StarfallGame game;
        Keyboard keyboard;
        Mouse mouse;
        string temporary;
        readonly List<string> m5LayoutProblems = new List<string>();
        InputSettings.BackgroundBehavior background;
        InputSettings.EditorInputBehaviorInPlayMode editorInput;
        [UnitySetUp] public IEnumerator Setup()
        {
            temporary = Path.Combine(Path.GetTempPath(), "StarfallM3-" + Guid.NewGuid());
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
            for(int wave=0;wave<4 && game.LivingEnemies>0;wave++) { foreach(var enemy in new List<PrototypeEnemy>(game.Enemies)) if(enemy.Alive) enemy.Health.Receive(new DamageContext(1000,Faction.Player)); yield return null; }
            yield return null; yield return null;
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
            while (game.LivingEnemies > 0 || game.Adventure.BossHealth != null && game.Adventure.BossHealth.State.Alive)
            {
                Assert.IsTrue(game.Player.Health.State.Alive, "Normal equipment bot died in " + game.Adventure.Current.Id);
                var weapon=game.Catalog.Find(game.Loadout.Weapon); if(weapon.id!="pistol" && game.Loadout.Energy<weapon.energyCost) { yield return Press(Key.Digit1); continue; }
                Assert.Less(Time.realtimeSinceStartup - started, 80, "Normal input fight stalled in " + game.Adventure.Current.Id);
                Vector2 target = game.Adventure.BossHealth != null ? game.Adventure.BossPosition : Vector2.zero; float score = float.MaxValue;
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

        void StartFixture(int stage)
        {
            game.ReturnToMenu(); var gear = new LoadoutState(game.Catalog); gear.Equip("medkit");
            Assert.IsTrue(game.SaveCheckpoint(new EntryCheckpoint { stage = stage, seed = 1729, runId = "fixture", eligible = false, coins = 100, loadout = gear.Snapshot() }));
            Assert.IsTrue(game.ContinueAdventure());
        }
        IEnumerator ReachGuardian(bool practice = true)
        {
            if (practice) game.Context.InvalidateRecord();
            yield return Interact("route.next");
            foreach (string id in new[] { "courtyard", "north", "crossing", "south" })
            {
                Assert.AreEqual(id, game.Adventure.Current.Id); yield return ClearRoom();
                if (id == "north" || id == "south") yield return Interact("beacon." + id);
                yield return Interact("route.next");
            }
            Assert.AreEqual("seal", game.Adventure.Current.Id); yield return Interact("seal.relay");
            yield return Interact("route.next"); yield return Interact("supply.restore"); yield return Interact("route.next");
            Assert.IsNotNull(game.Adventure.BossHealth);
        }
        IEnumerator FinishFixture()
        {
            yield return ReachGuardian(); game.Adventure.BossHealth.Receive(new DamageContext(10000, Faction.Player)); yield return null; yield return null;
            Assert.AreEqual(RunPhase.Complete, game.Context.Phase);
        }
        [UnityTest] [Timeout(900000)] public IEnumerator NormalInputAdventureCompletesSixStagesAndReloadsSixRecords()
        {
            var report = new System.Text.StringBuilder("M5 six independent starts, real keyboard/mouse. No teleport, injected damage, invincibility or speed changes. Temporary profile and records.\n");
            for(int stage=1;stage<=6;stage++) {
                Assert.IsTrue(game.StartStage(stage)); yield return null;
                if(stage==1) yield return WalkTo(new Vector2(-5.7f,-4));
                else { yield return WalkTo(game.Room.Exit); yield return Press(Key.F); }
                foreach(string id in stage==4 || stage==5 ? new[]{"courtyard","north","crossing","middle","south"} : new[]{"courtyard","north","crossing","south"}) {
                    Assert.AreEqual(id,game.Adventure.Current.Id); yield return FightUsingInput();
                    if(game.Adventure.Current.Kind==LevelRoomKind.Beacon) { yield return WalkTo(new Vector2(7,2)); yield return Press(Key.F); }
                    if(stage==1 && id=="south") { yield return WalkTo(new Vector2(-6,-4)); yield return Press(Key.F); }
                    yield return WalkTo(game.Room.Exit); yield return Press(Key.F);
                }
                if(stage>1) {
                    yield return WalkTo(new Vector2(-8,5.5f)); yield return WalkTo(new Vector2(7,5.5f)); yield return WalkTo(new Vector2(7,3)); yield return Press(Key.F);
                    yield return WalkTo(game.Room.Exit); yield return Press(Key.F);
                    yield return WalkTo(new Vector2(-1,-1)); yield return Press(Key.F); yield return WalkTo(game.Room.Exit); yield return Press(Key.F);
                }
                yield return FightUsingInput(); Assert.IsTrue(game.Adventure.Result.Success); Assert.IsTrue(game.Adventure.Result.Eligible); Assert.IsTrue(game.Adventure.Result.Saved);
                report.Append(stage).Append(" | seed ").Append(game.Adventure.Plan.Seed).Append(" | ").Append(LevelTimer.Format(game.Adventure.Result.Attempt.milliseconds)).Append(" | HP ").Append(game.Player.Health.State.Health).Append(" | ").Append(string.Join(" / ",game.Adventure.Result.Attempt.equipment)).Append("\n");
                if(game.Adventure.Rewards.Count>0) Assert.IsTrue(game.Adventure.SelectReward(game.Adventure.Rewards[0]));
                Capture("m5-normal-clear-s"+stage+"-en",1920,1080);
                Assert.IsTrue(game.Adventure.AdvanceStage()); yield return null;
                var gear=game.Profile.Prepared(); if(game.Profile.Owns("smg")) gear.Equip("smg"); else if(game.Profile.Owns("shotgun")) gear.Equip("shotgun");
                if(game.Profile.Owns("rapid") && gear.Layers("rapid")==0) gear.Equip("rapid"); Assert.IsTrue(game.Profile.SavePreparation(gear));
            }
            Assert.AreEqual(6,new PersonalBestStore(temporary).Book.bests.Count); File.WriteAllText(Path.Combine(Application.dataPath,"../../appendix/M5/normal-input.txt"),report.ToString());
        }
        [UnityTest] public IEnumerator RewardSaveFailureCannotGrantOrDuplicateNextEntryAndCancelKeepsProgress()
        {
            game.StartStage(2); yield return null; yield return ReachGuardian(false); game.Adventure.BossHealth.Receive(new DamageContext(10000,Faction.Player)); yield return null; yield return null;
            string id=game.Adventure.Rewards[0]; string blocked=Path.Combine(temporary,"starfall-profile.json.tmp"); Directory.CreateDirectory(blocked);
            Assert.IsFalse(game.Adventure.SelectReward(id)); Assert.IsFalse(game.Profile.Owns(id)); Assert.IsTrue(game.Profile.HasPending(2)); Assert.IsFalse(game.Adventure.AdvanceStage());
            Directory.Delete(blocked); Assert.IsTrue(game.Adventure.SelectReward(id)); Assert.IsFalse(game.Adventure.SelectReward(id)); Assert.IsTrue(game.Adventure.AdvanceStage());
            Assert.IsNull(game.Context); Assert.AreEqual(FrontPage.Stages,game.FrontEnd.Page); Assert.IsFalse(game.Checkpoints.HasEntry); Assert.IsTrue(new PlayerProfileStore(temporary,game.Catalog).Owns(id));
        }
        [UnityTest] public IEnumerator ResumeAfterApplicationReloadRestoresEntryAndPracticeCannotContaminateIt()
        {
            Assert.IsTrue(game.StartStage(6)); yield return null; var entry=JsonUtility.FromJson<EntryCheckpoint>(JsonUtility.ToJson(game.Checkpoints.Current));
            game.ReturnToMenu(); game.StartTraining(); game.EquipItem("rapid"); game.StartEnvironmentSample(2); yield return null; game.ReturnToMenu();
            Assert.AreEqual(JsonUtility.ToJson(entry),JsonUtility.ToJson(game.Checkpoints.Current)); Assert.IsFalse(game.Profile.Owns("rapid"));
            UnityEngine.Object.Destroy(game.gameObject); yield return null; SceneManager.LoadScene("Assets/_Game/Scenes/Boot.unity"); yield return null; game=UnityEngine.Object.FindFirstObjectByType<StarfallGame>();
            Assert.IsTrue(game.ContinueAdventure()); yield return null; Assert.AreEqual(6,game.Adventure.Stage); Assert.AreEqual("entry",game.Adventure.Current.Id); Assert.AreEqual(entry.seed,game.Adventure.Plan.Seed);
            Assert.AreEqual(JsonUtility.ToJson(entry.loadout),JsonUtility.ToJson(game.Loadout.Snapshot())); Assert.AreEqual(0,game.Context.Coins); Assert.IsFalse(game.Loadout.Invincible);
            game.Player.Health.State.Damage(10000,1000,false,0); yield return null; Assert.IsFalse(game.Checkpoints.HasEntry); Assert.AreEqual(2,game.Profile.Data.unlocked.Count);
        }
        [UnityTest] public IEnumerator ShopTimeIsExcludedAndPurchasesAreAtomicAcrossPausesAndRevisits()
        {
            double now = 0; StarfallGame.EditorAdventureClock = () => now; StartFixture(2); yield return null;
            yield return Interact("route.next"); yield return ClearRoom(); yield return Interact("route.next"); yield return ClearRoom(); yield return Interact("beacon.north"); yield return Interact("route.branch"); yield return Interact("shop.open");
            Assert.IsTrue(game.AdventureUI.ShopOpen); Assert.IsTrue(game.Pause.Has(PauseReason.Shop)); Assert.IsFalse(game.CanAct);
            now = 10; Assert.AreEqual(0, game.Adventure.Timer.Milliseconds); int coins = game.Context.Coins;
            Assert.IsFalse(game.Adventure.Buy("heal")); Assert.AreEqual(coins, game.Context.Coins);
            game.Loadout.SpendEnergy(50); Assert.IsTrue(game.Adventure.Buy("energy")); Assert.AreEqual(coins - game.CampaignConfig.energyPrice, game.Context.Coins);
            Assert.IsFalse(game.Adventure.Buy("energy")); Assert.AreEqual(coins - game.CampaignConfig.energyPrice, game.Context.Coins);
            game.SetPause(PauseReason.Menu, true); game.SetPause(PauseReason.Focus, true); now = 20; game.SetPause(PauseReason.Menu, false); now = 30; Assert.AreEqual(0, game.Adventure.Timer.Milliseconds);
            game.SetPause(PauseReason.Focus, false); now = 35; Assert.AreEqual(0, game.Adventure.Timer.Milliseconds);
            game.AdventureUI.CloseShop(); yield return Interact("route.back"); yield return Interact("route.branch"); yield return Interact("shop.open"); Assert.IsTrue(game.Adventure.Shop.Sold("energy"));
            Capture("shop-en", 1280, 720); game.SetLanguage("zh-CN"); Capture("shop-zh-CN", 1280, 720);
        }
        [UnityTest] public IEnumerator BothGuardiansHaveThreeMovesPauseCleanlyAndSameFrameDeathWins()
        {
            for (int stage = 2; stage <= 3; stage++)
            {
                StartFixture(stage); yield return null; yield return ReachGuardian(); var guardian = game.Adventure.Guardian; Place(new Vector2(-8, -5));
                var moves = new HashSet<int>();
                for (float time = 0; time < 8.5f; time += .1f) { moves.Add(guardian.Move); yield return new WaitForSeconds(.1f); }
                Assert.AreEqual(3, moves.Count); Assert.IsTrue(game.Player.Health.State.Alive);
                var move = guardian.Move; long elapsed = game.Adventure.Timer.Milliseconds; game.SetPause(PauseReason.Map, true); yield return new WaitForSecondsRealtime(.1f);
                Assert.AreEqual(move, guardian.Move); Assert.That(game.Adventure.Timer.Milliseconds, Is.InRange(elapsed, elapsed + 5)); game.SetPause(PauseReason.Map, false);
                game.Player.Health.State.Damage(10000, 9999, false, 0); guardian.Health.Receive(new DamageContext(10000, Faction.Player)); yield return null;
                Assert.AreEqual(RunPhase.Dead, game.Context.Phase); Assert.AreEqual(ClearFeedback.Failed, game.Adventure.Result.Feedback); Assert.IsFalse(game.Checkpoints.HasEntry); Assert.IsNull(game.Records.Find(game.Adventure.StageId)); Assert.AreEqual(0, game.Projectiles.ActiveCount);
                Capture("death-s" + stage + "-en", 1280, 720);
            }
        }
        [UnityTest] public IEnumerator OptionalProductionAndEscortDoNotBlockMainRouteAndNeverPayTwice()
        {
            double now = 0; StarfallGame.EditorAdventureClock = () => now;
            StartFixture(2); yield return null;
            yield return Interact("route.next"); yield return ClearRoom(); yield return Interact("route.branch"); Assert.AreEqual("bypass", game.Adventure.Current.Id);
            yield return ClearRoom(); yield return Interact("route.next"); Assert.AreEqual("crossing", game.Adventure.Current.Id); yield return ClearRoom(); yield return Interact("route.branch"); yield return ClearRoom();
            Assert.IsNull(game.Loadout.SpecialWeapon); yield return Interact("supply.workshop_smg"); Assert.AreEqual("workshop_smg", game.Loadout.SpecialWeapon); Assert.IsTrue(game.Adventure.Progress.IsClaimed("challenge.reward")); int coins = game.Context.Coins;
            yield return Interact("route.back"); yield return Interact("route.branch"); Assert.AreEqual(0, game.LivingEnemies); Assert.AreEqual(coins, game.Context.Coins);
            StartFixture(3); yield return null; yield return Interact("route.next"); yield return ClearRoom();
            yield return Interact("route.branch"); Assert.AreEqual("south", game.Adventure.Current.Id); yield return ClearRoom(); yield return Interact("beacon.south"); Assert.IsFalse(game.Adventure.Travel("seal"));
            yield return Interact("route.back"); yield return Interact("route.next"); yield return ClearRoom(); yield return Interact("beacon.north"); yield return Interact("route.next"); yield return ClearRoom(); yield return Interact("route.branch");
            Assert.IsNotNull(game.Adventure.Escort); yield return Interact("route.back"); yield return Interact("route.branch"); now = 46; yield return null;
            Assert.IsTrue(game.Adventure.ChallengeFailed); yield return Interact("route.back"); Assert.AreEqual("crossing", game.Adventure.Current.Id);
            StartFixture(3); yield return null; yield return Interact("route.next"); yield return ClearRoom(); yield return Interact("route.next"); yield return ClearRoom(); yield return Interact("beacon.north"); yield return Interact("route.next"); yield return ClearRoom(); yield return Interact("route.branch");
            Place(game.Adventure.Escort.transform.position); game.Adventure.Tick(18); yield return ClearRoom(); game.Adventure.Tick(0); yield return ClearRoom();
            Assert.IsTrue(game.Adventure.EscortComplete); Assert.IsTrue(game.Adventure.Progress.IsClaimed("challenge.reward")); coins = game.Context.Coins;
            yield return Interact("route.back"); yield return Interact("route.branch"); Assert.AreEqual(coins, game.Context.Coins);
        }
        [UnityTest] public IEnumerator TrainingSurfacesUseRealInputAndClearOnResetOrExit()
        {
            game.StartTraining(); yield return null; game.StartEnvironmentSample(2); Place(Vector2.zero); yield return new WaitForSeconds(.3f);
            Assert.Greater(game.Player.Body.position.x, .2f); var position = game.Player.Body.position;
            game.SetPause(PauseReason.Menu, true); yield return new WaitForSecondsRealtime(.1f); Assert.AreEqual(position, game.Player.Body.position); game.SetPause(PauseReason.Menu, false);
            game.StartEnvironmentSample(3); Place(Vector2.zero); InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D)); yield return new WaitForSeconds(.2f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null; yield return new WaitForFixedUpdate(); Assert.Greater(game.Player.Body.linearVelocity.x, 0);
            game.Training.Reset(); yield return null; Assert.IsNull(game.Surface); game.StartEnvironmentSample(2); game.ReturnToMenu(); game.StartAdventure(); yield return null;
            Assert.IsNull(game.Surface); Assert.IsTrue(game.Context.RecordEligible); Assert.IsFalse(game.Loadout.Invincible); Assert.IsFalse(game.Loadout.InfiniteEnergy);
        }
        [UnityTest] public IEnumerator ThemeScreensAreBilingualAtFourAspectRatios()
        {
            int[,] sizes = { {1280,720}, {1920,1080}, {2560,1440}, {1280,960} };
            for (int stage = 2; stage <= 3; stage++)
            {
                StartFixture(stage); yield return null;
                foreach (string language in new[] { "en", "zh-CN" })
                {
                    game.SetLanguage(language);
                    for (int i = 0; i < sizes.GetLength(0); i++) Capture("entry-s" + stage + "-" + language + "-" + sizes[i,0] + "x" + sizes[i,1], sizes[i,0], sizes[i,1]);
                }
                yield return ReachGuardian();
                foreach (string language in new[] { "en", "zh-CN" }) { game.SetLanguage(language); Capture("boss-s" + stage + "-" + language, 1920,1080); }
                game.Adventure.BossHealth.Receive(new DamageContext(10000,Faction.Player)); yield return null;
                foreach (string language in new[] { "en", "zh-CN" })
                {
                    game.SetLanguage(language);
                    for (int i = 0; i < sizes.GetLength(0); i++) Capture("result-s" + stage + "-" + language + "-" + sizes[i,0] + "x" + sizes[i,1], sizes[i,0], sizes[i,1]);
                }
            }
            game.ReturnToMenu(); game.StartTraining(); game.ModeUI.Open(PracticePanel.Environment); Capture("environment-en",1280,720); game.SetLanguage("zh-CN"); Capture("environment-zh-CN",1280,720);
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
            game.Interface.RefreshNow(); game.ModeUI.RefreshNow(); game.AdventureUI.RefreshNow(); game.GetComponent<LoadoutInterface>().RefreshNow(); Canvas.ForceUpdateCanvases();
            game.FrontEnd.RefreshLayout(); Canvas.ForceUpdateCanvases();
            foreach (var view in game.Interface.GetComponentsInChildren<Text>())
            {
                if (!view.gameObject.activeInHierarchy || string.IsNullOrEmpty(view.text)) continue;
                if(name.StartsWith("M5-")) {
                    if(view.preferredHeight > view.rectTransform.rect.height+3) m5LayoutProblems.Add(name+": overflow "+view.name+" actual="+view.preferredHeight+" box="+view.rectTransform.rect.height+" text="+view.text);
                    if(view.text.Contains("{")) m5LayoutProblems.Add(name+": unformatted "+view.text);
                    foreach(char character in view.text) if(character>127 && !char.IsWhiteSpace(character) && !view.font.HasCharacter(character,true,true)) m5LayoutProblems.Add(name+": missing glyph "+character);
                    continue;
                }
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
