using System;
using UnityEngine;

namespace Starfall
{
    public readonly struct DamageContext
    {
        public readonly float Amount;
        public readonly Faction Source;
        public readonly string Weapon;
        public readonly int Depth;
        public readonly Vector2 Knockback;
        public DamageContext(float amount, Faction source, string weapon = "pistol", int depth = 0, Vector2 knockback = default)
        { Amount = amount; Source = source; Weapon = weapon; Depth = depth; Knockback = knockback; }
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
        public StarfallGame Game { get; set; }
        public DamageContext LastDamage { get; private set; }
        float controlLeft, slow = 1;
        Vector2 impulse;
        public bool Controlled => controlLeft > 0;
        public float SpeedMultiplier => Controlled ? slow : 1;
        public void Control(float duration, float speed) { controlLeft = Mathf.Max(controlLeft, duration); slow = Mathf.Min(slow, Mathf.Clamp(speed, .2f, 1)); }
        public void ClearStatus() { controlLeft = 0; slow = 1; impulse = Vector2.zero; }
        public Vector2 Impulse => impulse;
        void Update() { if (Game == null || !Game.CanAct) return; controlLeft = Mathf.Max(0, controlLeft - Time.deltaTime); if (controlLeft == 0) slow = 1; impulse = Vector2.MoveTowards(impulse, Vector2.zero, Time.deltaTime * 12); }
        public void Initialize(Faction faction, float health, float protection)
        { Faction = faction; State = new VitalState(health); ProtectionDuration = protection; }
        public bool Receive(DamageContext context)
        {
            if (State == null || context.Source == Faction || context.Depth > 2 || context.Amount <= 0 || float.IsNaN(context.Amount) || float.IsInfinity(context.Amount)) return false;
            if (!State.Alive) return false;
            if (Invulnerable) { Evaded?.Invoke(context); return false; }
            float before = State.Health;
            float amount = Game != null ? Game.ModifyDamage(this, context) : context.Amount;
            if (!State.Damage(amount * Mathf.Clamp(DamageMultiplier, .1f, 2), Time.timeAsDouble, false, ProtectionDuration)) return false;
            LastDamage = context; impulse = Vector2.ClampMagnitude(context.Knockback, 8);
            Game?.Feedback?.Damage(this, before - State.Health, Game.CurrentDamageCritical);
            Damaged?.Invoke(context, before - State.Health);
            Hit?.Invoke();
            if (!State.Alive) Died?.Invoke();
            return true;
        }
        public bool Heal(float amount) => State != null && State.Heal(amount);
        void OnDestroy() { Died = null; Hit = null; Damaged = null; Evaded = null; }
    }
}
