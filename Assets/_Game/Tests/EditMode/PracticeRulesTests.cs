using NUnit.Framework;
using UnityEngine;

namespace Starfall.Tests
{
    public sealed class PracticeRulesTests
    {
        ItemCatalog catalog;
        [SetUp] public void Setup() { catalog = ItemCatalog.Defaults(); }
        [TearDown] public void Cleanup() { Object.DestroyImmediate(catalog); }
        [Test] public void CatalogHasNineUsableItemsAndExplicitFutureContent()
        {
            int usable = 0; var text = new LocalizationService("en");
            foreach (var item in catalog.items)
            {
                Assert.AreSame(item, catalog.Find(item.id));
                Assert.AreNotEqual(item.nameKey, text.Get(item.nameKey));
                if (item.implemented) { usable++; Assert.IsFalse(catalog.Describe(text, item).Contains("{")); }
            }
            Assert.AreEqual(24, catalog.items.Length); Assert.AreEqual(9, usable);
        }
        [Test] public void EquipmentLimitsAndIndependentCopiesCannotLeakBuilds()
        {
            var loadout = new LoadoutState(catalog); Assert.IsTrue(loadout.Equip("shotgun"));
            loadout.Switch(false); Assert.AreEqual("pistol", loadout.Weapon); loadout.Switch(true); Assert.AreEqual("shotgun", loadout.Weapon);
            Assert.IsFalse(loadout.Equip("crossbow"));
            Assert.IsTrue(loadout.Equip("rapid")); Assert.IsTrue(loadout.Equip("rapid")); Assert.IsFalse(loadout.Equip("rapid"));
            var copy = loadout.Copy(); copy.RemovePassive("rapid"); copy.Equip("smg");
            Assert.AreEqual(2, loadout.Layers("rapid")); Assert.AreEqual("shotgun", loadout.SpecialWeapon);
            Assert.That(loadout.FireRateMultiplier, Is.InRange(.45f, 1));
            loadout.DefaultEquipment(); Assert.IsNull(loadout.SpecialWeapon); Assert.IsEmpty(loadout.Passives);
        }
        [Test] public void FailedActivesAreAtomicAndAssistsKeepNormalCooldowns()
        {
            var loadout = new LoadoutState(catalog); var health = new VitalState(100); loadout.Equip("medkit");
            Assert.AreEqual("active.full", loadout.TryUse(health, false)); Assert.AreEqual(2, loadout.Charges); Assert.AreEqual(0, loadout.ActiveCooldown);
            health.Damage(45, 0, false, 0); Assert.AreEqual("active.dodging", loadout.TryUse(health, true));
            Assert.IsNull(loadout.TryUse(health, false)); Assert.AreEqual(95, health.Health); Assert.AreEqual(1, loadout.Charges);
            Assert.AreEqual("active.cooldown", loadout.TryUse(health, false)); loadout.Tick(9);
            loadout.Equip("shield"); loadout.InfiniteCharges = true;
            Assert.IsNull(loadout.TryUse(health, false)); Assert.AreEqual(2, loadout.Charges); Assert.AreEqual(3, loadout.ShieldLeft);
            Assert.AreEqual("active.cooldown", loadout.TryUse(health, false)); loadout.Tick(3.1f); Assert.AreEqual(0, loadout.ShieldLeft); Assert.Greater(loadout.ActiveCooldown, 0);
            loadout.Equip("medkit"); Assert.AreEqual(0, loadout.ShieldLeft);
        }
        [Test] public void EnergyFailureDoesNotSpendAndPistolAlwaysWorks()
        {
            var loadout = new LoadoutState(catalog); Assert.IsTrue(loadout.SpendEnergy(96));
            Assert.IsFalse(loadout.SpendEnergy(12)); Assert.AreEqual(4, loadout.Energy);
            Assert.IsTrue(loadout.SpendEnergy(0)); loadout.InfiniteEnergy = true; Assert.IsTrue(loadout.SpendEnergy(12)); Assert.AreEqual(4, loadout.Energy);
            loadout.Restore(); Assert.AreEqual(100, loadout.Energy); Assert.IsFalse(loadout.AddEnergy(35));
        }
        [Test] public void SixthPassiveTypeIsAllowedAndSeventhIsRejectedWithoutReplacing()
        {
            // Future definitions exercise the slot rule without claiming their effects are implemented.
            foreach (var item in catalog.items) if (item.kind == ItemKind.Passive) item.implemented = true;
            var loadout = new LoadoutState(catalog);
            foreach (string id in new[] { "rapid", "magnet", "vitality", "agile", "pierce", "bounce" }) Assert.IsTrue(loadout.Equip(id));
            Assert.IsFalse(loadout.Equip("critical")); Assert.AreEqual(6, loadout.Passives.Count);
            Assert.IsTrue(loadout.Equip("rapid")); Assert.IsFalse(loadout.Equip("rapid")); Assert.IsTrue(loadout.RemovePassive("bounce"));
            Assert.IsTrue(loadout.Equip("critical")); Assert.AreEqual(6, loadout.Passives.Count);
        }
        [Test] public void DamageWindowExpiresAtTenSecondsAndResetClearsTotals()
        {
            var window = new DamageWindow(); window.Reset(100); window.Record(101, 20); window.Record(105, 40);
            Assert.AreEqual(12, window.Dps(105)); Assert.AreEqual(40, window.Last); Assert.AreEqual(60, window.Total);
            Assert.AreEqual(4, window.Dps(111)); Assert.AreEqual(0, window.Dps(115)); Assert.AreEqual(60, window.Total);
            window.Reset(120); Assert.AreEqual(0, window.Total); Assert.AreEqual(0, window.Dps(120));
        }
        [Test] public void OnlyTutorialCanFinishThroughTutorialSettlement()
        {
            var training = new RunContext(GameMode.Training); training.BeginCombat(); Assert.IsFalse(training.FinishTutorial(true));
            var tutorial = new RunContext(GameMode.Tutorial); tutorial.BeginCombat(); Assert.IsFalse(tutorial.FinishTutorial(false));
            Assert.IsTrue(tutorial.FinishTutorial(true)); Assert.IsFalse(tutorial.FinishTutorial(true)); Assert.IsFalse(tutorial.RecordEligible);
        }
        [Test] public void IncreasingMaximumHealthCannotReviveDeadActors()
        {
            var health = new VitalState(100); health.Damage(1000, 0, false, 0); health.SetMaximum(150);
            Assert.IsFalse(health.Alive); health.Restore(); Assert.AreEqual(150, health.Health);
        }
    }
}
