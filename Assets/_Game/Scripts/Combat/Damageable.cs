using System;
using UnityEngine;

namespace Starfall
{
    public readonly struct DamageContext
    {
        public readonly float Amount;
        public readonly Faction Source;
        public readonly string Weapon;
        public DamageContext(float amount, Faction source, string weapon = "pistol") { Amount = amount; Source = source; Weapon = weapon; }
    }
    public sealed class Damageable : MonoBehaviour
    {
        public Faction Faction { get; private set; }
        public VitalState State { get; private set; }
        public bool Invulnerable { get; set; }
        public float ProtectionDuration { get; private set; }
        public event Action Died;
        public event Action Hit;
        public event Action<DamageContext, float> Damaged;
        public event Action<DamageContext> Evaded;
        public float DamageMultiplier { get; set; } = 1;
        public void Initialize(Faction faction, float health, float protection)
        { Faction = faction; State = new VitalState(health); ProtectionDuration = protection; }
        public bool Receive(DamageContext context)
        {
            if (State == null || context.Source == Faction) return false;
            if (!State.Alive) return false;
            if (Invulnerable) { Evaded?.Invoke(context); return false; }
            float before = State.Health;
            if (!State.Damage(context.Amount * DamageMultiplier, Time.timeAsDouble, false, ProtectionDuration)) return false;
            Damaged?.Invoke(context, before - State.Health);
            Hit?.Invoke();
            if (!State.Alive) Died?.Invoke();
            return true;
        }
        public bool Heal(float amount) => State != null && State.Heal(amount);
        void OnDestroy() { Died = null; Hit = null; Damaged = null; Evaded = null; }
    }
}
