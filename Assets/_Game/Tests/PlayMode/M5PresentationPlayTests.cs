#if UNITY_EDITOR
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.TestTools;
namespace Starfall.Tests
{
    public sealed partial class CampaignPlayTests
    {
        [UnityTest] public IEnumerator M5FocusReturnsToCategoryAndGearDescriptionsMatchSelection() {
            game.ReturnToMenu();game.ModeUI.HideIntroduction();game.FrontEnd.OpenPreparation();yield return null;
            var category=game.Interface.transform.Find("M5 Preparation").Find("Category Passive").GetComponent<Button>(); UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(category.gameObject);category.onClick.Invoke();yield return null;
            Assert.AreEqual("Category Passive",UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.name);
            Assert.IsTrue(game.Interface.transform.Find("M5 Preparation").Find("Item detail").GetComponent<TMPro.TextMeshProUGUI>().text.Contains(game.Text.Get("item.rapid")));
            game.Interface.OpenSettings();yield return null;game.Interface.CloseSettings();yield return null;yield return null;
            Assert.AreEqual("Category Passive",UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.name);
        }
        [UnityTest] public IEnumerator M5SoundTestUsesLiveMutedSettingsAndIndependentBuses() {
            game.StartStage(1);yield return null;game.Audio.Play(GameSound.Shot);game.Interface.OpenSettings();game.Audio.TestSound();yield return new WaitForSecondsRealtime(.06f);
            UnityEngine.AudioSource test=null; foreach(var source in game.GetComponents<AudioSource>()) if(source.ignoreListenerPause) test=source;
            Assert.IsNotNull(test);Assert.IsTrue(test.isPlaying);Assert.AreSame(M5Art.Catalog.uiGroup,test.outputAudioMixerGroup);
            var samples=new float[1024];test.GetOutputData(samples,0);double sum=0;foreach(float value in samples) sum+=value*value;
            float peak=game.GameCamera.GetComponent<AudioOutputLimiter>().peak; Assert.Greater(peak,0,"Paused-world UI test sound must reach the listener DSP output");
            File.WriteAllText(Path.Combine(Application.dataPath,"../../appendix/M5/audio-runtime.txt"),"World paused, music paused; UI test source playing. Listener DSP peak="+peak+" (nonzero asserted). Source GetOutputData RMS="+System.Math.Sqrt(sum/samples.Length)+" during listener pause. Automated signal probe, no hardware listening claim.\n");
            game.SetVolume("effects",0);foreach(var source in game.GetComponents<AudioSource>()) if(source.outputAudioMixerGroup!=M5Art.Catalog.musicGroup) Assert.AreEqual(0,source.volume); yield return new WaitForSecondsRealtime(.1f); Assert.Less(game.GameCamera.GetComponent<AudioOutputLimiter>().peak,.0001f);
            game.SetVolume("effects",1);Assert.Greater(test.volume,0);game.SetVolume("master",0);foreach(var source in game.GetComponents<AudioSource>()) Assert.AreEqual(0,source.volume);
            game.Interface.CloseSettings();Assert.IsFalse(AudioListener.pause);game.ReturnToMenu();
        }
        [UnityTest] public IEnumerator M5NewProfileCanStartAnyStageAndRetryWithoutOwningRewards() {
            for(int stage=6;stage>=1;stage--) { Assert.IsTrue(game.StartStage(stage)); yield return null; Assert.AreEqual(stage,game.Adventure.Stage); Assert.AreEqual("pistol",game.Loadout.Weapon); Assert.AreEqual("medkit",game.Loadout.Active); Assert.IsTrue(game.Context.RecordEligible); game.Context.AddCoins(30); game.RestartMode(); yield return null; Assert.AreEqual(stage,game.Adventure.Stage); Assert.AreEqual(0,game.Context.Coins); Assert.AreEqual(2,game.Profile.Data.unlocked.Count); }
        }
        [UnityTest] public IEnumerator M5SelectionDoesNotSpawnAndStartCreatesOnlySelectedWave() {
            game.StartTraining(); yield return null; game.ModeUI.Open(PracticePanel.Simulation); game.Training.SelectEnemy("shield"); game.Training.SetCount(12); Assert.AreEqual(0,game.LivingEnemies); Assert.IsFalse(game.Training.Running);
            game.ModeUI.Close(); yield return null; Assert.AreEqual(0,game.LivingEnemies); game.Training.StartSelected(); yield return null; Assert.IsTrue(game.Training.Running); Assert.That(game.LivingEnemies,Is.InRange(1,12)); game.Training.SelectEnemy("sniper"); Assert.AreEqual("shield",game.Training.SelectedEnemy);
            game.Training.StopSimulation(); Assert.AreEqual(0,game.LivingEnemies); Assert.AreEqual(0,game.Projectiles.ActiveCount); Assert.AreEqual(2,game.Profile.Data.unlocked.Count);
        }
        [UnityTest] public IEnumerator M5PickupAndFacilityKeysStaySeparateAndComparisonCancelPreservesLoot() {
            game.StartStage(1); yield return null; int picked=0,operated=0; game.AddStation("test.pickup","item.shotgun",new Vector2(-8,-4),()=>picked++); game.AddStation("test.facility","station.beacon",new Vector2(-8,-3.8f),()=>operated++);
            int sounds=game.Audio.AudibleEvents; yield return Press(Key.F); Assert.Greater(game.Audio.AudibleEvents,sounds); Assert.AreEqual(0,picked); Assert.AreEqual(1,operated); yield return Press(Key.E); Assert.AreEqual(1,picked); Assert.AreEqual(1,operated);
            game.StartStage(4); yield return null; game.EquipItem("smg",true); yield return Interact("supply.crossbow"); Assert.AreEqual("crossbow",game.PendingEquipment); Assert.IsFalse(game.Adventure.Progress.IsClaimed("supply.crossbow")); Assert.IsFalse(game.CanAct);
            game.CancelEquipmentReplacement(); Assert.IsFalse(game.Adventure.Progress.IsClaimed("supply.crossbow")); yield return Interact("supply.crossbow"); Assert.IsTrue(game.ConfirmEquipmentReplacement()); Assert.IsTrue(game.Adventure.Progress.IsClaimed("supply.crossbow")); Assert.AreEqual("crossbow",game.Loadout.SpecialWeapon);
        }
        [UnityTest] public IEnumerator M5CompletedRewardSurvivesResultExitAndReloadAndCanEquipIntoStageSix() {
            game.StartStage(2); yield return null; yield return ReachGuardian(false); game.Adventure.BossHealth.Receive(new DamageContext(10000,Faction.Player)); yield return null; yield return null;
            string clear=game.Adventure.ClearId; Assert.IsTrue(game.Profile.Owns("smg")); Assert.IsTrue(game.Profile.HasPending(2)); Assert.IsTrue(game.Adventure.AdvanceStage());
            var reload=new PlayerProfileStore(temporary,game.Catalog); Assert.IsTrue(reload.HasPending(2)); Assert.IsTrue(reload.Choose(clear,"shield")); Assert.IsTrue(game.Profile.Choose(clear,"shield"));
            var gear=game.Profile.Prepared(); gear.Equip("smg"); gear.Equip("shield"); Assert.IsTrue(game.Profile.SavePreparation(gear)); game.StartStage(6); yield return null; Assert.AreEqual("smg",game.Loadout.SpecialWeapon); Assert.AreEqual("shield",game.Loadout.Active);
        }
        [UnityTest] [Timeout(600000)] public IEnumerator M5ScreensCoverEveryPageAndSixThemesInBothLanguagesAtFourResolutions() {
            foreach(string language in new[]{"zh-CN","en"}) foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(2560,1440),new Vector2Int(1280,960)}) {
                game.ReturnToMenu(); game.ModeUI.HideIntroduction(); game.SetLanguage(language); yield return null; Capture("M5-menu-"+language+"-"+size.x+"x"+size.y,size.x,size.y);
                game.FrontEnd.OpenStages(); yield return null; Capture("M5-stages-"+language+"-"+size.x+"x"+size.y,size.x,size.y);
                game.FrontEnd.OpenPreparation(); yield return null; Capture("M5-preparation-"+language+"-"+size.x+"x"+size.y,size.x,size.y);
                foreach(ItemKind kind in System.Enum.GetValues(typeof(ItemKind))) {
                    game.Interface.transform.Find("M5 Preparation").Find("Category "+kind).GetComponent<Button>().onClick.Invoke(); yield return null;
                    Capture("M5-preparation-"+kind+"-"+language+"-"+size.x+"x"+size.y,size.x,size.y);
                }
                game.Interface.OpenSettings(); yield return null; Capture("M5-settings-"+language+"-"+size.x+"x"+size.y,size.x,size.y); game.Interface.CloseSettings();
                game.StartTraining(); yield return null; game.ModeUI.Open(PracticePanel.Simulation); yield return null; Capture("M5-training-"+language+"-"+size.x+"x"+size.y,size.x,size.y); game.ModeUI.Close();
                for(int stage=1;stage<=6;stage++) { game.StartStage(stage); yield return null; if(stage>1) { yield return Interact("route.next"); } else { Place(new Vector2(-5.7f,-4)); yield return null; }
                    Capture("M5-theme"+stage+"-"+language+"-"+size.x+"x"+size.y,size.x,size.y);
                }
                game.StartStage(2); yield return null; yield return ReachGuardian(false); game.Adventure.BossHealth.Receive(new DamageContext(10000,Faction.Player)); yield return null; yield return null;
                Capture("M5-result-"+language+"-"+size.x+"x"+size.y,size.x,size.y);
                game.AdventureUI.OpenRecords(); yield return null; Capture("M5-records-"+language+"-"+size.x+"x"+size.y,size.x,size.y); game.AdventureUI.CloseRecords(); game.ReturnToMenu();
                game.StartStage(2); yield return null; yield return Interact("route.next"); yield return ClearRoom(); yield return Interact("route.next"); yield return ClearRoom(); yield return Interact("beacon.north"); yield return Interact("route.branch"); yield return Interact("shop.open");
                Capture("M5-shop-"+language+"-"+size.x+"x"+size.y,size.x,size.y); game.AdventureUI.CloseShop(); game.ReturnToMenu();
                game.StartStage(4); yield return null; game.EquipItem("smg",true); yield return Interact("supply.crossbow"); yield return null; Capture("M5-comparison-"+language+"-"+size.x+"x"+size.y,size.x,size.y); game.CancelEquipmentReplacement();
            }
            game.ReturnToMenu(); game.SetLanguage("zh-CN"); game.StartStage(1); yield return null; Place(new Vector2(-5.7f,-4)); yield return null; game.Settings.minimalEffects=true; yield return null; Capture("M5-minimal-zh-CN",1920,1080); game.Settings.minimalEffects=false;
            game.Feedback.Item("shotgun"); yield return null; Capture("M5-pickup-zh-CN",1920,1080);
            game.Audio.TestSound(); Assert.Greater(game.Audio.AudibleEvents,0); Assert.IsNotNull(M5Art.Catalog.musicGroup); Assert.AreNotSame(M5Art.Catalog.musicGroup,M5Art.Catalog.effectsGroup);
            File.WriteAllLines(Path.Combine(Application.dataPath,"../../appendix/M5/layout-diagnostics.txt"),m5LayoutProblems); Assert.IsEmpty(m5LayoutProblems,string.Join("\n",m5LayoutProblems));
        }
    }
}
#endif
