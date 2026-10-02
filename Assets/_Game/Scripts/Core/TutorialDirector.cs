using UnityEngine;

namespace Starfall
{
    public enum TutorialStep { MoveAim, Targets, Dodge, Equipment, Tools, MapBeacon, Combat, Complete }
    public sealed class TutorialDirector
    {
        readonly StarfallGame game;
        LoadoutState entryEquipment;
        bool firstTarget, secondTarget, collected, specialHit, switchedBack, noEnergy, activeUsed, passiveChosen, mapOpened, mapClosed, beacon;
        float stepTime;
        public TutorialStep Step { get; private set; }
        public bool StrengthenedHint => stepTime >= 25;
        public int Revision { get; private set; }
        public int RetryCount { get; private set; }
        public bool StepSkipped { get; private set; }
        string chosenPassive;
        float passiveBefore, passiveAfter;
        public string PassiveComparison => chosenPassive == null ? null : game.Text.Get("tutorial.comparison." + chosenPassive,
            ("before", passiveBefore.ToString("0.00")), ("after", passiveAfter.ToString("0.00")));
        public TutorialDirector(StarfallGame owner) { game = owner; Enter(TutorialStep.MoveAim); }
        public Vector2 Marker => Step == TutorialStep.MoveAim ? new Vector2(-8, 1) : Step == TutorialStep.Targets ? new Vector2(-6, 3) :
            Step == TutorialStep.Dodge ? new Vector2(-4, 0) : Step == TutorialStep.Equipment ? new Vector2(-1, -4) :
            Step == TutorialStep.Tools ? new Vector2(2, -4) : Step == TutorialStep.MapBeacon ? new Vector2(7, 3) : game.Room.Exit;
        public string ObjectiveKey => "tutorial.step." + (int)Step;
        public void Tick(float delta)
        {
            if (Step == TutorialStep.Complete || !game.CanAct) return;
            stepTime += delta;
            if (Step == TutorialStep.MoveAim && Vector2.Distance(game.Player.Body.position, Marker) < .75f &&
                Vector2.Dot(game.Player.Aim, (new Vector2(-7, 4) - game.Player.Body.position).normalized) > .92f) Advance();
            else if (Step == TutorialStep.Equipment)
            {
                if (game.Loadout.Weapon == "pistol" && specialHit) switchedBack = true;
                if (collected && specialHit && switchedBack && noEnergy && game.Loadout.Weapon == "pistol") Advance();
            }
            else if (Step == TutorialStep.Tools && activeUsed && passiveChosen) Advance();
            else if (Step == TutorialStep.Combat && game.LivingEnemies == 0)
            {
                game.Room.OpenDoor();
                if (game.Input.InteractPressed && Vector2.Distance(game.Player.Body.position, game.Room.Exit) < 1.8f) Finish();
            }
        }
        void Enter(TutorialStep step)
        {
            Step = step; Revision++; stepTime = 0;
            firstTarget = secondTarget = collected = specialHit = switchedBack = noEnergy = activeUsed = passiveChosen = mapOpened = mapClosed = beacon = false;
            game.ClearPracticeObjects(); game.Loadout.Restore(); game.Player.ResetCooldowns();
            entryEquipment = game.Loadout.Copy();
            game.MarkerAt(Marker);
            if (step == TutorialStep.MoveAim) game.MarkerAt(new Vector2(-7, 4));
            if (step == TutorialStep.Targets) { game.AddTarget("tutorial.first", new Vector2(-7, 4)); game.AddTarget("tutorial.cover", new Vector2(-2, 4)); }
            else if (step == TutorialStep.Dodge) game.Hazards.Begin(1);
            else if (step == TutorialStep.Equipment)
            {
                game.Drop(new Vector2(-3, -4), SupplyKind.Energy); game.Drop(new Vector2(-4, -4), SupplyKind.Coin);
                game.AddStation("tutorial.weapon", "item.shotgun", new Vector2(-1, -4), () => game.EquipItem("shotgun"));
                game.AddTarget("tutorial.special", new Vector2(-1, -1.5f));
            }
            else if (step == TutorialStep.Tools)
            {
                game.Player.Health.State.Damage(45, Time.timeAsDouble + 1, false, 0);
                game.AddStation("tutorial.active", "item.medkit", new Vector2(2, -4), () => game.EquipItem("medkit"));
                game.AddStation("tutorial.passive.rapid", "item.rapid", new Vector2(4, -4), () => ChoosePassive("rapid"));
                game.AddStation("tutorial.passive.vitality", "item.vitality", new Vector2(6, -4), () => ChoosePassive("vitality"));
            }
            else if (step == TutorialStep.MapBeacon)
                game.AddStation("tutorial.beacon", "station.beacon", new Vector2(7, 3), ActivateBeacon);
            else if (step == TutorialStep.Combat)
            {
                game.Room.CloseDoor();
                game.SpawnEnemy(new Vector2(6, -3), false, false, .35f);
                game.SpawnEnemy(new Vector2(8, 4), true, false, .35f);
                game.SpawnEnemy(new Vector2(8, 0), false, true, .35f);
            }
        }
        void ChoosePassive(string id)
        {
            if (Step != TutorialStep.Tools || passiveChosen || !activeUsed) { game.Notify("tutorial.useFirst"); return; }
            float before = id == "rapid" ? game.Catalog.Find("pistol").interval * game.Loadout.FireRateMultiplier : game.Player.Health.State.Maximum;
            if (game.EquipItem(id))
            {
                chosenPassive = id; passiveBefore = before;
                passiveAfter = id == "rapid" ? game.Catalog.Find("pistol").interval * game.Loadout.FireRateMultiplier : game.Player.Health.State.Maximum;
                passiveChosen = true; game.Notify("tutorial.passiveChanged");
            }
        }
        public void TargetHit(string id, string weapon)
        {
            if (Step == TutorialStep.Targets)
            {
                if (id == "tutorial.first") firstTarget = true;
                if (id == "tutorial.cover") secondTarget = true;
                if (firstTarget && secondTarget) Advance();
            }
            else if (Step == TutorialStep.Equipment && id == "tutorial.special" && weapon == "shotgun") specialHit = true;
        }
        public void DodgedAttack() { if (Step == TutorialStep.Dodge) Advance(); }
        public void SupplyCollected() { if (Step == TutorialStep.Equipment) collected = true; }
        public void EnergyFailed() { if (Step == TutorialStep.Equipment) noEnergy = true; }
        public void ActiveUsed() { if (Step == TutorialStep.Tools) activeUsed = true; }
        public void MapChanged(bool open)
        {
            if (Step != TutorialStep.MapBeacon) return;
            if (open) mapOpened = true; else if (mapOpened) mapClosed = true;
        }
        void ActivateBeacon()
        {
            if (Step != TutorialStep.MapBeacon || !mapClosed) { game.Notify("tutorial.mapFirst"); return; }
            beacon = true; game.Room.OpenDoor(); game.Notify("tutorial.beaconReady");
        }
        public void TryDoor()
        {
            if (Step == TutorialStep.MapBeacon && beacon && Vector2.Distance(game.Player.Body.position, game.Room.Exit) < 1.8f) Advance();
        }
        void Advance() { if (Step < TutorialStep.Complete) Enter(Step + 1); }
        public void SkipDodge()
        {
            if (Step != TutorialStep.Dodge || !StrengthenedHint) return;
            StepSkipped = true; game.SaveTutorialMark(false, true); Advance();
        }
        public void Retry()
        {
            if (Step == TutorialStep.Complete) return;
            RetryCount++; game.ReplaceLoadout(entryEquipment.Copy()); game.Player.RestoreAt(SafePoint()); Enter(Step); game.Notify("tutorial.retryDone");
        }
        Vector2 SafePoint() => Step == TutorialStep.Combat ? new Vector2(5, -5) : Step == TutorialStep.Dodge ? new Vector2(-4, 0) : game.Room.Spawn;
        void Finish()
        {
            if (!game.Context.FinishTutorial(game.Player.Health.State.Alive)) return;
            Step = TutorialStep.Complete; Revision++; game.ClearPracticeObjects(); game.Player.Flush();
            game.SaveTutorialMark(true, StepSkipped); game.Interface.RefreshNow();
        }
        public void SkipAll() { game.SaveTutorialMark(false, true); game.ReturnToMenu(); }
    }
}
