using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
namespace Starfall.Tests
{
    public sealed class M5ProfileTests
    {
        string dir; ItemCatalog catalog;
        [SetUp] public void Setup() { dir=Path.Combine(Path.GetTempPath(),"StarfallM5-"+Guid.NewGuid()); catalog=Resources.Load<ItemCatalog>("ItemCatalog"); }
        [TearDown] public void Cleanup() { if(Directory.Exists(dir)) Directory.Delete(dir,true); }
        [Test] public void EveryEquipmentHasAReachablePermanentSource() { var ids=new HashSet<string>(); foreach(var i in catalog.items) { Assert.GreaterOrEqual(StageRewards.Source(i.id),0,i.id); ids.Add(i.id); } Assert.AreEqual(25,ids.Count); }
        [Test] public void ClearAndChoiceAreIdempotentAndPersistWithoutNextStage() {
            var p=new PlayerProfileStore(dir,catalog); Assert.IsTrue(p.Complete("clear6",6)); Assert.IsTrue(p.Owns("arc")); Assert.IsFalse(p.IsClear(1)); Assert.IsTrue(p.HasPending(6));
            Assert.IsTrue(p.Complete("clear6",6)); Assert.AreEqual(1,p.Data.receipts.Count); Assert.IsTrue(p.Choose("clear6","recharge")); Assert.IsTrue(p.Choose("clear6","recharge")); Assert.IsFalse(p.Choose("clear6","decoy"));
            p=new PlayerProfileStore(dir,catalog); Assert.IsTrue(p.Owns("recharge")); Assert.IsFalse(p.HasPending(6)); Assert.AreEqual(4,p.Data.unlocked.Count);
        }
        [Test] public void FailedWritesKeepPendingEligibilityAndOwnershipAtomic() {
            bool writable=false; var p=new PlayerProfileStore(dir,catalog,(a,b)=>writable);
            Assert.IsFalse(p.Complete("one",1)); Assert.IsFalse(p.Owns("shotgun")); Assert.IsEmpty(p.Data.receipts); writable=true; Assert.IsTrue(p.Complete("one",1));
            writable=false; Assert.IsFalse(p.Choose("one","rapid")); Assert.IsFalse(p.Owns("rapid")); Assert.IsTrue(p.HasPending(1)); writable=true; Assert.IsTrue(p.Choose("one","rapid")); Assert.IsFalse(p.HasPending(1));
        }
        [Test] public void FullCollectionAllowsFurtherClearsWithoutDuplicateItems() {
            var p=new PlayerProfileStore(dir,catalog); for(int stage=1;stage<=6;stage++) foreach(var item in StageRewards.Pools[stage-1]) { string id=stage+item; Assert.IsTrue(p.Complete(id,stage)); Assert.IsTrue(p.Choose(id,item)); }
            Assert.AreEqual(25,p.Data.unlocked.Count); Assert.IsTrue(p.Complete("extra",6)); Assert.IsEmpty(p.Choices("extra")); Assert.IsTrue(p.Choose("extra",null)); Assert.IsTrue(p.AllCollected(6));
        }
        [Test] public void RewardJournalReplaysAfterProfileWriteFailureAndRestart() {
            var failed=new PlayerProfileStore(dir,catalog,(a,b)=>false); Assert.IsFalse(failed.Complete("recover6",6));
            var recovered=new PlayerProfileStore(dir,catalog); Assert.IsTrue(recovered.Owns("arc")); Assert.IsTrue(recovered.HasPending(6)); Assert.AreEqual(1,recovered.Data.receipts.Count);
            Assert.IsTrue(recovered.Choose("recover6","decoy")); Assert.IsTrue(new PlayerProfileStore(dir,catalog).Owns("decoy"));
        }
        [Test] public void PreparationRejectsLockedOrStackedEquipmentAndResetsSupplies() {
            var p=new PlayerProfileStore(dir,catalog); var gear=p.Prepared(); gear.Equip("arc"); Assert.IsFalse(p.SavePreparation(gear));
            p.Complete("one",1); p.Choose("one","rapid"); gear=p.Prepared(); gear.Equip("shotgun"); gear.Equip("rapid"); gear.SpendEnergy(50); Assert.IsTrue(p.SavePreparation(gear)); Assert.AreEqual(100,p.Prepared().Energy);
            gear.Equip("rapid"); Assert.IsFalse(p.SavePreparation(gear)); Assert.AreEqual(1,p.Prepared().Layers("rapid"));
        }
        [Test] public void ModeGroupsPreserveLegacyRecordsWithoutComparingThem() {
            var store=new PersonalBestStore(dir); var legacy=new BestRecord {stageId="gardens",attemptId="old",milliseconds=1000,equipment=new[]{"pistol"}}; store.Commit(store.Prepare(legacy,true,true));
            var modern=new BestRecord {stageId="gardens",mode="stage_select",attemptId="new",milliseconds=2000,equipment=new[]{"pistol"}}; var result=store.Prepare(modern,true,true); Assert.IsNull(result.Previous); Assert.IsTrue(store.Commit(result)); Assert.AreEqual(2,store.Book.bests.Count);
        }
        [Test] public void FirstStageHasFiveUsefulMainRoomsAndNoEmptyRelay() {
            var plan=new FirstLevelPlan(1729); Assert.AreEqual("courtyard",plan.StartRoom); Assert.AreEqual(6,plan.Rooms.Count); Assert.IsNull(plan.Find("seal")); Assert.IsNull(plan.Find("supply"));
            var progress=new FirstLevelProgress(); progress.Clear("north");progress.Activate("north");Assert.IsFalse(progress.CanEnterBoss);progress.Clear("south");progress.Activate("south");Assert.IsTrue(progress.CanEnterBoss);
        }
        [Test] public void MixedAudioHasHeadroomAndPreservesQuietSignals() {
            float previous=0; for(int i=0;i<=500;i++) { float sample=i/100f; float value=AudioOutputLimiter.Limit(sample); Assert.LessOrEqual(value,.95f); Assert.GreaterOrEqual(value,previous); Assert.AreEqual(-value,AudioOutputLimiter.Limit(-sample),.00001f); previous=value; }
            Assert.AreEqual(.4f,AudioOutputLimiter.Limit(.4f));
        }
    }
}
