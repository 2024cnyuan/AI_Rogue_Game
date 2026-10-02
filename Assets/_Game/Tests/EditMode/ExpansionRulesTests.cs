using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
namespace Starfall.Tests
{
    public sealed class ExpansionRulesTests
    {
        [Test] public void SixHundredSeedsHaveConnectedObjectivesAndLegalSpawns() {
            for(int stage=1;stage<=6;stage++) for(int seed=0;seed<100;seed++) {
                var plan=new FirstLevelPlan(seed,stage); var graph=new HashSet<string>(); var rooms=new Queue<string>(); rooms.Enqueue("entry");
                var layouts=new HashSet<string>(); foreach(string id in new[]{"courtyard","north","crossing","south"}) layouts.Add(string.Join("/",Array.ConvertAll(plan.Find(id).Cover,r=>r.ToString())));
                Assert.AreEqual(4,layouts.Count,"Layouts stage "+stage);
                while(rooms.Count>0) { string id=rooms.Dequeue(); if(!graph.Add(id)) continue; var room=plan.Find(id); Assert.IsNotNull(room); foreach(string next in new[]{room.Next,room.Back,room.Branch}) if(next!=null) rooms.Enqueue(next); if(stage==1 && id=="courtyard") rooms.Enqueue("challenge"); }
                Assert.AreEqual(plan.Rooms.Count,graph.Count,"Graph stage/seed "+stage+"/"+seed);
                var ledger=new FirstLevelProgress(stage);
                foreach(string id in plan.RequiredTasks==3 ? new[]{"north","middle","south"} : new[]{"north","south"}) { Assert.IsTrue(graph.Contains(id)); ledger.Clear(id); Assert.IsTrue(ledger.Activate(id)); }
                ledger.Clear("seal"); Assert.IsTrue(ledger.CanEnterBoss);
                foreach(var room in plan.Rooms) {
                    var reached=new HashSet<Vector2Int>(); var queue=new Queue<Vector2Int>(); queue.Enqueue(new Vector2Int(-8,-4));
                    while(queue.Count>0) { var at=queue.Dequeue(); if(!reached.Add(at)) continue; foreach(var direction in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right}) { var next=at+direction; if(!reached.Contains(next) && FirstLevelPlan.IsClear(room,next,.45f)) queue.Enqueue(next); } }
                    string context=stage+"/"+seed+"/"+room.Id;
                    foreach(var point in room.Enemies) { Assert.IsTrue(FirstLevelPlan.IsClear(room,point,.55f),context); Assert.IsTrue(reached.Contains(Vector2Int.RoundToInt(point)),context); Assert.Greater(Vector2.Distance(point,new Vector2(-8,-4)),1.2f,context); Assert.Greater(Vector2.Distance(point,new Vector2(10.4f,0)),1.5f,context); }
                    foreach(var target in new[]{new Vector2Int(10,0),new Vector2Int(7,2),new Vector2Int(7,3),new Vector2Int(0,-5)}) Assert.IsTrue(reached.Contains(target),context+" target "+target);
                }
            }
        }
        [Test] public void ThreeRelaysRejectWrongOrderAndDuplicateActivation() {
            var state=new FirstLevelProgress(5); foreach(string id in new[]{"north","middle","south"}) state.Clear(id);
            Assert.IsFalse(state.Activate("south")); Assert.IsFalse(state.Activate("middle")); Assert.IsTrue(state.Activate("north")); Assert.IsFalse(state.Activate("north")); Assert.IsTrue(state.Activate("middle")); Assert.IsTrue(state.Activate("south")); Assert.IsFalse(state.CanEnterBoss); state.Clear("seal"); Assert.IsTrue(state.CanEnterBoss);
            var spore=new FirstLevelProgress(4); foreach(string id in new[]{"south","middle","north"}) { spore.Clear(id); Assert.IsTrue(spore.Activate(id)); } Assert.AreEqual(3,spore.Beacons);
        }
        [Test] public void CompleteCatalogCapsStatsAndReplacementPreservesUnrelatedLayers() {
            var catalog=ItemCatalog.Defaults(); try {
                var gear=new LoadoutState(catalog); foreach(string id in new[]{"rapid","agile","pierce","bounce","critical","blast"}) { Assert.IsTrue(gear.Equip(id)); Assert.IsTrue(gear.Equip(id)); Assert.IsFalse(gear.Equip(id)); }
                Assert.AreEqual(2,gear.Pierce); Assert.AreEqual(2,gear.Bounces); Assert.AreEqual(.3f,gear.CritChance,.001); Assert.AreEqual(1.5f,gear.BlastMultiplier);
                Assert.IsFalse(gear.Equip("controlled")); Assert.IsTrue(gear.ReplacePassive("blast","controlled")); Assert.AreEqual(2,gear.Layers("rapid")); Assert.AreEqual(1,gear.Layers("controlled")); Assert.AreEqual(0,gear.Layers("blast")); Assert.AreEqual(6,gear.Passives.Count);
                string before=JsonUtility.ToJson(gear.Snapshot()); var bad=gear.Snapshot(); bad.passives[0].layers=0; Assert.IsFalse(gear.RestoreSnapshot(bad)); Assert.AreEqual(before,JsonUtility.ToJson(gear.Snapshot()));
                gear.DefaultEquipment(); gear.Equip("lowhealth"); gear.Equip("lowhealth"); var vital=new VitalState(100); vital.Damage(65,0,false,0); Assert.AreEqual(.6f,gear.IncomingMultiplier(vital),.001);
            } finally { UnityEngine.Object.DestroyImmediate(catalog); }
        }
        [Test] public void AreaActivationFailureNeverSpendsAndSuccessKeepsConfiguredCooldown() {
            var catalog=ItemCatalog.Defaults(); try { var gear=new LoadoutState(catalog); var vital=new VitalState(100);
                foreach(string id in new[]{"slow","shock","decoy","grenade"}) { gear.Equip(id); Assert.AreEqual("active.blocked",gear.TryUse(vital,false,()=>false)); Assert.AreEqual(2,gear.Charges); Assert.AreEqual(0,gear.ActiveCooldown); Assert.IsNull(gear.TryUse(vital,false,()=>true)); Assert.AreEqual(1,gear.Charges); Assert.AreEqual(catalog.Find(id).cooldown,gear.ActiveCooldown); Assert.AreEqual("active.cooldown",gear.TryUse(vital,false,()=>true)); }
            } finally { UnityEngine.Object.DestroyImmediate(catalog); }
        }
        [Test] public void LegacyCheckpointMigratesAndSixthEntryKeepsHistoryWithoutResettingEarlierRecords() {
            string directory=Path.Combine(Path.GetTempPath(),"StarfallM4Rules-"+Guid.NewGuid()); Directory.CreateDirectory(directory);
            try { var legacy=new EntryCheckpoint {contentVersion="m3-v1",stage=3,seed=7,runId="legacy"}; File.WriteAllText(Path.Combine(directory,"starfall-checkpoint.json"),JsonUtility.ToJson(legacy)); var store=new CheckpointStore(directory); Assert.IsTrue(store.HasEntry); Assert.AreEqual("m4-v1",store.Current.contentVersion);
                var sixth=new EntryCheckpoint {stage=6,runId="six",seed=8,completedTimes=new List<long>{11,22,33,44,55}}; Assert.IsTrue(store.Save(sixth)); var loaded=new CheckpointStore(directory); Assert.AreEqual(5,loaded.Current.completedTimes.Count); Assert.IsTrue(loaded.Clear()); Assert.IsFalse(new CheckpointStore(directory).HasEntry);
                sixth.stage=7; Assert.IsFalse(store.Save(sixth));
            } finally { Directory.Delete(directory,true); }
        }
    }
}
