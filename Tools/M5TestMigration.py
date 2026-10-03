from pathlib import Path
root=Path(__file__).resolve().parents[1]
tests=root/'Assets/_Game/Tests'
def method_replace(s,name,body):
    at=s.index('public IEnumerator '+name+'('); start=s.index('{',at); depth=1; end=start+1
    while depth:
        if s[end]=='{': depth+=1
        if s[end]=='}': depth-=1
        end+=1
    return s[:start]+'{\n'+body+'\n        }'+s[end:]
for p in (tests/'PlayMode').glob('*.cs'):
    s=p.read_text(encoding='utf-8-sig').replace('using UnityEngine.UI;', 'using UnityEngine.UI;\nusing Text = TMPro.TextMeshProUGUI;')
    s=s.replace('Place(station.transform.position); yield return Press(Key.E);','Place(station.transform.position); yield return Press(station.IsPickup ? Key.E : Key.F);')
    s=s.replace('view.font.HasCharacter(character)', 'view.font.HasCharacter(character,true,true)')
    p.write_text(s,encoding='utf-8')
p=tests/'EditMode/AdventureRulesTests.cs'; s=p.read_text().replace('Assert.AreEqual(9, plan.Rooms.Count)','Assert.AreEqual(6, plan.Rooms.Count)').replace('state.Activate("south"); Assert.IsFalse(state.CanEnterBoss); state.Clear("seal"); Assert.IsTrue(state.CanEnterBoss);','state.Activate("south"); Assert.IsTrue(state.CanEnterBoss);'); p.write_text(s,encoding='utf-8')
p=tests/'PlayMode/CampaignPlayTests.cs'; s=p.read_text()
s=method_replace(s,'NormalInputAdventureCompletesSixStagesAndReloadsSixRecords','''            var report = new System.Text.StringBuilder("M5 six independent starts, real keyboard/mouse. No teleport, injected damage, invincibility or speed changes. Temporary profile and records.\\n");
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
                report.Append(stage).Append(" | ").Append(LevelTimer.Format(game.Adventure.Result.Attempt.milliseconds)).Append(" | HP ").Append(game.Player.Health.State.Health).Append(" | ").Append(string.Join(" / ",game.Adventure.Result.Attempt.equipment)).Append("\\n");
                if(game.Adventure.Rewards.Count>0) Assert.IsTrue(game.Adventure.SelectReward(game.Adventure.Rewards[0]));
                Capture("m5-normal-clear-s"+stage+"-en",1920,1080);
                Assert.IsTrue(game.Adventure.AdvanceStage()); yield return null;
                var gear=game.Profile.Prepared(); if(game.Profile.Owns("smg")) gear.Equip("smg"); else if(game.Profile.Owns("shotgun")) gear.Equip("shotgun");
                if(game.Profile.Owns("rapid") && gear.Layers("rapid")==0) gear.Equip("rapid"); Assert.IsTrue(game.Profile.SavePreparation(gear));
            }
            Assert.AreEqual(6,new PersonalBestStore(temporary).Book.bests.Count); File.WriteAllText(Path.Combine(Application.dataPath,"../../appendix/M5/normal-input.txt"),report.ToString());''')
s=method_replace(s,'RewardSaveFailureCannotGrantOrDuplicateNextEntryAndCancelKeepsProgress','''            game.StartStage(2); yield return null; yield return ReachGuardian(false); game.Adventure.BossHealth.Receive(new DamageContext(10000,Faction.Player)); yield return null; yield return null;
            string id=game.Adventure.Rewards[0]; string blocked=Path.Combine(temporary,"starfall-profile.json.tmp"); Directory.CreateDirectory(blocked);
            Assert.IsFalse(game.Adventure.SelectReward(id)); Assert.IsFalse(game.Profile.Owns(id)); Assert.IsTrue(game.Profile.HasPending(2)); Assert.IsFalse(game.Adventure.AdvanceStage());
            Directory.Delete(blocked); Assert.IsTrue(game.Adventure.SelectReward(id)); Assert.IsFalse(game.Adventure.SelectReward(id)); Assert.IsTrue(game.Adventure.AdvanceStage());
            Assert.IsNull(game.Context); Assert.AreEqual(FrontPage.Stages,game.FrontEnd.Page); Assert.IsFalse(game.Checkpoints.HasEntry); Assert.IsTrue(new PlayerProfileStore(temporary,game.Catalog).Owns(id));''')
