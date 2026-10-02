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
        public ClearResult Result { get; private set; }
        public bool ChallengeFailed { get; private set; }
        public bool RewardSelected { get; private set; }
        public double ChallengeElapsed => Timer.Seconds - challengeStart;
        public string SelectedReward { get; private set; }
        public List<string> Rewards { get; } = new List<string>();
        public AdventureDirector(StarfallGame owner, int seed, Func<double> clock = null)
        {
            game = owner; Timer = new LevelTimer(clock); Plan = new FirstLevelPlan(seed); Enter("entry"); Timer.Start();
        }
        public void Tick(float delta)
        {
            if (Result != null) return;
            if (Mathf.Abs(Time.timeScale - (game.Pause.IsPaused ? 0 : 1)) > .001f || game.Loadout.Invincible || game.Loadout.InfiniteEnergy || game.Loadout.InfiniteCharges)
                game.Context.InvalidateRecord();
            if (Current.Kind == LevelRoomKind.Challenge && !Progress.IsClear(Current.Id) && Timer.Seconds - challengeStart >= game.LevelConfig.challengeSeconds)
            {
                ChallengeFailed = true; Progress.Clear(Current.Id); game.ClearEnemies(); game.Projectiles.Clear(); game.Room.OpenDoor(); game.Notify("adventure.challengeFailed");
            }
        }
        public void ResolveFrame()
        {
            if (Result != null) return;
            if (!game.Player.Health.State.Alive) { Finish(false); return; }
            if (Current.Kind == LevelRoomKind.Boss)
            {
                if (Boss != null && !Boss.Health.State.Alive && Progress.CanEnterBoss) Finish(true);
                return;
            }
            if ((Current.Kind == LevelRoomKind.Combat || Current.Kind == LevelRoomKind.Beacon || Current.Kind == LevelRoomKind.Challenge) &&
                game.LivingEnemies == 0 && Progress.Clear(Current.Id))
            {
                game.Projectiles.Clear(); game.Context.AddCoins(game.LevelConfig.roomCoins); game.Loadout.AddEnergy(game.LevelConfig.roomEnergy);
                game.Player.Health.State.Heal(game.LevelConfig.roomHeal); game.Loadout.RefillCharge(); game.Audio?.Play(GameSound.Clear);
                if (Current.Kind == LevelRoomKind.Challenge && !ChallengeFailed && Progress.Claim("challenge.reward")) game.EquipItem("agile", true);
                if (Current.Kind == LevelRoomKind.Beacon) BuildBeacon(); else game.Room.OpenDoor();
                game.Notify(Current.Kind == LevelRoomKind.Beacon ? "adventure.activateBeacon" : "adventure.roomClear");
            }
        }
        void Enter(string id)
        {
            game.SetPause(PauseReason.Loading, true); Boss?.Stop(); if (Boss != null) UnityEngine.Object.Destroy(Boss.gameObject); Boss = null;
            Current = Plan.Find(id); beaconBuilt = sealBuilt = false;
            game.RebuildAdventureRoom(Current); game.Loadout.EndRoom(); game.Player.MoveTo(game.Room.Spawn);
            if (Current.Kind == LevelRoomKind.Safe) Progress.Clear(id);
            if (!Progress.IsClear(id))
            {
                for (int i = 0; i < Current.Enemies.Length; i++) game.SpawnEnemy(Current.Enemies[i], i % 3 == 1, Current.Kind == LevelRoomKind.Challenge && i == 0);
                if (Current.Kind == LevelRoomKind.Challenge) challengeStart = Timer.Seconds;
                if (Current.Kind == LevelRoomKind.Boss) Boss = game.CreateCaptain(new Vector2(5, 0));
            }
            if (Current.Kind == LevelRoomKind.Safe) BuildSupplies();
            if (Current.Kind == LevelRoomKind.Mechanism)
            {
                game.Room.OpenDoor(); game.AddStation("seal.relay", "adventure.relay", new Vector2(7, 3), ActivateSeal); sealBuilt = true;
            }
            if (Current.Kind == LevelRoomKind.Beacon && Progress.IsClear(id) && !activated.Contains(id)) BuildBeacon();
            if (CanLeave()) game.Room.OpenDoor(); else game.Room.CloseDoor();
            if (Current.Next != null) { string next = Current.Next; game.AddStation("route.next", "room." + next, game.Room.Exit, () => Travel(next)); }
            if (Current.Back != null) { string back = Current.Back; game.AddStation("route.back", "room." + back, new Vector2(-9, -4), () => Travel(back)); }
            if (id == "courtyard") game.AddStation("route.challenge", "room.challenge", new Vector2(0, -5), () => Travel("challenge"));
            Physics2D.SyncTransforms(); game.Room.RefreshNavigation(game.Room.Spawn); game.Interface.ResetPanels();
            game.SetPause(PauseReason.Loading, false); game.Notify("adventure.enter");
        }
        void BuildSupplies()
        {
            if (Current.Id == "entry")
            {
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
            game.AddStation("beacon." + id, "adventure.beacon", new Vector2(7, 2), () =>
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
            if (id != Current.Next && id != Current.Back && !(Current.Id == "courtyard" && id == "challenge")) return false;
            if (id == "boss" && !Progress.CanEnterBoss) { game.Notify("adventure.needBeacons"); return false; }
            Enter(id); return true;
        }
        public void ApplyMechanism()
        {
            if (Current.Kind != LevelRoomKind.Mechanism || Progress.IsClear(Current.Id) || !game.CanAct) return;
            float phase = (float)(Timer.Seconds % 5); bool active = phase >= 1 && phase < 1.6f;
            game.Room.SetPulse(phase < 1 || active, active);
            if (active && Mathf.Abs(game.Player.Body.position.y) < .65f && Mathf.Abs(game.Player.Body.position.x) > 1.5f)
                game.Player.Health.Receive(new DamageContext(6, Faction.Enemy));
        }
        void Finish(bool success)
        {
            long time = Timer.Stop(); game.Context.EndLevel(success); game.Projectiles.Clear(); Boss?.Stop(); game.Player.Flush();
            var equipment = new List<string> { "pistol" };
            if (game.Loadout.SpecialWeapon != null) equipment.Add(game.Loadout.SpecialWeapon);
            equipment.Add(game.Loadout.Active ?? "none");
            foreach (var pair in game.Loadout.Passives) equipment.Add(pair.Key + ":" + pair.Value);
            var attempt = new BestRecord { stageId = "gardens", timingVersion = game.LevelConfig.timingVersion, balanceVersion = game.LevelConfig.balanceVersion, attemptId = attemptId, milliseconds = time, seed = Plan.Seed, date = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'"), equipment = equipment.ToArray() };
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
            if (id == "coins") game.Context.AddCompletedCoins(20);
            else if (id == "heal") game.Player.Health.State.Heal(30);
            else if (id == "energy") game.Loadout.AddEnergy(40);
            else if (!game.EquipItem(id, true)) return false;
            RewardSelected = true; SelectedReward = id; game.Audio?.Play(GameSound.Pickup); return true;
        }
        public void SyncTimerPause() => Timer.Exclude((game.Pause.Reasons & (PauseReason.Menu | PauseReason.Map | PauseReason.Focus | PauseReason.Settings | PauseReason.Loading)) != 0);
    }
}
