from pathlib import Path
root=Path(__file__).resolve().parents[1]
p=root/'Assets/_Game/Tests/PlayMode/AdventurePlayTests.cs'; s=p.read_text()
def replace(name,body):
 global s
 at=s.index('IEnumerator '+name+'('); a=s.index('{',at); depth=1; b=a+1
 while depth:
  if s[b]=='{':depth+=1
  if s[b]=='}':depth-=1
  b+=1
 s=s[:a]+'{\n'+body+'\n        }'+s[b:]
replace('ReachBoss','''            if(practice) game.Context.InvalidateRecord(); Place(new Vector2(-5.7f,-4)); yield return null; yield return null;
            foreach(string id in new[]{"courtyard","north","crossing","south"}) {
                Assert.AreEqual(id,game.Adventure.Current.Id); yield return ClearRoom();
                if(id=="north" || id=="south") yield return Interact("beacon."+id);
                yield return Interact("route.next");
            }
            Assert.AreEqual("boss",game.Adventure.Current.Id); Assert.IsNotNull(game.Adventure.Boss);''')
replace('NormalEquipmentInputRunCompletesStageAndPersistsEligibleRecord','''            game.StartStage(1); yield return null; Assert.AreEqual("medkit",game.Loadout.Active); yield return WalkTo(new Vector2(-5.7f,-4));
            foreach(string id in new[]{"courtyard","north","crossing","south"}) {
                Assert.AreEqual(id,game.Adventure.Current.Id); yield return FightUsingInput();
                if(id=="north" || id=="south") { yield return WalkTo(new Vector2(7,2)); yield return Press(Key.F); }
                if(id=="south") { yield return WalkTo(new Vector2(-6,-4)); yield return Press(Key.F); }
                yield return WalkTo(game.Room.Exit); yield return Press(Key.F);
            }
            yield return FightUsingInput(); Assert.IsTrue(game.Adventure.Result.Success); Assert.IsTrue(game.Context.RecordEligible); Assert.IsTrue(game.Adventure.Result.Saved); Assert.IsTrue(game.Profile.Owns("shotgun"));
            Assert.IsNotNull(new PersonalBestStore(temporary).Find("gardens",game.LevelConfig.timingVersion,game.LevelConfig.balanceVersion,"stage_select")); Capture("m5-basic-first-clear-en",1920,1080);''')
replace('FirstLevelTasksRoomTransitionsAndSettlementAreAtomic','''            game.StartStage(1); yield return null; Assert.AreEqual(0,game.LivingEnemies); Assert.IsFalse(game.Adventure.Progress.IsClear("courtyard")); Assert.IsFalse(game.Adventure.Travel("north"));
            Place(new Vector2(-5.7f,-4)); yield return null; yield return null; Assert.Greater(game.LivingEnemies,0); yield return ClearRoom(); yield return Interact("courtyard.chest");
            var chest=UnityEngine.Object.FindFirstObjectByType<PracticeInteractable>(); yield return Interact("supply.shotgun"); Assert.AreEqual("shotgun",game.Loadout.SpecialWeapon); Assert.IsTrue(game.Adventure.Progress.IsClaimed("courtyard.weapon"));
            yield return Interact("route.next"); Assert.AreEqual("north",game.Adventure.Current.Id); yield return ClearRoom(); yield return Interact("beacon.north"); Assert.AreEqual(1,game.Adventure.Progress.Beacons); yield return Interact("route.back");
            Assert.AreEqual("courtyard",game.Adventure.Current.Id); Assert.AreEqual(0,game.LivingEnemies); Assert.Greater(game.Player.Body.position.x,8); Assert.IsTrue(game.Adventure.Progress.IsClaimed("courtyard.weapon"));
            game.RestartMode(); yield return null; Assert.AreEqual("courtyard",game.Adventure.Current.Id); Assert.IsNull(game.Loadout.SpecialWeapon); Assert.AreEqual(0,game.Context.Coins); Assert.AreEqual(2,game.Profile.Data.unlocked.Count);''')
replace('ChallengeCanBeAbandonedAndDoesNotBlockMainRoute','''            game.StartStage(1); yield return null; Place(new Vector2(-5.7f,-4)); yield return null; yield return ClearRoom(); yield return Interact("route.branch");
            Assert.AreEqual("challenge",game.Adventure.Current.Id); Assert.IsTrue(game.Adventure.Travel("courtyard")); yield return null; Assert.IsTrue(game.Adventure.ChallengeFailed); Assert.Less(Vector2.Distance(game.Player.Body.position,new Vector2(0,-4)),.1f);
            yield return Interact("route.branch"); Assert.AreEqual(0,game.LivingEnemies); Assert.IsFalse(game.Adventure.Progress.IsClaimed("challenge.reward")); Assert.IsTrue(game.Adventure.Travel("courtyard")); Assert.IsTrue(game.Adventure.Travel("north"));''')
# Existing feedback capture fixture must not invent permanent choices for a practice result.
s=s.replace('Assert.AreEqual(3, adventure.Rewards.Count);','Assert.IsEmpty(adventure.Rewards);')
s=s.replace('game.Records.Find("gardens",game.LevelConfig.timingVersion,game.LevelConfig.balanceVersion)', 'game.Records.Find("gardens",game.LevelConfig.timingVersion,game.LevelConfig.balanceVersion,"stage_select")')
s=s.replace('new PersonalBestStore(temporary).Find("gardens",game.LevelConfig.timingVersion,game.LevelConfig.balanceVersion)', 'new PersonalBestStore(temporary).Find("gardens",game.LevelConfig.timingVersion,game.LevelConfig.balanceVersion,"stage_select")')
p.write_text(s,encoding='utf-8'); print('First-stage regression adapted to five-room route')
