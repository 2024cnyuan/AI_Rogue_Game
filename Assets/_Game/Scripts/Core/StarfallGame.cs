using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Starfall
{
    [DefaultExecutionOrder(-100)]
    public sealed class StarfallGame : MonoBehaviour
    {
#if UNITY_EDITOR
        // Test harness injection: never compiled into a normal Windows player.
        public static string EditorSettingsDirectory;
        public static System.Func<double> EditorAdventureClock;
#endif
        public PrototypeConfig config;
        public PrototypeConfig Config => config;
        public RunContext Context { get; private set; }
        public PauseState Pause { get; } = new PauseState();
        public PlayerInputReader Input { get; private set; }
        public LocalizationService Text { get; private set; }
        public GameSettings Settings { get; private set; }
        public Camera GameCamera { get; private set; }
        public ExplorerController Player { get; private set; }
        public PrototypeRoom Room { get; private set; }
        public ProjectilePool Projectiles { get; private set; }
        public GameInterface Interface { get; private set; }
        public PracticeInterface ModeUI { get; private set; }
        public ItemCatalog Catalog { get; private set; }
        public LoadoutState Loadout { get; private set; }
        public TutorialDirector Tutorial { get; private set; }
        public TrainingDirector Training { get; private set; }
        public PracticeHazards Hazards { get; private set; }
        public DamageWindow PracticeStats { get; } = new DamageWindow();
        public double EffectivePracticeTime { get; private set; }
        public PracticeInteractable Interaction { get; private set; }
        public AdventureDirector Adventure { get; private set; }
        public AdventureInterface AdventureUI { get; private set; }
        public FirstLevelConfig LevelConfig { get; private set; }
        public PersonalBestStore Records { get; private set; }
        public GameAudio Audio { get; private set; }
        public List<ClearResult> PendingRecords { get; } = new List<ClearResult>();
        public void RetryRecordSaves()
        {
            for (int i = PendingRecords.Count - 1; i >= 0; i--) if (Records.Commit(PendingRecords[i])) PendingRecords.RemoveAt(i);
        }
        public IReadOnlyList<PrototypeEnemy> Enemies => enemies;
        public bool CanAct => Context != null && !Pause.IsPaused && (Context.Phase == RunPhase.Combat || Context.Phase == RunPhase.RoomClear)
            && Player != null && Player.Health.State.Alive;
        readonly List<PrototypeEnemy> enemies = new List<PrototypeEnemy>();
        GameObject runRoot;
        GameObject practiceRoot;
        GameObject discardedWeapon;
        readonly List<PracticeInteractable> stations = new List<PracticeInteractable>();
        readonly List<PracticeTarget> targets = new List<PracticeTarget>();
        SettingsStore store;
        RoomCamera cameraFollow;
        bool previousActorCollision;
        float navigationLeft;
        string notificationKey;
        float notificationLeft;
        public string NotificationKey => notificationLeft > 0 ? notificationKey : null;
        public string SaveErrorKey { get; private set; }

        void Awake()
        {
            if (config == null) config = Resources.Load<PrototypeConfig>("PrototypeConfig");
            if (config == null) { Debug.LogError("M1 config missing. Run Starfall > Setup M1 Prototype in the Editor."); enabled = false; return; }
            Input = new PlayerInputReader();
            Catalog = Resources.Load<ItemCatalog>("ItemCatalog");
            if (Catalog == null) { Debug.LogError("ItemCatalog missing. Run Starfall > Setup M2a."); enabled = false; return; }
            string settingsDirectory = Application.persistentDataPath;
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(EditorSettingsDirectory)) settingsDirectory = EditorSettingsDirectory;
#endif
            store = new SettingsStore(settingsDirectory);
            Records = new PersonalBestStore(settingsDirectory);
            LevelConfig = Resources.Load<FirstLevelConfig>("FirstLevelConfig");
            if (LevelConfig == null) { Debug.LogError("FirstLevelConfig missing. Run Starfall > Setup M2b."); enabled = false; return; }
            Settings = store.Load(SettingsStore.DefaultLanguage(Application.systemLanguage));
#if !UNITY_EDITOR
            Screen.fullScreenMode = Settings.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
#endif
            Text = new LocalizationService(Settings.language);
            foreach (var error in Text.Validate()) Debug.LogError(error);
            if (store.ReadProblem || Records.ReadProblem) SaveErrorKey = "save.recovered";
            previousActorCollision = Physics2D.GetIgnoreLayerCollision(2, 2); Physics2D.IgnoreLayerCollision(2, 2, true);
            var cameraObject = new GameObject("M1 Camera"); cameraObject.transform.SetParent(transform, false);
            GameCamera = cameraObject.AddComponent<Camera>(); GameCamera.tag = "MainCamera"; GameCamera.orthographic = true; GameCamera.orthographicSize = 9.3f;
            GameCamera.backgroundColor = new Color(.035f, .065f, .1f); GameCamera.clearFlags = CameraClearFlags.SolidColor;
            GameCamera.nearClipPlane = .1f; GameCamera.farClipPlane = 100; cameraObject.transform.position = new Vector3(0, 0, -10);
            cameraFollow = cameraObject.AddComponent<RoomCamera>(); cameraFollow.Game = this;
            cameraObject.AddComponent<AudioListener>();
            Audio = gameObject.AddComponent<GameAudio>(); Audio.Initialize(this);
            var events = new GameObject("M1 EventSystem"); events.transform.SetParent(transform, false);
            events.AddComponent<EventSystem>(); events.AddComponent<InputSystemUIInputModule>();
            CreateRoom();
            Interface = new GameObject("M1 Interface").AddComponent<GameInterface>(); Interface.transform.SetParent(transform, false); Interface.Initialize(this);
            ModeUI = gameObject.AddComponent<PracticeInterface>(); ModeUI.Initialize(this);
            AdventureUI = gameObject.AddComponent<AdventureInterface>(); AdventureUI.Initialize(this);
        }
        void CreateRoom(GameMode mode = GameMode.Adventure)
        {
            runRoot = new GameObject("M1 Room Runtime"); runRoot.transform.SetParent(transform, false);
            var geometry = new GameObject("Room geometry"); geometry.transform.SetParent(runRoot.transform, false);
            Room = geometry.AddComponent<PrototypeRoom>(); Room.Build(mode);
            practiceRoot = new GameObject("Practice objects"); practiceRoot.transform.SetParent(runRoot.transform, false);
        }
        public void StartAdventure()
        {
            if (Interface != null && Interface.FirstLaunch) return;
            StartMode(GameMode.Adventure);
        }
        public void StartTutorial() { if (!Interface.FirstLaunch) StartMode(GameMode.Tutorial); }
        public void StartTraining() { if (!Interface.FirstLaunch) StartMode(GameMode.Training); }
#if UNITY_EDITOR
        // Retain the original isolated regression fixture; this entry is absent in ordinary players.
        public void StartPrototypeForTests() { StartMode(GameMode.Adventure, true); }
#endif
        public void RestartMode()
        {
            if (Context?.Mode == GameMode.Tutorial) StartTutorial();
            else if (Context?.Mode == GameMode.Training) { Training.Reset(); SetPause(PauseReason.Menu, false); }
            else StartAdventure();
        }
        void StartMode(GameMode mode, bool prototype = false)
        {
            Settings.introductionSeen = true; SaveSettings(); ModeUI?.HideIntroduction();
            DisposeRun(); CreateRoom(mode); Context = new RunContext(mode); if (mode != GameMode.Adventure || prototype) Context.InvalidateRecord();
            Loadout = new LoadoutState(Catalog);
            var playerObject = new GameObject("Explorer"); playerObject.transform.SetParent(runRoot.transform, false); playerObject.transform.position = Room.Spawn;
            Player = playerObject.AddComponent<ExplorerController>(); Player.Initialize(this, config);
            Projectiles = new GameObject("Projectile Pool").AddComponent<ProjectilePool>(); Projectiles.transform.SetParent(runRoot.transform, false); Projectiles.Game = this;
            Hazards = new GameObject("Practice hazards").AddComponent<PracticeHazards>(); Hazards.transform.SetParent(runRoot.transform, false); Hazards.Initialize(this);
            Context.BeginCombat(); EffectivePracticeTime = 0; ResetPracticeStats();
            if (mode == GameMode.Adventure && prototype)
            {
                SpawnEnemy(new Vector2(-6, 3), false); SpawnEnemy(new Vector2(1, 4), false); SpawnEnemy(new Vector2(5, -4), false);
                SpawnEnemy(new Vector2(7, 4), true); SpawnEnemy(new Vector2(8, -1), true);
                Drop(new Vector2(-8, -2), SupplyKind.Coin); Drop(new Vector2(-7, -5), SupplyKind.Health); Drop(new Vector2(8, 5), SupplyKind.Health);
            }
            else if (mode == GameMode.Adventure)
            {
                System.Func<double> clock = null;
#if UNITY_EDITOR
                clock = EditorAdventureClock;
#endif
                Adventure = new AdventureDirector(this, System.Guid.NewGuid().GetHashCode() & int.MaxValue, clock);
            }
            else if (mode == GameMode.Tutorial) Tutorial = new TutorialDirector(this);
            else Training = new TrainingDirector(this);
            Physics2D.SyncTransforms(); Room.RefreshNavigation(Room.Spawn); navigationLeft = .25f;
            cameraFollow.ResetView(); Input.Flush(); Notify(mode == GameMode.Adventure ? "adventure.start" : mode == GameMode.Tutorial ? "tutorial.reenter" : "training.welcome"); Interface?.ResetPanels(); ModeUI?.Close();
        }
        public void SpawnEnemy(Vector2 at, bool ranged, bool elite = false, float strength = 1)
        {
            if (LivingEnemies >= 12 || !Room.IsClear(at, .4f) || Vector2.Distance(at, Player.Body.position) < 1.2f) return;
            var go = new GameObject(ranged ? "Sentinel" : "Pursuer"); go.transform.SetParent(runRoot.transform, false); go.transform.position = at;
            var enemy = go.AddComponent<PrototypeEnemy>(); enemy.Initialize(this, ranged, elite, strength); enemies.Add(enemy);
        }
        public void Drop(Vector2 at, SupplyKind kind)
        {
            var go = new GameObject("Supply " + kind); go.transform.SetParent(practiceRoot.transform, false); go.transform.position = at;
            go.AddComponent<SupplyPickup>().Initialize(this, kind);
        }
        public void EnemyKilled(PrototypeEnemy enemy)
        {
            if (Context == null || Context.Phase != RunPhase.Combat) return;
            Context.RegisterKill(); if (Context.Mode != GameMode.Training && Adventure?.Current.Kind != LevelRoomKind.Challenge) Drop(enemy.transform.position, SupplyKind.Coin);
            if (Context.Mode != GameMode.Adventure) { gameObject.GetComponent<PracticeInterface>()?.RefreshNow(); }
        }
        public int LivingEnemies { get { int count = 0; foreach (var enemy in enemies) if (enemy != null && enemy.Alive) count++; return count; } }
        public void RebuildAdventureRoom(LevelRoomPlan plan)
        {
            ClearPracticeObjects(); if (Room != null) { Room.gameObject.SetActive(false); Destroy(Room.gameObject); }
            var geometry = new GameObject("Room " + plan.Id); geometry.transform.SetParent(runRoot.transform, false);
            Room = geometry.AddComponent<PrototypeRoom>(); Room.Build(GameMode.Adventure, plan); cameraFollow.ResetView();
        }
        public StoneCaptain CreateCaptain(Vector2 at)
        {
            var boss = new GameObject("Stone Captain"); boss.transform.SetParent(runRoot.transform, false); boss.transform.position = at;
            var captain = boss.AddComponent<StoneCaptain>(); captain.Initialize(this); return captain;
        }
        public void ShakeCamera(float amount) => cameraFollow?.Kick(amount);
        void Update()
        {
            if (Input == null) return;
            Input.Sample(CanAct && !Interface.SettingsOpen);
            notificationLeft -= Time.deltaTime;
            if (Context != null && (Context.Phase == RunPhase.Combat || Context.Phase == RunPhase.RoomClear))
            {
                if (Input.PausePressed)
                {
                    if (ModeUI != null && ModeUI.IsOpen) ModeUI.Close();
                    else if (Interface.SettingsOpen) Interface.CloseSettings();
                    else if (Pause.Has(PauseReason.Map)) SetPause(PauseReason.Map, false);
                    else SetPause(PauseReason.Menu, !Pause.Has(PauseReason.Menu));
                }
                if (Input.MapPressed && !Interface.SettingsOpen && (ModeUI == null || !ModeUI.IsOpen) && !Pause.Has(PauseReason.Menu) && !Pause.Has(PauseReason.Focus))
                    SetPause(PauseReason.Map, !Pause.Has(PauseReason.Map));
                if (CanAct)
                {
                    Adventure?.Tick(Time.deltaTime); Adventure?.ApplyMechanism();
                    EffectivePracticeTime += Time.deltaTime;
                    navigationLeft -= Time.deltaTime;
                    if (navigationLeft <= 0) { Room.RefreshNavigation(Player.Body.position); navigationLeft = .25f; }
                    SelectInteraction();
                    if (Input.InteractPressed && Interaction != null) Interaction.Action?.Invoke();
                    if (Context.Mode == GameMode.Tutorial)
                    {
                        Tutorial.Tick(Time.deltaTime); if (Input.InteractPressed) Tutorial.TryDoor();
                    }
                    else if (Context.Mode == GameMode.Training && Input.InteractPressed && Vector2.Distance(Player.Body.position, Room.Exit) < 1.8f) ReturnToMenu();
                    else if (Adventure == null && Context.Phase == RunPhase.RoomClear && Input.InteractPressed && Vector2.Distance(Player.Body.position, Room.Exit) < 1.8f)
                    {
                        if (Context.Complete(Player.Health.State.Alive)) { Projectiles.Clear(); StopBodies(); Input.Flush(); }
                    }
                }
            }
        }
        void LateUpdate()
        {
            if (Context == null || Player == null) return;
            if (Adventure != null) { Adventure.ResolveFrame(); return; }
            if (Context.Mode != GameMode.Adventure)
            {
                if (!Player.Health.State.Alive) { if (Tutorial != null) Tutorial.Retry(); else Training.OnDeath(); }
                return;
            }
            if (!Context.Resolve(Player.Health.State.Alive, LivingEnemies)) return;
            if (Context.Phase == RunPhase.Dead)
            {
                Pause.Clear(); Time.timeScale = 1; Projectiles.Clear(); StopBodies(); Input.Flush(); Interface.ResetPanels();
            }
            else if (Context.Phase == RunPhase.RoomClear)
            {
                Context.GrantRoomReward(); Room.OpenDoor(); Projectiles.Clear(); Notify("room.clear");
            }
        }
        public void SetPause(PauseReason reason, bool value)
        {
            Pause.Set(reason, value); Time.timeScale = Pause.IsPaused ? 0 : 1;
            Adventure?.SyncTimerPause(); Audio?.Pause(Pause.IsPaused);
            if (reason == PauseReason.Map) Tutorial?.MapChanged(value);
            Input?.Flush(); Player?.Flush(); if (Pause.IsPaused) StopBodies();
        }
        public void Resume() { SetPause(PauseReason.Menu, false); if (Application.isFocused) SetPause(PauseReason.Focus, false); }
        void StopBodies() { Player?.Flush(); foreach (var enemy in enemies) if (enemy != null) enemy.Stop(); }
        public void ReturnToMenu() { ModeUI?.Close(); AdventureUI?.CloseRecords(); DisposeRun(); CreateRoom(); cameraFollow.ResetView(); Interface.ResetPanels(); }
        void DisposeRun()
        {
            Pause.Clear(); Time.timeScale = 1; Input?.Flush(); Projectiles?.Clear(); StopBodies();
            Audio?.Pause(false); Adventure?.Boss?.Stop(); AdventureUI?.CloseRecords();
            if (runRoot != null) { runRoot.SetActive(false); Destroy(runRoot); }
            enemies.Clear(); Player = null; Projectiles = null; Context = null; notificationLeft = 0;
            Tutorial = null; Training = null; Loadout = null; Hazards = null; Interaction = null; stations.Clear(); targets.Clear(); discardedWeapon = null;
            Adventure = null;
        }
        public void ClearEnemies()
        {
            foreach (var enemy in enemies) if (enemy != null) { enemy.gameObject.SetActive(false); Destroy(enemy.gameObject); }
            enemies.Clear();
        }
        public void ClearPracticeObjects()
        {
            ClearEnemies(); Projectiles?.Clear(); Hazards?.Stop(); stations.Clear(); targets.Clear(); Interaction = null; discardedWeapon = null;
            if (practiceRoot != null) { practiceRoot.SetActive(false); Destroy(practiceRoot); }
            practiceRoot = new GameObject("Practice objects"); practiceRoot.transform.SetParent(runRoot.transform, false); ResetPracticeStats();
        }
        public void AddTarget(string id, Vector2 at, bool moving = false, bool armored = false)
        {
            var go = new GameObject(id); go.transform.SetParent(practiceRoot.transform, false); go.transform.position = at;
            var target = go.AddComponent<PracticeTarget>(); target.Initialize(this, id, moving, armored); targets.Add(target);
        }
        public void AddStation(string id, string key, Vector2 at, System.Action action)
        {
            var go = new GameObject(id); go.transform.SetParent(practiceRoot.transform, false); go.transform.position = at;
            var station = go.AddComponent<PracticeInteractable>(); station.Initialize(id, key, action, PrototypeVisuals.Teal); stations.Add(station);
        }
        public void MarkerAt(Vector2 at) => PrototypeVisuals.Draw(practiceRoot.transform, "Tutorial marker", at, new Vector2(.9f, .9f), PrototypeVisuals.Gold, 0, "orb");
        void SelectInteraction()
        {
            Interaction = null; float bestDistance = float.MaxValue, bestFacing = -2;
            foreach (var station in stations)
            {
                if (station == null || !station.gameObject.activeInHierarchy) continue;
                Vector2 direction = (Vector2)station.transform.position - Player.Body.position; float distance = direction.sqrMagnitude;
                if (distance > 2.56f || Physics2D.Raycast(Player.Body.position, direction.normalized, direction.magnitude, 1)) continue;
                float facing = Vector2.Dot(Player.Aim, direction.normalized);
                if (distance < bestDistance - .001f || Mathf.Abs(distance - bestDistance) < .001f && (facing > bestFacing + .001f || Mathf.Abs(facing - bestFacing) < .001f && string.CompareOrdinal(station.Id, Interaction?.Id) < 0))
                { Interaction = station; bestDistance = distance; bestFacing = facing; }
            }
        }
        public bool EquipItem(string id, bool gameplayGrant = false)
        {
            if (Adventure != null && !gameplayGrant) Context.InvalidateRecord();
            var definition = Catalog.Find(id); string old = Loadout.SpecialWeapon;
            if (!Loadout.Equip(id)) { Notify("item.limit"); return false; }
            if (definition.kind == ItemKind.Weapon && id != "pistol" && old != null && old != id)
            {
                if (discardedWeapon != null)
                {
                    stations.Remove(discardedWeapon.GetComponent<PracticeInteractable>());
                    discardedWeapon.SetActive(false); Destroy(discardedWeapon);
                }
                Vector2 at = Player.Body.position; string previous = old;
                AddStation("dropped.weapon", Catalog.Find(old).nameKey, at, () => { EquipItem(previous, true); });
                discardedWeapon = stations[stations.Count - 1].gameObject;
            }
            EquipmentChanged(); return true;
        }
        public void ReplaceLoadout(LoadoutState value) { Loadout = value; EquipmentChanged(); }
        public void EquipmentChanged() { Player?.ApplyStats(); ResetPracticeStats(); }
        public void ResetPracticeStats() => PracticeStats.Reset(EffectivePracticeTime);
        public void SaveTutorialMark(bool complete, bool skip) { Settings.tutorialCompleted |= complete; Settings.tutorialSkipped |= skip; SaveSettings(); }
        public void Notify(string key) { notificationKey = key; notificationLeft = 2.5f; }
        public void SetLanguage(string language)
        {
            Settings.language = language == "zh-CN" ? "zh-CN" : "en"; Text.SetLanguage(Settings.language); SaveSettings();
        }
        public void ConfirmLanguage()
        {
            Settings.languageSelected = true; SaveSettings(); Interface.FinishFirstLaunch();
            if (!Settings.introductionSeen && !Settings.tutorialCompleted && !Settings.tutorialSkipped) ModeUI?.ShowIntroduction();
        }
        public void ToggleFullscreen()
        {
            Settings.fullscreen = !Settings.fullscreen; Screen.fullScreenMode = Settings.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed; SaveSettings();
        }
        public void SetVolume(string channel, float value)
        {
            if (channel == "master") Settings.masterVolume = Mathf.Clamp01(value);
            else if (channel == "music") Settings.musicVolume = Mathf.Clamp01(value);
            else if (channel == "effects") Settings.effectsVolume = Mathf.Clamp01(value);
            else Settings.shake = Mathf.Clamp01(value);
            Audio.ApplySettings(); SaveSettings();
        }
        public void ToggleFlash() { Settings.reduceFlash = !Settings.reduceFlash; SaveSettings(); }
        public void ToggleTimer() { Settings.hideTimer = !Settings.hideTimer; SaveSettings(); }
        void SaveSettings() { SaveErrorKey = store.Save(Settings) ? null : "save.failed"; }
        void OnApplicationFocus(bool focus)
        {
            if (Context != null && (Context.Phase == RunPhase.Combat || Context.Phase == RunPhase.RoomClear)) SetPause(PauseReason.Focus, !focus);
        }
        void OnApplicationPause(bool paused) => OnApplicationFocus(!paused);
        public void Quit()
        {
            DisposeRun();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        void OnDestroy()
        {
            Time.timeScale = 1; Input?.Dispose(); Physics2D.IgnoreLayerCollision(2, 2, previousActorCollision); PrototypeVisuals.Release();
        }
    }
}
