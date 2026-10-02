using System;
using System.Collections.Generic;
using UnityEngine;

namespace Starfall
{
    public sealed class AdventureDirector
    {
        readonly StarfallGame game;
        readonly string attemptId = Guid.NewGuid().ToString("N");
        readonly HashSet<string> activated = new HashSet<string>();
        double challengeStart;
        bool beaconBuilt, sealBuilt;
        public FirstLevelPlan Plan { get; }
        public FirstLevelProgress Progress { get; } = new FirstLevelProgress();
        public LevelRoomPlan Current { get; private set; }
        public LevelTimer Timer { get; }
        public StoneCaptain Boss { get; private set; }
        public ThemeBoss Guardian { get; private set; }
        public Damageable BossHealth => Boss != null ? Boss.Health : Guardian != null ? Guardian.Health : null;
        public Vector2 BossPosition => Boss != null ? (Vector2)Boss.transform.position : Guardian != null ? (Vector2)Guardian.transform.position : Vector2.zero;
        public ShopLedger Shop { get; } = new ShopLedger();
        public Damageable Escort { get; private set; }
        float escortProgress;
        int ambush;
        public bool EscortComplete => escortProgress >= 1;
        public string StageId => Plan.StageId;
        public int Stage => Plan.Stage;
        public string RoomKey(string id) => Plan.RoomKey(id);
        public string TaskKey => Stage == 1 ? "adventure.objective" : "adventure.s" + Stage + ".objective";
        public string ResultErrorKey { get; private set; }
        public ClearResult Result { get; private set; }
        public bool ChallengeFailed { get; private set; }
        public bool RewardSelected { get; private set; }
        public double ChallengeElapsed => Timer.Seconds - challengeStart;
        public string SelectedReward { get; private set; }
        public List<string> Rewards { get; } = new List<string>();
        public AdventureDirector(StarfallGame owner, int seed, Func<double> clock = null, int stage = 1)
        {
            game = owner; Timer = new LevelTimer(clock); Plan = new FirstLevelPlan(seed, stage); Enter("entry"); Timer.Start();
        }
        public void Tick(float delta)
        {
            if (Result != null) return;
            if (Mathf.Abs(Time.timeScale - (game.Pause.IsPaused ? 0 : 1)) > .001f || game.Loadout.Invincible || game.Loadout.InfiniteEnergy || game.Loadout.InfiniteCharges)
                game.Context.InvalidateRecord();
            if (Current.Kind == LevelRoomKind.Challenge && Stage == 3 && !Progress.IsClear(Current.Id) && Escort != null)
            {
                if (Escort.State.Alive && Vector2.Distance(game.Player.Body.position, Escort.transform.position) <= 3)
                {
                    escortProgress = Mathf.Min(1, escortProgress + delta / 17);
                    Escort.transform.position = Vector2.Lerp(new Vector2(-6, -2), new Vector2(6, 2), escortProgress);
                }
                if (escortProgress > .25f && ambush == 0 || escortProgress > .6f && ambush == 1)
                {
                    ambush++; game.SpawnEnemy(new Vector2(6, 4), true, false, 1, EnemyStyle.Frost);
                    game.SpawnEnemy(new Vector2(-5, 4), false, false, 1, EnemyStyle.Flanker);
                }
            }
            if (Current.Kind == LevelRoomKind.Challenge && !Progress.IsClear(Current.Id) &&
                (Timer.Seconds - challengeStart >= ChallengeLimit || Stage == 3 && Escort != null && !Escort.State.Alive))
            {
                ChallengeFailed = true; Progress.Clear(Current.Id); game.ClearEnemies(); game.Projectiles.Clear(); game.Room.OpenDoor(); game.Notify("adventure.challengeFailed");
            }
        }
        public float ChallengeLimit => Stage == 3 ? game.CampaignConfig.escortSeconds : game.LevelConfig.challengeSeconds;
        public void ResolveFrame()
        {
            if (Result != null) return;
            if (!game.Player.Health.State.Alive) { Finish(false); return; }
            if (Current.Kind == LevelRoomKind.Boss)
            {
                if (BossHealth != null && !BossHealth.State.Alive && Progress.CanEnterBoss) Finish(true);
                return;
            }
            if (Stage == 3 && Current.Kind == LevelRoomKind.Challenge && !EscortComplete) return;
            if ((Current.Kind == LevelRoomKind.Combat || Current.Kind == LevelRoomKind.Beacon || Current.Kind == LevelRoomKind.Challenge) &&
                game.LivingEnemies == 0 && Progress.Clear(Current.Id))
            {
                game.Projectiles.Clear(); game.Context.AddCoins(game.LevelConfig.roomCoins); game.Loadout.AddEnergy(game.LevelConfig.roomEnergy);
                game.Player.Health.State.Heal(game.LevelConfig.roomHeal); game.Loadout.RefillCharge(); game.Audio?.Play(GameSound.Clear);
                if (Current.Kind == LevelRoomKind.Challenge && !ChallengeFailed && Progress.Claim("challenge.reward"))
                {
                    if (Stage == 3) { game.Player.Health.State.Heal(40); game.Loadout.AddEnergy(60); }
                    else if (Stage == 2) game.EquipItem("workshop_smg", true);
                    else game.EquipItem("agile", true);
                }
                if (Current.Kind == LevelRoomKind.Beacon) BuildBeacon(); else game.Room.OpenDoor();
                game.Notify(Current.Kind == LevelRoomKind.Beacon ? "adventure.activateBeacon" : "adventure.roomClear");
            }
        }
        void Enter(string id)
        {
            game.SetPause(PauseReason.Loading, true); StopBosses(); if (Boss != null) UnityEngine.Object.Destroy(Boss.gameObject); if (Guardian != null) UnityEngine.Object.Destroy(Guardian.gameObject); Boss = null; Guardian = null; Escort = null;
            Current = Plan.Find(id); beaconBuilt = sealBuilt = false;
            game.RebuildAdventureRoom(Current); game.Loadout.EndRoom(); game.Player.MoveTo(game.Room.Spawn);
            if (Current.Kind == LevelRoomKind.Safe || Current.Kind == LevelRoomKind.Shop) Progress.Clear(id);
            if (!Progress.IsClear(id))
            {
                for (int i = 0; i < Current.Enemies.Length; i++) game.SpawnEnemy(Current.Enemies[i], i % 3 == 1, Current.Kind == LevelRoomKind.Challenge && i == 0, 1,
                    Stage == 1 ? EnemyStyle.Basic : Stage == 2 ? (EnemyStyle)(1 + i % 3) : i % 2 == 0 ? EnemyStyle.Frost : EnemyStyle.Flanker);
                if (Current.Kind == LevelRoomKind.Challenge)
                {
                    challengeStart = Timer.Seconds; escortProgress = 0; ambush = 0;
                    if (Stage == 3) Escort = game.CreateEscort();
                }
                if (Current.Kind == LevelRoomKind.Boss) { if (Stage == 1) Boss = game.CreateCaptain(new Vector2(5, 0)); else Guardian = game.CreateThemeBoss(Stage); }
            }
            if (Current.Kind == LevelRoomKind.Safe) BuildSupplies();
            if (Current.Kind == LevelRoomKind.Mechanism)
            {
                game.Room.OpenDoor(); game.AddStation("seal.relay", Stage == 1 ? "adventure.relay" : "adventure.s" + Stage + ".relay", new Vector2(7, 3), ActivateSeal); sealBuilt = true;
            }
            if (Current.Kind == LevelRoomKind.Beacon && Progress.IsClear(id) && !activated.Contains(id)) BuildBeacon();
            if (CanLeave()) game.Room.OpenDoor(); else game.Room.CloseDoor();
            if (Current.Next != null) { string next = Current.Next; game.AddStation("route.next", RoomKey(next), game.Room.Exit, () => Travel(next)); }
            if (Current.Back != null) { string back = Current.Back; game.AddStation("route.back", RoomKey(back), new Vector2(-9, -4), () => Travel(back)); }
            if (Current.Branch != null) { string branch = Current.Branch; game.AddStation("route.branch", RoomKey(branch), new Vector2(0, -5), () => Travel(branch)); }
            if (Current.Kind == LevelRoomKind.Shop) game.AddStation("shop.open", "shop.title", new Vector2(1, 0), game.AdventureUI.OpenShop);
            if (Stage == 1 && id == "courtyard") game.AddStation("route.challenge", "room.challenge", new Vector2(0, -5), () => Travel("challenge"));
            Physics2D.SyncTransforms(); game.Room.RefreshNavigation(game.Room.Spawn); game.Interface.ResetPanels();
            game.SetPause(PauseReason.Loading, false); game.Notify("adventure.enter");
        }
        void BuildSupplies()
        {
            if (Current.Id == "entry")
            {
                if (Stage > 1) { game.AddStation("entry.info", "checkpoint.entryInfo", new Vector2(1, -2), () => game.Notify("checkpoint.entryInfo")); return; }
                AddItem("shotgun", new Vector2(-4, -4)); AddItem("smg", new Vector2(-1, -4));
                AddItem("medkit", new Vector2(2, -4)); AddItem("shield", new Vector2(5, -4));
            }
            else
            {
                game.AddStation("supply.restore", "adventure.restore", new Vector2(-1, -1), () =>
                {
                    if (!Progress.Claim("supply.restore")) { game.Notify("adventure.claimed"); return; }
                    game.Loadout.Restore(); game.Player.Health.State.Restore(); game.Player.ResetCooldowns(); game.Audio?.Play(GameSound.Pickup); game.Notify("adventure.restored");
                });
            }
        }
        void AddItem(string id, Vector2 at)
        {
            game.AddStation("supply." + id, game.Catalog.Find(id).nameKey, at, () =>
            {
                if (!Progress.Claim("supply." + id)) { game.Notify("adventure.claimed"); return; }
                game.EquipItem(id, true); game.Audio?.Play(GameSound.Pickup);
            });
        }
        void BuildBeacon()
        {
            if (beaconBuilt) return; beaconBuilt = true; string id = Current.Id;
            game.AddStation("beacon." + id, Stage == 1 ? "adventure.beacon" : "adventure.s" + Stage + ".beacon", new Vector2(7, 2), () =>
            {
                if (Current.Id != id || !Progress.Activate(id)) return;
                activated.Add(id); game.Room.OpenDoor(); game.Audio?.Play(GameSound.Pickup); game.Notify("adventure.beaconReady");
            });
        }
        void ActivateSeal()
        {
            if (!sealBuilt || Progress.Beacons != 2) { game.Notify("adventure.needBeacons"); return; }
            if (Progress.Clear("seal")) { game.Room.SetPulse(false, false); game.Room.OpenDoor(); game.Projectiles.Clear(); game.Audio?.Play(GameSound.Clear); game.Notify("adventure.sealReady"); }
        }
        public bool CanLeave() => Current.Kind == LevelRoomKind.Challenge || Progress.IsClear(Current.Id) && (Current.Kind != LevelRoomKind.Beacon || activated.Contains(Current.Id));
        public bool Travel(string id)
        {
            if (Result != null || !game.CanAct || !CanLeave()) { game.Notify("adventure.locked"); return false; }
            if (id != Current.Next && id != Current.Back && id != Current.Branch && !(Stage == 1 && Current.Id == "courtyard" && id == "challenge")) return false;
            if (Stage == 3 && Current.Id == "south" && id == "seal" && Progress.Beacons != 2) { game.Notify("adventure.needBeacons"); return false; }
            if (id == "boss" && !Progress.CanEnterBoss) { game.Notify("adventure.needBeacons"); return false; }
            Enter(id); return true;
        }
        public void ApplyMechanism()
        {
            if (Stage != 1 || Current.Kind != LevelRoomKind.Mechanism || Progress.IsClear(Current.Id) || !game.CanAct) return;
            float phase = (float)(Timer.Seconds % 5); bool active = phase >= 1 && phase < 1.6f;
            game.Room.SetPulse(phase < 1 || active, active);
            if (active && Mathf.Abs(game.Player.Body.position.y) < .65f && Mathf.Abs(game.Player.Body.position.x) > 1.5f)
                game.Player.Health.Receive(new DamageContext(6, Faction.Enemy));
        }
        void Finish(bool success)
        {
            long time = Timer.Stop(); game.Context.EndLevel(success); game.Projectiles.Clear(); StopBosses(); game.Player.Flush();
            if (!success || Stage == 3) game.EndCheckpoint();
            var equipment = new List<string> { "pistol" };
            if (game.Loadout.SpecialWeapon != null) equipment.Add(game.Loadout.SpecialWeapon);
            equipment.Add(game.Loadout.Active ?? "none");
            foreach (var pair in game.Loadout.Passives) equipment.Add(pair.Key + ":" + pair.Value);
            var attempt = new BestRecord { stageId = StageId, timingVersion = game.LevelConfig.timingVersion, balanceVersion = game.LevelConfig.balanceVersion, attemptId = attemptId, milliseconds = time, seed = Plan.Seed, date = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'"), equipment = equipment.ToArray() };
            Result = game.Records.Prepare(attempt, success, game.Context.RecordEligible);
            if (success)
            {
                if (Result.Eligible && !game.Records.Commit(Result)) game.PendingRecords.Add(Result);
                BuildRewards();
            }
            game.Audio?.Play(success ? GameSound.Clear : GameSound.Hurt); game.Interface.RefreshNow(); game.AdventureUI.RefreshNow();
        }
        void BuildRewards()
        {
            var candidates = new List<string>();
            foreach (var item in game.Catalog.items) if (item.kind == ItemKind.Passive && item.implemented && game.Loadout.Layers(item.id) < item.stackLimit && (game.Loadout.Layers(item.id) > 0 || game.Loadout.Passives.Count < 6)) candidates.Add(item.id);
            var random = new System.Random(unchecked(Plan.Seed ^ 104729));
            while (Rewards.Count < 3 && candidates.Count > 0) { int at = random.Next(candidates.Count); Rewards.Add(candidates[at]); candidates.RemoveAt(at); }
            string[] alternatives = { "coins", "heal", "energy" }; for (int i = 0; Rewards.Count < 3; i++) Rewards.Add(alternatives[i]);
        }
        public bool SelectReward(string id)
        {
            if (Result == null || !Result.Success || RewardSelected || !Rewards.Contains(id)) return false;
            if (Stage < 3)
            {
                // Prepare the reward and next entry together. A failed write grants neither.
                var gear = game.Loadout.Copy(); float health = game.Player.Health.State.Health; int coins = game.Context.Coins;
                if (id == "coins") coins += 20;
                else if (id == "heal") health = Mathf.Min(game.Player.Health.State.Maximum, health + 30);
                else if (id == "energy") gear.AddEnergy(40);
                else if (!gear.Equip(id)) return false;
                health += Mathf.Max(0, gear.ExtraHealth - game.Loadout.ExtraHealth);
                var entry = new EntryCheckpoint { stage = Stage + 1, runId = game.RunId, seed = new System.Random(unchecked(Plan.Seed ^ 49979687)).Next(), coins = coins,
                    health = health, eligible = game.Context.RecordEligible, loadout = gear.Snapshot() };
                if (!game.SaveCheckpoint(entry)) { ResultErrorKey = "checkpoint.rewardFailed"; return false; }
            }
            if (id == "coins") game.Context.AddCompletedCoins(20);
            else if (id == "heal") game.Player.Health.State.Heal(30);
            else if (id == "energy") game.Loadout.AddEnergy(40);
            else if (!game.EquipItem(id, true)) return false;
            RewardSelected = true; SelectedReward = id; ResultErrorKey = null; game.Audio?.Play(GameSound.Pickup); return true;
        }
        public bool AdvanceStage() => Stage < 3 && RewardSelected && Result != null && Result.Success && game.Checkpoints.HasEntry && game.Checkpoints.Current.stage == Stage + 1 && game.ContinueAdventure();
        public void StopBosses() { Boss?.Stop(); Guardian?.Stop(); }
        public string[] ShopItems => new[] { "heal", "energy", Stage == 2 ? "rapid" : "vitality", Stage == 2 ? "shotgun" : "smg" };
        public int Price(string id) => id == "heal" ? game.CampaignConfig.healPrice : id == "energy" ? game.CampaignConfig.energyPrice : game.Catalog.Find(id).kind == ItemKind.Passive ? game.CampaignConfig.passivePrice : game.CampaignConfig.weaponPrice;
        public bool CanBuy(string id)
        {
            if (Array.IndexOf(ShopItems, id) < 0 || Shop.Sold(id) || game.Context.Coins < Price(id)) return false;
            if (id == "heal") return game.Player.Health.State.Health < game.Player.Health.State.Maximum;
            if (id == "energy") return game.Loadout.Energy < 100;
            var item = game.Catalog.Find(id);
            if (item.kind == ItemKind.Weapon) return game.Loadout.SpecialWeapon != id;
            return game.Loadout.Layers(id) < item.stackLimit && (game.Loadout.Layers(id) > 0 || game.Loadout.Passives.Count < 6);
        }
        public bool Buy(string id)
        {
            if (Result != null || Current.Kind != LevelRoomKind.Shop || !game.AdventureUI.ShopOpen || !game.Player.Health.State.Alive || (game.Pause.Reasons & ~PauseReason.Shop) != 0 || Array.IndexOf(ShopItems, id) < 0) return false;
            if (!CanBuy(id)) { game.Notify("shop.failed"); return false; }
            bool bought = Shop.Buy(id, Price(id), game.Context, () => id == "heal" ? game.Player.Health.State.Heal(game.CampaignConfig.shopHeal) : id == "energy" ? game.Loadout.AddEnergy(game.CampaignConfig.shopEnergy) : game.EquipItem(id, true));
            game.Notify(bought ? "shop.bought" : "shop.failed"); return bought;
        }
        public void SyncTimerPause() => Timer.Exclude((game.Pause.Reasons & (PauseReason.Menu | PauseReason.Map | PauseReason.Focus | PauseReason.Settings | PauseReason.Loading)) != 0);
    }
}
