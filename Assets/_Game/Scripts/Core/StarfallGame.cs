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
        public CombatEffects Effects { get; private set; }
        System.Random combatRandom;
        public List<long> RunTimes { get; private set; } = new List<long>();
        public string PendingPassive { get; private set; }
        public string PendingEquipment { get; private set; }
        System.Func<bool> pendingEquipmentAction;
        public void RequestEquipmentReplacement(string id, System.Func<bool> action) { PendingEquipment = id; pendingEquipmentAction = action; SetPause(PauseReason.PracticePanel, true); }
        public bool ConfirmEquipmentReplacement() { if (PendingEquipment == null || pendingEquipmentAction?.Invoke() != true) return false; CancelEquipmentReplacement(); return true; }
        public void CancelEquipmentReplacement() { PendingEquipment = null; pendingEquipmentAction = null; SetPause(PauseReason.PracticePanel, false); }
        System.Func<string,bool> pendingReplacement;
        public void RequestPassiveReplacement(string id, System.Func<string,bool> action = null) { PendingPassive = id; pendingReplacement = action; SetPause(PauseReason.PracticePanel,true); }
        public void CancelPassive() { PendingPassive = null; pendingReplacement = null; SetPause(PauseReason.PracticePanel, false); }
        public bool ReplacePassive(string old) {
            if (PendingPassive == null || !(pendingReplacement != null ? pendingReplacement(old) : Loadout.ReplacePassive(old, PendingPassive))) return false;
            CancelPassive(); EquipmentChanged(); return true;
        }
        public float ModifyDamage(Damageable target, DamageContext hit) {
            CurrentDamageCritical = false;
            if (target == Player?.Health) return hit.Amount * Loadout.IncomingMultiplier(target.State);
            if (hit.Source != Faction.Player) return hit.Amount;
            float amount = hit.Amount * (target.Controlled ? 1 + Loadout.PassiveValue("controlled") / 100 : 1);
            if (combatRandom != null && Loadout.CritChance > 0 && combatRandom.NextDouble() < Loadout.CritChance) { amount *= 1.75f; CurrentDamageCritical = true; }
            return amount;
        }
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
        public CampaignConfig CampaignConfig { get; private set; }
        public CheckpointStore Checkpoints { get; private set; }
        public PlayerProfileStore Profile { get; private set; }
        public FrontEndInterface FrontEnd { get; private set; }
        public FeedbackPresenter Feedback { get; private set; }
        public int SelectedStage { get; set; } = 1;
        public bool CurrentDamageCritical { get; private set; }
        public PracticeInteractable PickupInteraction { get; private set; }
        public ThemeEnvironment Surface { get; private set; }
        public string RunId { get; private set; }
        public bool NewRunConfirmation { get; private set; }
        public string CheckpointErrorKey { get; private set; }
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
        readonly Dictionary<string, GameObject> roomObjects = new Dictionary<string, GameObject>();
        readonly Dictionary<string, List<Rect>> brokenCover = new Dictionary<string, List<Rect>>();
        string builtRoomId;
        int droppedId;
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
            Checkpoints = new CheckpointStore(settingsDirectory);
            Profile = new PlayerProfileStore(settingsDirectory, Catalog);
            CampaignConfig = Resources.Load<CampaignConfig>("CampaignConfig");
            if (CampaignConfig == null) { Debug.LogError("CampaignConfig missing. Run Starfall > Setup M3."); enabled = false; return; }
            LevelConfig = Resources.Load<FirstLevelConfig>("FirstLevelConfig");
            if (LevelConfig == null) { Debug.LogError("FirstLevelConfig missing. Run Starfall > Setup M2b."); enabled = false; return; }
            Settings = store.Load(SettingsStore.DefaultLanguage(Application.systemLanguage));
#if !UNITY_EDITOR
            Screen.fullScreenMode = Settings.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
