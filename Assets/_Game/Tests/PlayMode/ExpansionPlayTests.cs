#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace Starfall.Tests
{
    public sealed partial class CampaignPlayTests
    {
        IEnumerator ReachLateBoss(int stage) {
            StartFixture(stage); game.Loadout.Invincible=true; yield return Interact("route.next");
            foreach(string id in stage==4 || stage==5 ? new[]{"courtyard","north","crossing","middle","south"} : new[]{"courtyard","north","crossing","south"}) { Assert.AreEqual(id,game.Adventure.Current.Id); yield return ClearRoom(); if(game.Adventure.Current.Kind==LevelRoomKind.Beacon) yield return Interact("beacon."+id); yield return Interact("route.next"); }
            yield return Interact("seal.relay"); yield return Interact("route.next"); yield return Interact("supply.restore"); yield return Interact("route.next"); Assert.IsNotNull(game.Adventure.FinalGuardian);
        }
        [UnityTest] public IEnumerator M4AllWeaponsUseRealInputAndActiveChargesAndClearAtReset() {
            game.StartTraining(); yield return null; Place(new Vector2(-7,0)); game.AddTarget("m4.test",new Vector2(-3,0)); Physics2D.SyncTransforms();
            foreach(string id in new[]{"pistol","shotgun","smg","crossbow","launcher","arc"}) {
                game.EquipItem(id); game.ResetPracticeStats();
                InputSystem.QueueStateEvent(mouse,new MouseState {position=game.GameCamera.WorldToScreenPoint(new Vector2(-3,0)),buttons=1});
                if(id=="crossbow") { yield return new WaitForSeconds(.35f); Assert.AreEqual(0,game.PracticeStats.Total); }
                yield return new WaitForSeconds(2.2f); InputSystem.QueueStateEvent(mouse,new MouseState()); yield return null;
                Assert.Greater(game.PracticeStats.Total,0,"Weapon did not damage target: "+id); game.Projectiles.Clear(); game.Effects.Clear();
            }
            game.Loadout.InfiniteCharges=false;
            foreach(string id in new[]{"medkit","shield","slow","shock","decoy","grenade"}) {
                game.EquipItem(id); if(id=="medkit") game.Player.Health.State.Damage(50,Time.timeAsDouble,false,0);
                InputSystem.QueueStateEvent(mouse,new MouseState {position=game.GameCamera.WorldToScreenPoint(new Vector2(-3,0))}); yield return Press(Key.Q);
                Assert.AreEqual(1,game.Loadout.Charges,id); Assert.Greater(game.Loadout.ActiveCooldown,0,id);
                yield return Press(Key.Q); Assert.AreEqual(1,game.Loadout.Charges,"Cooldown was bypassed");
                game.Training.Reset(); Assert.AreEqual(0,game.Effects.ActiveCount); Assert.AreEqual(0,game.Projectiles.ActiveCount); Assert.AreEqual(0,game.Loadout.ShieldLeft); Place(new Vector2(-7,0));
            }
            game.ReturnToMenu(); game.StartAdventure(); Assert.IsFalse(game.Loadout.Invincible); Assert.IsFalse(game.Loadout.InfiniteEnergy); Assert.IsFalse(game.Loadout.InfiniteCharges);
        }
        [UnityTest] public IEnumerator M4CrossbowSlowComboBoostsPiercingAndShotgunShieldComboKeepsDodgeRules() {
            game.StartTraining(); yield return null; Place(new Vector2(-7,0)); game.EquipItem("crossbow"); game.EquipItem("controlled"); game.EquipItem("slow");
            game.AddTarget("m4.first",new Vector2(-4,0)); game.AddTarget("m4.second",new Vector2(-2,0)); Physics2D.SyncTransforms();
            InputSystem.QueueStateEvent(mouse,new MouseState {position=game.GameCamera.WorldToScreenPoint(new Vector2(-2,0))}); yield return Press(Key.Q); yield return null;
            game.ResetPracticeStats(); InputSystem.QueueStateEvent(mouse,new MouseState {position=game.GameCamera.WorldToScreenPoint(new Vector2(-2,0)),buttons=1});
            yield return new WaitForSeconds(.95f); InputSystem.QueueStateEvent(mouse,new MouseState()); yield return null;
            Assert.AreEqual(169,game.PracticeStats.Total,.2f,"Two pierced targets should each receive 65 * 1.3 under slow");
            game.Training.Reset(true); Place(new Vector2(-7,0)); game.EquipItem("shotgun"); game.EquipItem("agile"); game.EquipItem("shield");
            yield return Press(Key.Q); Assert.Greater(game.Loadout.ShieldLeft,0); game.Loadout.Invincible=false; game.Player.ApplyStats();
            Assert.IsFalse(game.Player.Health.Receive(new DamageContext(20,Faction.Enemy))); yield return Press(Key.Space); Assert.IsTrue(game.Player.IsDodging); Assert.Less(game.Player.DodgeCooldown,.86f);
        }
        [UnityTest] public IEnumerator M4ArcRechargeDecoyComboAttractsTargetsAndOnlyRewardsOriginalKills() {
            game.StartTraining(); yield return null; Place(new Vector2(-7,0)); game.EquipItem("arc"); game.EquipItem("recharge"); game.EquipItem("decoy"); game.Loadout.InfiniteEnergy=false; game.Loadout.SpendEnergy(60);
            game.SpawnEnemy(new Vector2(-3,0),false); game.SpawnEnemy(new Vector2(-1,0),false); game.SpawnEnemy(new Vector2(1,0),false); Physics2D.SyncTransforms();
            InputSystem.QueueStateEvent(mouse,new MouseState {position=game.GameCamera.WorldToScreenPoint(new Vector2(-3,0))}); yield return Press(Key.Q); Assert.IsNotNull(game.Effects.Decoy);
            var first=game.Enemies[0]; Vector2 before=first.transform.position; yield return new WaitForSeconds(.3f); Assert.Less(Vector2.Distance(first.transform.position,game.Effects.Decoy.transform.position),Vector2.Distance(before,game.Effects.Decoy.transform.position));
            Assert.AreEqual(2,game.Effects.Chain(first.Health,1000,3,4)); Assert.AreEqual(54,game.Loadout.Energy,.1f);
            first.Health.Receive(new DamageContext(1000,Faction.Player,"arc")); Assert.AreEqual(61,game.Loadout.Energy,.1f); Assert.AreEqual(3,game.Context.Kills);
            game.SpawnEnemy(new Vector2(2,2),false,false,1,EnemyStyle.Sporelet,false); var child=game.Enemies[game.Enemies.Count-1]; child.Health.Receive(new DamageContext(1000,Faction.Player,"arc.chain",1)); Assert.AreEqual(61,game.Loadout.Energy,.1f); Assert.AreEqual(3,game.Context.Kills);
            Assert.IsFalse(game.Player.Health.Receive(new DamageContext(100,Faction.Enemy,"recursive",3))); game.Training.Reset(); Assert.IsNull(game.Effects.Decoy);
        }
        [UnityTest] public IEnumerator M4SporeSplitsRegisterBeforeOpeningAndDestructibleCoverUpdatesNavigation() {
            StartFixture(4); yield return Interact("route.next"); int original=game.LivingEnemies; PrototypeEnemy spore=null; foreach(var enemy in game.Enemies) if(enemy.Style==EnemyStyle.Spore) { spore=enemy; break; }
            Assert.IsNotNull(spore); spore.Health.Receive(new DamageContext(1000,Faction.Player)); Assert.GreaterOrEqual(game.LivingEnemies,original); Assert.IsFalse(game.Room.DoorOpen);
            int walls=game.Room.Walls.Count; var cover=game.Room.GetComponentInChildren<DestructibleCover>(); Assert.IsNotNull(cover); Rect bounds=cover.Bounds; cover.GetComponent<Damageable>().Receive(new DamageContext(1000,Faction.Player)); Assert.AreEqual(walls-1,game.Room.Walls.Count); Assert.IsTrue(game.Room.IsClear(bounds.center,.3f));
            game.Loadout.Invincible=true; yield return ClearRoom(); Assert.IsTrue(game.Room.DoorOpen); int coins=game.Context.Coins;
            yield return Interact("route.next"); yield return ClearRoom(); yield return Interact("beacon.north"); yield return Interact("route.back"); Assert.AreEqual(coins+game.LevelConfig.roomCoins,game.Context.Coins);
        }
        [UnityTest] public IEnumerator M4OptionalRoundsBankRewardsAndGridRewardAndFinalPreparationAreOnceOnly() {
            StartFixture(4); game.Loadout.Invincible=true; yield return Interact("route.next"); yield return ClearRoom(); yield return Interact("route.next"); yield return ClearRoom(); yield return Interact("beacon.north"); yield return Interact("route.next"); yield return ClearRoom(); yield return Interact("route.branch");
            Assert.AreEqual("challenge",game.Adventure.Current.Id); yield return Interact("spore.start"); Assert.IsTrue(game.Adventure.RoundActive); Assert.IsFalse(game.Adventure.Travel("crossing")); yield return ClearRoom(); Assert.AreEqual(1,game.Adventure.SporeRound); int coins=game.Context.Coins;
            yield return Interact("spore.stop"); yield return Interact("route.back"); yield return Interact("route.branch"); yield return Interact("spore.start"); Assert.IsFalse(game.Adventure.RoundActive); Assert.AreEqual(coins,game.Context.Coins); Assert.AreEqual(1,game.Adventure.Progress.Beacons);
            StartFixture(4); game.Loadout.Invincible=true; yield return Interact("route.next"); yield return ClearRoom(); yield return Interact("route.next"); yield return ClearRoom(); yield return Interact("beacon.north"); yield return Interact("route.next"); yield return ClearRoom(); yield return Interact("route.branch");
            int bank=game.Context.Coins; for(int round=1;round<=3;round++) { yield return Interact("spore.start"); yield return ClearRoom(); Assert.AreEqual(round,game.Adventure.SporeRound); Assert.AreEqual(bank+12*round*(round+1)/2,game.Context.Coins); }
            Assert.AreEqual(1,game.Loadout.Layers("controlled")); Assert.AreEqual("crossbow",game.Loadout.SpecialWeapon); yield return Interact("spore.start"); Assert.IsFalse(game.Adventure.RoundActive);
            double clock=0; StarfallGame.EditorAdventureClock=()=>clock; StartFixture(5); game.Loadout.Invincible=true;
            yield return Interact("route.next"); yield return ClearRoom(); yield return Interact("route.next"); yield return ClearRoom(); yield return Interact("beacon.north"); yield return Interact("route.next"); yield return ClearRoom(); yield return Interact("route.branch");
            yield return ClearRoom(); Assert.IsFalse(game.Adventure.Progress.IsClear("challenge")); clock=19; yield return null; yield return null; Assert.IsTrue(game.Adventure.Progress.IsClear("challenge")); Assert.AreEqual(3,game.Adventure.BonusRewards.Count); Assert.IsTrue(game.Adventure.ChooseBonus(game.Adventure.BonusRewards[0])); Assert.IsFalse(game.Adventure.ChooseBonus(game.Adventure.BonusRewards[1]));
            StartFixture(6); game.Loadout.Invincible=true; game.EquipItem("rapid",true); yield return Interact("route.next"); yield return ClearRoom(); yield return Interact("route.next"); yield return ClearRoom(); yield return Interact("beacon.north"); yield return Interact("route.next"); yield return ClearRoom(); yield return Interact("route.next"); yield return ClearRoom(); yield return Interact("beacon.south"); yield return Interact("route.next"); yield return Interact("seal.relay"); yield return Interact("route.next"); yield return Interact("route.branch");
            Assert.IsTrue(game.Adventure.RerollPassive("rapid")); Assert.AreEqual(0,game.Loadout.Layers("rapid")); Assert.AreEqual(1,game.Loadout.Passives.Count); Assert.IsFalse(game.Adventure.RerollPassive("rapid"));
        }
        [UnityTest] public IEnumerator M4ThreeNewBossesWarnRecoverAndFinalTransitionTimesOutWithDeathPriority() {
            foreach(int stage in new[]{4,5,6}) {
                yield return ReachLateBoss(stage); var boss=game.Adventure.FinalGuardian; var moves=new HashSet<int>();
                float until=Time.realtimeSinceStartup+11; while(Time.realtimeSinceStartup<until) { moves.Add(boss.Move); yield return null; } Assert.AreEqual(3,moves.Count);
                int shots=game.Projectiles.ActiveCount; game.SetPause(PauseReason.Focus,true); yield return new WaitForSecondsRealtime(.12f); Assert.AreEqual(shots,game.Projectiles.ActiveCount); game.SetPause(PauseReason.Focus,false);
                if(stage==6) { boss.Health.Receive(new DamageContext(400,Faction.Player)); yield return null; Assert.IsTrue(boss.Transitioning); Assert.IsTrue(boss.Health.Invulnerable); yield return new WaitForSeconds(1.1f); Assert.IsFalse(boss.Health.Invulnerable); Assert.AreEqual(2,boss.Phase); boss.Health.Receive(new DamageContext(350,Faction.Player)); yield return null; Assert.IsTrue(boss.Transitioning); yield return new WaitForSeconds(1.1f); Assert.AreEqual(3,boss.Phase); Assert.IsFalse(boss.Health.Invulnerable); }
                boss.Health.Receive(new DamageContext(10000,Faction.Player)); game.Player.Health.State.Damage(10000,Time.timeAsDouble,false,0); yield return null;
                Assert.AreEqual(RunPhase.Dead,game.Context.Phase); Assert.IsFalse(game.Adventure.Result.Success); Assert.IsFalse(game.Checkpoints.HasEntry); Assert.AreEqual(0,game.LivingEnemies); Assert.AreEqual(0,game.Projectiles.ActiveCount); Assert.AreEqual(0,game.Effects.ActiveCount); Assert.AreEqual(0,game.Records.Book.bests.Count);
            }
        }
        [UnityTest] public IEnumerator M4FullSlotsReplacementDiscardAndAtomicShopReplacement() {
            game.StartTraining(); yield return null; foreach(string id in new[]{"rapid","agile","pierce","bounce","magnet","vitality"}) game.EquipItem(id);
            Assert.IsFalse(game.EquipItem("critical")); Assert.AreEqual("critical",game.PendingPassive); int types=game.Loadout.Passives.Count; game.CancelPassive(); Assert.AreEqual(types,game.Loadout.Passives.Count); Assert.AreEqual(0,game.Loadout.Layers("critical"));
            game.EquipItem("critical"); Assert.IsTrue(game.ReplacePassive("bounce")); Assert.AreEqual(6,game.Loadout.Passives.Count); Assert.AreEqual(1,game.Loadout.Layers("critical"));
            double clock=0; StarfallGame.EditorAdventureClock=()=>clock;
            StartFixture(4); game.Loadout.Invincible=true; foreach(string id in new[]{"rapid","agile","pierce","bounce","magnet","vitality"}) game.EquipItem(id,true);
            yield return Interact("route.next"); yield return ClearRoom(); yield return Interact("route.next"); yield return ClearRoom(); yield return Interact("beacon.north"); yield return Interact("route.branch"); yield return Interact("shop.open");
            int coins=game.Context.Coins; Assert.IsFalse(game.Adventure.Buy("controlled")); Assert.AreEqual(coins,game.Context.Coins); game.CancelPassive(); Assert.AreEqual(coins,game.Context.Coins); Assert.IsFalse(game.Adventure.Shop.Sold("controlled"));
            game.Adventure.Buy("controlled"); long before=game.Adventure.Timer.Milliseconds; clock+=10; Assert.AreEqual(before+10000,game.Adventure.Timer.Milliseconds,"Shop replacement deliberation must still count");
            yield return null; foreach(string language in new[]{"en","zh-CN"}) { game.SetLanguage(language); yield return null; foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(2560,1440),new Vector2Int(1280,960)}) CaptureM4("replacement-"+language,size.x,size.y); }
            Assert.IsTrue(game.ReplacePassive("bounce")); Assert.AreEqual(coins-game.CampaignConfig.passivePrice,game.Context.Coins); Assert.IsTrue(game.Adventure.Shop.Sold("controlled")); Assert.IsFalse(game.Adventure.Buy("controlled"));
        }
        [UnityTest] public IEnumerator M4PassiveRicochetBlastCriticalDefenseAndFlawlessHaveRealEffects() {
            game.StartTraining(); yield return null; Place(new Vector2(-9,0)); game.EquipItem("bounce"); game.AddTarget("ricochet",new Vector2(-7,0)); Physics2D.SyncTransforms(); game.ResetPracticeStats();
            game.Projectiles.Spawn(new Vector2(-9,0),Vector2.left,Faction.Player,20,12); yield return new WaitForSeconds(.55f); Assert.AreEqual(20,game.PracticeStats.Total,.1f,"Wall ricochet must damage the target behind the original shot");
            game.Training.Reset(true); Place(new Vector2(-7,0)); game.EquipItem("pierce"); game.AddTarget("pierce-a",new Vector2(-4,0)); game.AddTarget("pierce-b",new Vector2(-2,0)); Physics2D.SyncTransforms(); game.ResetPracticeStats();
            game.Projectiles.Spawn(new Vector2(-6,0),Vector2.right,Faction.Player,20,16); yield return new WaitForSeconds(.5f); Assert.AreEqual(40,game.PracticeStats.Total,.1f);
            game.Training.Reset(true); Place(new Vector2(-7,0)); game.AddTarget("blast-edge",new Vector2(3.25f,0)); Physics2D.SyncTransforms(); game.ResetPracticeStats(); game.Effects.Explosion(Vector2.zero,50,game.Catalog.Find("grenade").radius,.1f,"grenade"); yield return new WaitForSeconds(.2f); Assert.AreEqual(0,game.PracticeStats.Total);
            game.EquipItem("blast"); game.EquipItem("blast"); game.Effects.Explosion(Vector2.zero,50,game.Catalog.Find("grenade").radius*game.Loadout.BlastMultiplier,.1f,"grenade"); yield return new WaitForSeconds(.2f); Assert.AreEqual(50,game.PracticeStats.Total,.1f);
            game.EquipItem("critical"); game.EquipItem("critical"); PracticeTarget target=null; foreach(var candidate in UnityEngine.Object.FindObjectsByType<PracticeTarget>(FindObjectsSortMode.None)) if(candidate.Id=="training.static") target=candidate; Assert.IsNotNull(target); int critical=0;
            for(int i=0;i<100;i++) { float hp=target.Health.State.Health; target.Health.Receive(new DamageContext(10,Faction.Player)); if(hp-target.Health.State.Health>10.1f) critical++; }
            Assert.Greater(critical,0); Assert.Less(critical,100);
            game.Training.Reset(true); game.EquipItem("lowhealth"); game.EquipItem("lowhealth"); game.Loadout.Invincible=false; game.Player.ApplyStats(); game.Player.Health.State.RestoreValue(35); Assert.IsTrue(game.Player.Health.Receive(new DamageContext(25,Faction.Enemy))); Assert.AreEqual(20,game.Player.Health.State.Health,.01f);
            game.Training.Reset(true); game.EquipItem("flawless"); game.Player.Health.State.RestoreValue(80); game.Training.SetCount(1); game.Training.Simulate("chaser"); yield return ClearRoom(); Assert.AreEqual(88,game.Player.Health.State.Health,.1f); yield return null; Assert.AreEqual(88,game.Player.Health.State.Health,.1f);
        }
        [UnityTest] public IEnumerator M4GridWarnsBeforeDamagePausesAndAlwaysLeavesSafeRoutes() {
            game.StartTraining(); yield return null; game.Loadout.Invincible=false; game.Player.ApplyStats(); game.StartEnvironmentSample(5); Place(new Vector2(0,2.5f));
            Assert.IsFalse(game.Surface.Electrified); Assert.IsFalse(game.Surface.Dangerous(new Vector2(0,0))); Assert.IsFalse(game.Surface.Dangerous(new Vector2(8.5f,2.5f))); Assert.IsFalse(game.Surface.Dangerous(new Vector2(-8,-4)));
            yield return new WaitForSeconds(4.4f); Assert.IsTrue(game.Surface.Warning); Assert.AreEqual(100,game.Player.Health.State.Health); float countdown=game.Surface.Countdown;
            game.SetPause(PauseReason.Menu,true); yield return new WaitForSecondsRealtime(.2f); Assert.AreEqual(countdown,game.Surface.Countdown,.001f); game.SetPause(PauseReason.Menu,false); yield return new WaitForSeconds(.95f); Assert.IsTrue(game.Surface.Electrified); Assert.Less(game.Player.Health.State.Health,100);
            game.Training.Reset(); Assert.IsNull(game.Surface); Assert.AreEqual(100,game.Player.Health.State.Health);
        }
        [UnityTest] public IEnumerator M4BilingualScreensCoverCatalogNewThemesFinalAndFourResolutions() {
            int[,] sizes={{1280,720},{1920,1080},{2560,1440},{1280,960}};
            game.StartTraining(); yield return null;
            foreach(string language in new[]{"en","zh-CN"}) { game.SetLanguage(language);
                foreach(ItemKind kind in new[]{ItemKind.Weapon,ItemKind.Active,ItemKind.Passive}) { game.ModeUI.OpenEquipment(kind); yield return null; for(int i=0;i<4;i++) CaptureM4("catalog-"+kind+"-"+language,sizes[i,0],sizes[i,1]); game.ModeUI.Close(); }
                game.ModeUI.Open(PracticePanel.Simulation); yield return null; CaptureM4("simulation-"+language,1280,720); game.ModeUI.Close(); game.ModeUI.Open(PracticePanel.Environment); yield return null; CaptureM4("environment-"+language,1280,720); game.ModeUI.Close();
            }
            foreach(int stage in new[]{4,5,6}) {
                StartFixture(stage); yield return null; foreach(string language in new[]{"en","zh-CN"}) { game.SetLanguage(language); for(int i=0;i<4;i++) CaptureM4("entry-s"+stage+"-"+language,sizes[i,0],sizes[i,1]); }
                yield return ReachLateBoss(stage); foreach(string language in new[]{"en","zh-CN"}) { game.SetLanguage(language); CaptureM4("boss-s"+stage+"-"+language,1920,1080); }
                game.Adventure.BossHealth.Receive(new DamageContext(10000,Faction.Player)); yield return null; foreach(string language in new[]{"en","zh-CN"}) { game.SetLanguage(language); for(int i=0;i<4;i++) CaptureM4("result-s"+stage+"-"+language,sizes[i,0],sizes[i,1]); }
            }
        }
        void CaptureM4(string name,int width,int height) { Capture("m4-"+name+"-"+width+"x"+height,width,height); string directory=Path.Combine(Application.dataPath,"../Logs/M4-Screens"); Directory.CreateDirectory(directory); File.Copy(Path.Combine(Application.dataPath,"../Logs/M3-Screens/m4-"+name+"-"+width+"x"+height+".png"),Path.Combine(directory,name+"-"+width+"x"+height+".png"),true); }
    }
}
#endif
