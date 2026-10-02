using System;
using UnityEngine;

namespace Starfall
{
    public enum GameMode { None, Adventure, Tutorial, Training }
    public enum RunPhase { Menu, Loading, Combat, RoomClear, Dead, Complete }
    [Flags] public enum PauseReason { None = 0, Menu = 1, Map = 2, Focus = 4, Settings = 8, PracticePanel = 16, Loading = 32, Shop = 64 }
    public enum Faction { Player, Enemy }

    // A new context owns every mutable run value; future modes cannot inherit training aids.
    public sealed class RunContext
    {
        public GameMode Mode { get; }
        public RunPhase Phase { get; private set; } = RunPhase.Loading;
        public int Coins { get; private set; }
        public int Kills { get; private set; }
        public bool RewardGranted { get; private set; }
        public bool RecordEligible { get; private set; }
        public RunContext(GameMode mode) { Mode = mode; RecordEligible = mode == GameMode.Adventure; }
        public void BeginCombat() { if (Phase == RunPhase.Loading) Phase = RunPhase.Combat; }
        public bool Resolve(bool playerAlive, int livingEnemies)
        {
            if (Phase != RunPhase.Combat && Phase != RunPhase.RoomClear) return false;
            if (!playerAlive) { Phase = RunPhase.Dead; return true; }
            if (Phase == RunPhase.Combat && livingEnemies == 0) { Phase = RunPhase.RoomClear; return true; }
            return false;
        }
        public bool GrantRoomReward()
        {
            if (Phase != RunPhase.RoomClear || RewardGranted) return false;
            RewardGranted = true; Coins += 10; return true;
        }
        public bool Complete(bool playerAlive)
        {
            if (!playerAlive) { Resolve(false, 0); return false; }
            if (Phase != RunPhase.RoomClear) return false;
            Phase = RunPhase.Complete; return true;
        }
        public void AddCoins(int amount) { if (Phase == RunPhase.Combat || Phase == RunPhase.RoomClear) Coins += Math.Max(0, amount); }
        public void RegisterKill() { if (Phase == RunPhase.Combat) Kills++; }
        public void ClearPracticeCounters() { if (Mode == GameMode.Training || Mode == GameMode.Tutorial) Coins = Kills = 0; }
        public void InvalidateRecord() => RecordEligible = false;
        public void EndLevel(bool success) { if (Phase == RunPhase.Combat || Phase == RunPhase.RoomClear) Phase = success ? RunPhase.Complete : RunPhase.Dead; }
        public void AddCompletedCoins(int amount) { if (Phase == RunPhase.Complete) Coins += Math.Max(0, amount); }
        public bool SpendCoins(int amount) { if (amount < 0 || Coins < amount || Phase != RunPhase.Combat) return false; Coins -= amount; return true; }
        public void RestoreCoins(int amount) { if (Phase == RunPhase.Combat) Coins = Math.Max(0, amount); }
        public bool FinishTutorial(bool alive)
        {
            if (Mode != GameMode.Tutorial || Phase != RunPhase.Combat || !alive) return false;
            Phase = RunPhase.Complete; return true;
        }
    }

    public sealed class PauseState
    {
        public PauseReason Reasons { get; private set; }
        public bool IsPaused => Reasons != PauseReason.None;
        public void Set(PauseReason reason, bool enabled) { if (enabled) Reasons |= reason; else Reasons &= ~reason; }
        public bool Has(PauseReason reason) => (Reasons & reason) != 0;
        public void Clear() => Reasons = PauseReason.None;
    }

    public sealed class VitalState
    {
        public float Health { get; private set; }
        public float Maximum { get; private set; }
        public double ProtectedUntil { get; private set; }
        public bool Alive => Health > 0;
        public VitalState(float maximum) { Maximum = maximum; Health = maximum; }
        public bool Damage(float amount, double now, bool invulnerable, float protection)
        {
            if (!Alive || invulnerable || now < ProtectedUntil || amount <= 0) return false;
            Health = Mathf.Max(0, Health - amount); ProtectedUntil = now + protection; return true;
        }
        public bool Heal(float amount)
        {
            if (!Alive || Health >= Maximum || amount <= 0) return false;
            Health = Mathf.Min(Maximum, Health + amount); return true;
        }
        public void SetMaximum(float value) { float old = Maximum; Maximum = Mathf.Max(1, value); Health = Alive ? Mathf.Clamp(Health + Mathf.Max(0, Maximum - old), 0, Maximum) : 0; }
        public void Restore() { Health = Maximum; ProtectedUntil = 0; }
        public void RestoreValue(float value) { Health = Mathf.Clamp(value, 1, Maximum); ProtectedUntil = 0; }
    }
}