#endif
            Text = new LocalizationService(Settings.language);
            foreach (var error in Text.Validate()) Debug.LogError(error);
            if (store.ReadProblem || Records.ReadProblem || Checkpoints.ReadProblem || Profile.ReadProblem) SaveErrorKey = "save.recovered";
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
            gameObject.AddComponent<LoadoutInterface>().Initialize(this);
            FrontEnd = gameObject.AddComponent<FrontEndInterface>(); FrontEnd.Initialize(this);
            Feedback = gameObject.AddComponent<FeedbackPresenter>(); Feedback.Initialize(this);
            gameObject.AddComponent<ThemeLookController>().Initialize(this);
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
            StartStage(SelectedStage);
        }
        public bool StartStage(int stage)
        {
            if (stage < 1 || stage > 6 || Interface.FirstLaunch) return false;
            if (Adventure?.ResultErrorKey != null) { Adventure.RetryUnlockSave(); if (Adventure.ResultErrorKey != null) return false; }
            var gear = Profile.Prepared();
            var entry = new EntryCheckpoint { stage = stage, runId = System.Guid.NewGuid().ToString("N"), seed = System.Guid.NewGuid().GetHashCode() & int.MaxValue,
                health = config.health + gear.ExtraHealth, loadout = gear.Snapshot() };
            if (!SaveCheckpoint(entry)) return false;
            SelectedStage = stage; FrontEnd?.Close(); BeginEntry(entry); return true;
        }
        public void ReturnToStageSelect() { ReturnToMenu(); FrontEnd?.OpenStages(); }
        public void RequestAdventureStart()
        {
            if (Interface.FirstLaunch) return;
            ModeUI?.HideIntroduction(); FrontEnd?.OpenStages();
        }
        public void CancelNewRun() { NewRunConfirmation = false; Interface.RefreshNow(); }
        public void ConfirmNewRun() { NewRunConfirmation = false; StartAdventure(); }
        public bool SaveCheckpoint(EntryCheckpoint entry)
        {
            bool saved = Checkpoints.Save(entry); CheckpointErrorKey = saved ? null : "checkpoint.writeFailed"; return saved;
        }
        public void EndCheckpoint() { CheckpointErrorKey = Checkpoints.Clear() ? null : "checkpoint.clearFailed"; }
        public bool ContinueAdventure()
        {
            if (!Checkpoints.HasEntry || Interface.FirstLaunch || Context != null && Context.Phase != RunPhase.Complete) return false;
            var entry = Checkpoints.Current; var gear = new LoadoutState(Catalog);
            if (!gear.RestoreSnapshot(entry.loadout)) { CheckpointErrorKey = "checkpoint.invalid"; return false; }
            SelectedStage = entry.stage; entry.runId = System.Guid.NewGuid().ToString("N");
            if (!SaveCheckpoint(entry)) return false;
            FrontEnd?.Close(); BeginEntry(entry); return true;
        }
        void BeginEntry(EntryCheckpoint entry)
        {
            StartMode(GameMode.Adventure, false, entry); NewRunConfirmation = false;
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
            else { int stage = Adventure?.Stage ?? SelectedStage; StartStage(stage); }
        }
        void StartMode(GameMode mode, bool prototype = false, EntryCheckpoint entry = null)
        {
            Settings.introductionSeen = true; SaveSettings(); ModeUI?.HideIntroduction(); FrontEnd?.Close(); Feedback?.Clear();
            DisposeRun(); CreateRoom(mode); Context = new RunContext(mode); if (mode != GameMode.Adventure || prototype) Context.InvalidateRecord();
            Loadout = new LoadoutState(Catalog);
            PendingPassive = null; brokenCover.Clear(); combatRandom = new System.Random(entry?.seed ?? 1729); RunTimes = entry?.completedTimes != null ? new List<long>(entry.completedTimes) : new List<long>();
            var playerObject = new GameObject("Explorer"); playerObject.transform.SetParent(runRoot.transform, false); playerObject.transform.position = Room.Spawn;
            Player = playerObject.AddComponent<ExplorerController>(); Player.Initialize(this, config);
            Projectiles = new GameObject("Projectile Pool").AddComponent<ProjectilePool>(); Projectiles.transform.SetParent(runRoot.transform, false); Projectiles.Game = this;
            Effects = new GameObject("Combat effects pool").AddComponent<CombatEffects>(); Effects.transform.SetParent(runRoot.transform, false); Effects.Initialize(this);
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
                RunId = entry.runId;
                Loadout.RestoreSnapshot(entry.loadout); Player.ApplyStats(); Player.Health.State.RestoreValue(entry.health);
                Context.RestoreCoins(entry.coins); if (!entry.eligible) Context.InvalidateRecord();
                Adventure = new AdventureDirector(this, entry.seed, clock, entry.stage);
            }
            else if (mode == GameMode.Tutorial) Tutorial = new TutorialDirector(this);
            else Training = new TrainingDirector(this);
            Physics2D.SyncTransforms(); Room.RefreshNavigation(Room.Spawn); navigationLeft = .25f;
            cameraFollow.ResetView(); Input.Flush(); Notify(mode == GameMode.Adventure ? "adventure.start" : mode == GameMode.Tutorial ? "tutorial.reenter" : "training.welcome"); Interface?.ResetPanels(); ModeUI?.Close();
        }
        public void SpawnEnemy(Vector2 at, bool ranged, bool elite = false, float strength = 1, EnemyStyle style = EnemyStyle.Basic, bool reward = true)
        {
            if (LivingEnemies >= 12 || !Room.IsClear(at, .4f) || Vector2.Distance(at, Player.Body.position) < 1.2f) return;
            var go = new GameObject(ranged ? "Sentinel" : "Pursuer"); go.transform.SetParent(runRoot.transform, false); go.transform.position = at;
            var enemy = go.AddComponent<PrototypeEnemy>(); enemy.Initialize(this, ranged, elite, strength, style); enemy.GivesReward = reward; enemies.Add(enemy);
        }
        public void Drop(Vector2 at, SupplyKind kind)
        {
            var go = new GameObject("Supply " + kind); go.transform.SetParent(practiceRoot.transform, false); go.transform.position = at;
            go.AddComponent<SupplyPickup>().Initialize(this, kind);
        }
        public void EnemyKilled(PrototypeEnemy enemy)
        {
            if (Context == null || Context.Phase != RunPhase.Combat) return;
            if (!enemy.GivesReward || enemy.Health.LastDamage.Source != Faction.Player) return;
            Context.RegisterKill(); Loadout.AddEnergy(Loadout.PassiveValue("recharge"));
            if (Context.Mode != GameMode.Training && Adventure?.Current.Kind != LevelRoomKind.Challenge) Drop(enemy.transform.position, SupplyKind.Coin);
            if (Context.Mode != GameMode.Adventure) { gameObject.GetComponent<PracticeInterface>()?.RefreshNow(); }
        }
        public int LivingEnemies { get { int count = 0; foreach (var enemy in enemies) if (enemy != null && enemy.Alive) count++; return count; } }
        public void RebuildAdventureRoom(LevelRoomPlan plan)
        {
            ClearEnemies(); Projectiles?.Clear(); Effects?.Clear(); Player?.Health.ClearStatus(); Hazards?.Stop();
            if (builtRoomId != null && practiceRoot != null) { practiceRoot.SetActive(false); roomObjects[builtRoomId] = practiceRoot; }
            if (builtRoomId != null && Room != null) { var missing = new List<Rect>(); foreach (var cover in Room.GetComponentsInChildren<DestructibleCover>(true)) if (!cover.gameObject.activeSelf) missing.Add(cover.Bounds); brokenCover[builtRoomId] = missing; }
            builtRoomId = plan.Id; stations.Clear(); targets.Clear(); Interaction = PickupInteraction = null;
            if (roomObjects.TryGetValue(plan.Id, out var cached)) { practiceRoot = cached; cached.SetActive(true); stations.AddRange(cached.GetComponentsInChildren<PracticeInteractable>()); }
            else { if (practiceRoot != null && builtRoomId == null) Destroy(practiceRoot); practiceRoot = new GameObject("Objects " + plan.Id); practiceRoot.transform.SetParent(runRoot.transform, false); }
            if (Room != null) { Room.gameObject.SetActive(false); Destroy(Room.gameObject); }
            var geometry = new GameObject("Room " + plan.Id); geometry.transform.SetParent(runRoot.transform, false);
            Room = geometry.AddComponent<PrototypeRoom>(); Room.Build(GameMode.Adventure, plan); cameraFollow.ResetView();
            Room.AttachDestructibles(this);
            if (brokenCover.TryGetValue(plan.Id, out var broken)) foreach (var cover in Room.GetComponentsInChildren<DestructibleCover>()) if (broken.Contains(cover.Bounds)) { cover.gameObject.SetActive(false); Room.RemoveCover(cover.Bounds); }
            Surface = null;
            if (plan.Stage > 1 && plan.Kind != LevelRoomKind.Safe && plan.Kind != LevelRoomKind.Shop && !(plan.Stage == 6 && plan.Kind == LevelRoomKind.Boss))
            { Surface = geometry.AddComponent<ThemeEnvironment>(); Surface.Initialize(this, plan.Stage, plan.Kind == LevelRoomKind.Mechanism); }
        }
        public StoneCaptain CreateCaptain(Vector2 at)
        {
            var boss = new GameObject("Stone Captain"); boss.transform.SetParent(runRoot.transform, false); boss.transform.position = at;
            var captain = boss.AddComponent<StoneCaptain>(); captain.Initialize(this); return captain;
        }
        public ThemeBoss CreateThemeBoss(int stage)
        {
            var boss = new GameObject("Theme Boss"); boss.transform.SetParent(runRoot.transform, false); boss.transform.position = new Vector2(5, 0);
            var guardian = boss.AddComponent<ThemeBoss>(); guardian.Initialize(this, stage); return guardian;
        }
        public LateCampaignBoss CreateLateBoss(int stage) {
            var go = new GameObject("Campaign Boss"); go.transform.SetParent(runRoot.transform,false); go.transform.position = new Vector2(5,0);
            var boss = go.AddComponent<LateCampaignBoss>(); boss.Initialize(this,stage); return boss;
        }
        public Damageable CreateEscort()
        {
            var robot = new GameObject("Supply robot"); robot.transform.SetParent(practiceRoot.transform, false); robot.transform.position = new Vector2(-6, -2);
            PrototypeVisuals.Draw(robot.transform, "Robot", Vector2.zero, new Vector2(.8f, .8f), PrototypeVisuals.Teal, 5, "cross");
            PrototypeVisuals.Body(robot, .3f).bodyType = RigidbodyType2D.Kinematic;
            var health = robot.AddComponent<Damageable>(); health.Initialize(Faction.Player, 60, .6f); health.Game = this; return health;
        }
        public void StartEnvironmentSample(int stage)
        {
            if (Context?.Mode != GameMode.Training) return;
            StopEnvironmentSample(); Surface = new GameObject("Training surface").AddComponent<ThemeEnvironment>(); Surface.transform.SetParent(runRoot.transform, false);
            Surface.Initialize(this, stage, true, true); ModeUI.Close();
        }
        public void StopEnvironmentSample() { if (Surface != null) { Surface.Stop(); Destroy(Surface); if (Surface.gameObject.name == "Training surface") Destroy(Surface.gameObject); Surface = null; } }
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
                    if (PendingEquipment != null) CancelEquipmentReplacement();
                    else if (PendingPassive != null) CancelPassive();
                    else if (AdventureUI.ShopOpen) AdventureUI.CloseShop();
                    else if (ModeUI != null && ModeUI.IsOpen) ModeUI.Close();
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
                    bool used = false;
                    if (Input.PickupPressed && PickupInteraction != null) { PickupInteraction.Invoke(); used = true; }
                    else if (Input.InteractPressed && Interaction != null) { Interaction.Invoke(); used = true; }
                    if (!CanAct) return;
                    if (Context.Mode == GameMode.Tutorial)
                    {
                        if (CanAct) Tutorial.Tick(Time.deltaTime); if (!used && CanAct && Input.InteractPressed) Tutorial.TryDoor();
                    }
                    else if (!used && Context.Mode == GameMode.Training && Input.InteractPressed && Vector2.Distance(Player.Body.position, Room.Exit) < 1.8f) ReturnToMenu();
                    else if (!used && Adventure == null && Context.Phase == RunPhase.RoomClear && Input.InteractPressed && Vector2.Distance(Player.Body.position, Room.Exit) < 1.8f)
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
                else if(CanAct) Training?.ResolveSimulation();
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
            if(reason==PauseReason.Map && value) Interface?.RefreshMap();
            Adventure?.SyncTimerPause(); Audio?.Pause(Pause.IsPaused);
            if (reason == PauseReason.Map) Tutorial?.MapChanged(value);
            Input?.Flush(); Player?.Flush(); if (Pause.IsPaused) StopBodies();
        }
        public void Resume() { SetPause(PauseReason.Menu, false); if (Application.isFocused) SetPause(PauseReason.Focus, false); }
        void StopBodies() { Player?.Flush(); foreach (var enemy in enemies) if (enemy != null) enemy.Stop(); }
        public void ReturnToMenu() { if (Adventure?.ResultErrorKey != null) { Adventure.RetryUnlockSave(); if (Adventure.ResultErrorKey != null) return; } ModeUI?.Close(); AdventureUI?.CloseRecords(); CancelEquipmentReplacement(); DisposeRun(); CreateRoom(); cameraFollow.ResetView(); Interface.ResetPanels(); FrontEnd?.OpenMenu(); Feedback?.Clear(); }
        void DisposeRun()
        {
            Pause.Clear(); Time.timeScale = 1; Input?.Flush(); Projectiles?.Clear(); Effects?.Clear(); PendingEquipment = null; pendingEquipmentAction = null; PendingPassive = null; pendingReplacement = null; StopBodies();
            Audio?.Pause(false); Adventure?.StopBosses(); AdventureUI?.CloseRecords(); AdventureUI?.CloseShop(); Surface = null;
            if (runRoot != null) { runRoot.SetActive(false); Destroy(runRoot); }
            enemies.Clear(); Player = null; Projectiles = null; Effects = null; Context = null; notificationLeft = 0;
            Tutorial = null; Training = null; Loadout = null; Hazards = null; Interaction = null; stations.Clear(); targets.Clear(); discardedWeapon = null;
            Adventure = null; roomObjects.Clear(); builtRoomId = null; PickupInteraction = null;
        }
        public void ClearEnemies()
        {
            foreach (var enemy in enemies) if (enemy != null) { enemy.gameObject.SetActive(false); Destroy(enemy.gameObject); }
            enemies.Clear();
        }
        public void ClearPracticeObjects()
        {
            if (Context?.Mode == GameMode.Training) StopEnvironmentSample();
            ClearEnemies(); Projectiles?.Clear(); Effects?.Clear(); Player?.Health.ClearStatus(); Hazards?.Stop(); stations.Clear(); targets.Clear(); Interaction = null; discardedWeapon = null;
            if (practiceRoot != null) { practiceRoot.SetActive(false); Destroy(practiceRoot); }
            practiceRoot = new GameObject("Practice objects"); practiceRoot.transform.SetParent(runRoot.transform, false); ResetPracticeStats();
        }
        public void AddTarget(string id, Vector2 at, bool moving = false, bool armored = false)
        {
            var go = new GameObject(id); go.transform.SetParent(practiceRoot.transform, false); go.transform.position = at;
            var target = go.AddComponent<PracticeTarget>(); target.Initialize(this, id, moving, armored); targets.Add(target);
        }
        public PracticeInteractable AddStation(string id, string key, Vector2 at, System.Action action)
        {
            foreach (var existing in stations) if (existing != null && existing.Id == id) return existing;
            var go = new GameObject(id); go.transform.SetParent(practiceRoot.transform, false); go.transform.position = at;
            var station = go.AddComponent<PracticeInteractable>(); station.Initialize(id, key, action, PrototypeVisuals.Teal); stations.Add(station); return station;
        }
        public void MarkerAt(Vector2 at) => PrototypeVisuals.Draw(practiceRoot.transform, "Tutorial marker", at, new Vector2(.9f, .9f), PrototypeVisuals.Gold, 0, "orb");
        void SelectInteraction()
        {
            Interaction = PickupInteraction = null;
            float bestDistance = float.MaxValue, bestFacing = -2, pickupDistance = float.MaxValue, pickupFacing = -2;
            foreach (var station in stations)
            {
                if (station == null || !station.gameObject.activeInHierarchy || station.Completed) continue;
                Vector2 direction = (Vector2)station.transform.position - Player.Body.position; float distance = direction.sqrMagnitude;
                if (distance > 2.56f || Physics2D.Raycast(Player.Body.position, direction.normalized, direction.magnitude, 1)) continue;
                float facing = Vector2.Dot(Player.Aim, direction.normalized);
                if (station.IsPickup) {
                    if (distance < pickupDistance - .001f || Mathf.Abs(distance - pickupDistance) < .001f && (facing > pickupFacing + .001f || Mathf.Abs(facing - pickupFacing) < .001f && string.CompareOrdinal(station.Id, PickupInteraction?.Id) < 0))
                    { PickupInteraction = station; pickupDistance = distance; pickupFacing = facing; }
                    continue;
                }
                if (distance < bestDistance - .001f || Mathf.Abs(distance - bestDistance) < .001f && (facing > bestFacing + .001f || Mathf.Abs(facing - bestFacing) < .001f && string.CompareOrdinal(station.Id, Interaction?.Id) < 0))
                { Interaction = station; bestDistance = distance; bestFacing = facing; }
            }
        }
        public bool EquipItem(string id, bool gameplayGrant = false)
        {
            if (Adventure != null && !gameplayGrant) Context.InvalidateRecord();
            var definition = Catalog.Find(id); string old = Loadout.SpecialWeapon;
            if (definition != null && definition.kind == ItemKind.Passive && Loadout.Layers(id) == 0 && Loadout.Passives.Count >= 6) {
                RequestPassiveReplacement(id); Notify("item.replace"); return false;
            }
            if (gameplayGrant && definition?.kind == ItemKind.Active && id == Loadout.Active && Loadout.Charges >= definition.maxCharges) { Notify("item.limit"); return false; }
            bool sameActive=gameplayGrant && definition?.kind==ItemKind.Active && id==Loadout.Active;
            if (!(sameActive ? Loadout.RefillActive(id) : Loadout.Equip(id))) { Notify("item.limit"); return false; }
            if (definition.kind == ItemKind.Active && !sameActive) Effects?.ClearActive();
            if (definition.kind == ItemKind.Weapon && id != "pistol" && old != null && old != id)
            {
                Vector2 at = Player.Body.position; string previous = old;
                PracticeInteractable drop = null;
                drop = AddStation("dropped.weapon." + droppedId++, Catalog.Find(old).nameKey, at, () => RequestEquipmentReplacement(previous,()=> { if(drop.Completed || !EquipItem(previous,true)) return false; drop.Complete(); return true; }));
            }
            EquipmentChanged(); Feedback?.Item(id); Audio?.Play(GameSound.Pickup); return true;
        }
        public void ReplaceLoadout(LoadoutState value) { Loadout = value; EquipmentChanged(); }
        public void EquipmentChanged() { Player?.ApplyStats(); ResetPracticeStats(); }
        public void ClearCombatStatus() { Player?.Health.ClearStatus(); foreach(var enemy in enemies) if(enemy!=null) enemy.Health.ClearStatus(); foreach(var target in targets) if(target!=null) target.Health.ClearStatus(); }
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
        public void ToggleMinimal() { Settings.minimalEffects = !Settings.minimalEffects; SaveSettings(); }
        public void ToggleVignette() { Settings.vignette = !Settings.vignette; SaveSettings(); }
        public void ToggleDamageNumbers() { Settings.damageNumbers = !Settings.damageNumbers; SaveSettings(); }
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
