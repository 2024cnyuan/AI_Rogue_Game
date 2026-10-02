using System;
using System.IO;
using NUnit.Framework;

namespace Starfall.Tests
{
    public sealed class RulesTests
    {
        [Test]
        public void DamageProtectionInvulnerabilityAndDeathAreExclusive()
        {
            var life = new VitalState(100);
            Assert.IsFalse(life.Damage(40, 0, true, .6f));
            Assert.IsTrue(life.Damage(40, 0, false, .6f));
            Assert.IsFalse(life.Damage(80, .59, false, .6f));
            Assert.IsTrue(life.Damage(80, .61, false, .6f));
            Assert.IsFalse(life.Alive); Assert.IsFalse(life.Heal(100)); Assert.IsFalse(life.Damage(10, 2, false, 0));
        }
        [Test]
        public void FullHealthDoesNotConsumeHealing()
        {
            var life = new VitalState(100); Assert.IsFalse(life.Heal(25));
            life.Damage(10, 0, false, 0); Assert.IsTrue(life.Heal(25)); Assert.AreEqual(100, life.Health);
        }
        [Test]
        public void NestedPauseReasonsDoNotResumeEarly()
        {
            var pause = new PauseState(); pause.Set(PauseReason.Menu, true); pause.Set(PauseReason.Focus, true); pause.Set(PauseReason.Settings, true);
            pause.Set(PauseReason.Settings, false); pause.Set(PauseReason.Focus, false); Assert.IsTrue(pause.IsPaused);
            pause.Set(PauseReason.Menu, false); Assert.IsFalse(pause.IsPaused);
        }
        [Test]
        public void RoomCannotClearBeforeRegistrationOrGrantRewardTwice()
        {
            var run = new RunContext(GameMode.Adventure); Assert.IsFalse(run.Resolve(true, 0));
            run.BeginCombat(); Assert.IsFalse(run.Resolve(true, 2)); Assert.IsTrue(run.Resolve(true, 0));
            Assert.IsTrue(run.GrantRoomReward()); Assert.IsFalse(run.GrantRoomReward()); Assert.AreEqual(10, run.Coins);
            Assert.IsTrue(run.Complete(true)); Assert.IsFalse(run.Complete(true));
        }
        [Test]
        public void SimultaneousDeathAndClearCannotSucceed()
        {
            var run = new RunContext(GameMode.Adventure); run.BeginCombat(); run.Resolve(false, 0);
            Assert.AreEqual(RunPhase.Dead, run.Phase); Assert.IsFalse(run.GrantRoomReward()); Assert.IsFalse(run.Complete(false));
        }
        [Test]
        public void DeathWinsEvenAtAlreadyOpenedExit()
        {
            var run = new RunContext(GameMode.Adventure); run.BeginCombat(); run.Resolve(true, 0);
            Assert.IsFalse(run.Complete(false)); Assert.AreEqual(RunPhase.Dead, run.Phase);
        }
        [Test]
        public void ModesAndNewRunsOwnIndependentState()
        {
            var adventure = new RunContext(GameMode.Adventure); adventure.BeginCombat(); adventure.AddCoins(30);
            var training = new RunContext(GameMode.Training); training.BeginCombat(); training.AddCoins(500);
            Assert.AreEqual(30, adventure.Coins); Assert.IsFalse(training.RecordEligible);
            adventure.InvalidateRecord(); Assert.IsFalse(adventure.RecordEligible);
            Assert.AreEqual(0, new RunContext(GameMode.Adventure).Coins);
        }
        [Test]
        public void AllTranslationsHaveMatchingKeysAndNamedParameters()
        {
            var text = new LocalizationService("en"); Assert.IsEmpty(text.Validate());
            Assert.AreEqual("Coins 42", text.Get("hud.coins", ("coins", "42")));
            text.SetLanguage("zh-CN"); Assert.AreEqual("金币 42", text.Get("hud.coins", ("coins", "42")));
        }
        [Test]
        public void SettingsRoundtripRecoveryPreservesDamagedOriginal()
        {
            string directory = Path.Combine(Path.GetTempPath(), "StarfallM1-" + Guid.NewGuid());
            try
            {
                var store = new SettingsStore(directory);
                Assert.IsTrue(store.Save(new GameSettings { language = "zh-CN", languageSelected = true }));
                Assert.IsTrue(store.Save(new GameSettings { language = "en", languageSelected = true }));
                Assert.AreEqual("en", new SettingsStore(directory).Load("en").language);
                File.WriteAllText(Path.Combine(directory, "starfall-settings.json"), "broken-json");
                var recovery = new SettingsStore(directory); Assert.AreEqual("zh-CN", recovery.Load("en").language);
                Assert.IsTrue(recovery.ReadProblem); Assert.AreEqual(1, Directory.GetFiles(directory, "*.corrupt-*").Length);
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }
        [Test]
        public void UnsupportedLanguageFallsBackToSystemDefault()
        {
            string directory = Path.Combine(Path.GetTempPath(), "StarfallM1-" + Guid.NewGuid());
            try
            {
                var store = new SettingsStore(directory); store.Save(new GameSettings { language = "invalid" });
                Assert.AreEqual("en", new SettingsStore(directory).Load("en").language);
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }
    }
}
