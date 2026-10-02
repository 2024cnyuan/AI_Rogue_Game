using UnityEngine;

namespace Starfall
{
    public sealed class TrainingDirector
    {
        readonly StarfallGame game;
        public int SimulationCount { get; private set; } = 3;
        public TrainingDirector(StarfallGame owner)
        {
            game = owner; game.Loadout.InfiniteEnergy = game.Loadout.InfiniteCharges = game.Loadout.Invincible = true; Build();
        }
        void Build()
        {
            game.AddStation("training.gear", "zone.equipment", new Vector2(-8, -4), () => game.ModeUI.Open(PracticePanel.Equipment));
            game.AddStation("training.range", "zone.range", new Vector2(-7, 2), () => game.ModeUI.Open(PracticePanel.Range));
            game.AddStation("training.dodge", "zone.dodge", new Vector2(4, 5), () => game.ModeUI.Open(PracticePanel.Dodge));
            game.AddStation("training.simulation", "zone.simulation", new Vector2(8, -5), () => game.ModeUI.Open(PracticePanel.Simulation));
            game.AddStation("training.environment", "zone.environment", new Vector2(-1, -4), () => game.ModeUI.Open(PracticePanel.Environment));
            game.AddTarget("training.static", new Vector2(-9, 4));
            game.AddTarget("training.moving", new Vector2(-6, 4), true);
            game.AddTarget("training.armored", new Vector2(-4, 4), false, true);
            game.Room.OpenDoor();
        }
        public void ToggleEnergy() { game.Loadout.InfiniteEnergy = !game.Loadout.InfiniteEnergy; }
        public void ToggleCharges() { game.Loadout.InfiniteCharges = !game.Loadout.InfiniteCharges; }
        public void ToggleInvincible() { game.Loadout.Invincible = !game.Loadout.Invincible; game.Player.Health.Invulnerable = game.Loadout.Invincible || game.Loadout.ShieldLeft > 0; }
        public void SetCount(int count) { SimulationCount = Mathf.Clamp(count, 1, 12); }
        public void Simulate(string type)
        {
            StopSimulation();
            for (int index = 0; index < SimulationCount; index++)
            {
                Vector2 candidate = new Vector2(5 + index % 3 * 1.8f, -3.7f + index / 3 * 1.6f);
                if (Vector2.Distance(candidate, game.Player.Body.position) < 1.5f) candidate = new Vector2(-8 + index % 3 * 2, -.5f + index / 3 * 1.4f);
                game.SpawnEnemy(candidate, type == "shooter" || type == "mix" && index % 2 == 0, type == "elite");
            }
        }
        public void StopSimulation() { game.ClearEnemies(); game.Projectiles.Clear(); }
        public void Reset(bool defaults = false)
        {
            game.ClearPracticeObjects(); game.Context.ClearPracticeCounters(); if (defaults) game.Loadout.DefaultEquipment(); else game.Loadout.ClearEffects();
            game.Loadout.Restore(); game.Player.RestoreAt(game.Room.Spawn); game.ResetPracticeStats(); game.Input.Flush(); Build();
        }
        public void Restore()
        {
            game.Loadout.Restore(); game.Player.Health.State.Restore(); game.Player.ResetCooldowns(); game.Input.Flush();
        }
        public void OnDeath() { Reset(); game.Notify("training.respawn"); }
    }
}