s=method_replace(s,'ResumeAfterApplicationReloadRestoresEntryAndPracticeCannotContaminateIt','''            Assert.IsTrue(game.StartStage(6)); yield return null; var entry=JsonUtility.FromJson<EntryCheckpoint>(JsonUtility.ToJson(game.Checkpoints.Current));
            game.ReturnToMenu(); game.StartTraining(); game.EquipItem("rapid"); game.StartEnvironmentSample(2); yield return null; game.ReturnToMenu();
            Assert.AreEqual(JsonUtility.ToJson(entry),JsonUtility.ToJson(game.Checkpoints.Current)); Assert.IsFalse(game.Profile.Owns("rapid"));
            UnityEngine.Object.Destroy(game.gameObject); yield return null; SceneManager.LoadScene("Assets/_Game/Scenes/Boot.unity"); yield return null; game=UnityEngine.Object.FindFirstObjectByType<StarfallGame>();
            Assert.IsTrue(game.ContinueAdventure()); yield return null; Assert.AreEqual(6,game.Adventure.Stage); Assert.AreEqual("entry",game.Adventure.Current.Id); Assert.AreEqual(entry.seed,game.Adventure.Plan.Seed);
            Assert.AreEqual(JsonUtility.ToJson(entry.loadout),JsonUtility.ToJson(game.Loadout.Snapshot())); Assert.AreEqual(0,game.Context.Coins); Assert.IsFalse(game.Loadout.Invincible);
            game.Player.Health.State.Damage(10000,1000,false,0); yield return null; Assert.IsFalse(game.Checkpoints.HasEntry); Assert.AreEqual(2,game.Profile.Data.unlocked.Count);''')
s=s.replace('now = 10; Assert.AreEqual(10000, game.Adventure.Timer.Milliseconds);','now = 10; Assert.AreEqual(0, game.Adventure.Timer.Milliseconds);').replace('now = 30; Assert.AreEqual(10000, game.Adventure.Timer.Milliseconds);','now = 30; Assert.AreEqual(0, game.Adventure.Timer.Milliseconds);').replace('Assert.AreEqual(15000, game.Adventure.Timer.Milliseconds);','Assert.AreEqual(0, game.Adventure.Timer.Milliseconds);')
s=s.replace('public IEnumerator ShopTimeCountsAndPurchases', 'public IEnumerator ShopTimeIsExcludedAndPurchases')
p.write_text(s,encoding='utf-8')
# Core tutorial has the same seven steps, but facilities use F and the energy example is explicit.
p=tests/'PlayMode/PracticePlayTests.cs'; s=p.read_text()
s=s.replace('Place(new Vector2(7, 3)); yield return KeyPress(Key.E);', 'Place(new Vector2(7, 3)); yield return KeyPress(Key.F);')
s=s.replace('yield return KeyPress(Key.E); Assert.IsTrue(game.Room.DoorOpen);','yield return KeyPress(Key.F); Assert.IsTrue(game.Room.DoorOpen);')
s=s.replace('Place(game.Room.Exit); yield return KeyPress(Key.E);','Place(game.Room.Exit); yield return KeyPress(Key.F);')
s=s.replace('Assert.AreEqual(6, UnityEngine.Object.FindObjectsByType<PracticeInteractable>(FindObjectsSortMode.None).Length, "Old discarded weapons must be recycled")','Assert.AreEqual(30, UnityEngine.Object.FindObjectsByType<PracticeInteractable>(FindObjectsSortMode.None).Length, "Every unequipped weapon remains independently pickable until reset")')
p.write_text(s,encoding='utf-8')
print('Regression tests migrated to M5 stage and input contract')
