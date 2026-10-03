using UnityEngine;

namespace Starfall
{
    public sealed class PracticeTarget : MonoBehaviour
    {
        StarfallGame game;
        SpriteRenderer view;
        Vector2 home;
        bool moving;
        public string Id { get; private set; }
        public Damageable Health { get; private set; }
        public void Initialize(StarfallGame owner, string id, bool motion, bool armored)
        {
            game = owner; Id = id; moving = motion; home = transform.position;
            view = PrototypeVisuals.Draw(transform, "Target", Vector2.zero, Vector2.one * .8f, armored ? new Color(.65f, .72f, .85f) : PrototypeVisuals.Gold, 5, armored ? "shooter" : "orb");
            M5Art.Apply(view,armored?"enemy.shield":"enemy.basic",1.5f); M5Art.Shadow(transform,.8f);
            gameObject.layer = 2; gameObject.AddComponent<CircleCollider2D>().radius = .4f;
            var body = gameObject.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
            Health = gameObject.AddComponent<Damageable>(); Health.Initialize(Faction.Enemy, 100000, 0); Health.DamageMultiplier = armored ? .5f : 1;
            Health.Game = game;
            Health.Damaged += OnDamage;
        }
        void OnDamage(DamageContext context, float actual)
        {
            game.PracticeStats.Record(game.EffectivePracticeTime, actual); game.Tutorial?.TargetHit(Id, context.Weapon);
            view.color = Color.white;
        }
        void Update()
        {
            if (game == null || !game.CanAct) return;
            if (moving) transform.position = home + new Vector2(Mathf.Sin((float)game.EffectivePracticeTime * 1.6f) * 1.1f, 0);
            view.color = Color.Lerp(view.color, Health.DamageMultiplier < 1 ? new Color(.65f, .72f, .85f) : PrototypeVisuals.Gold, Time.deltaTime * 8);
            if (Health.State.Health < 50000) Health.State.Restore();
        }
        void OnDestroy() { if (Health != null) Health.Damaged -= OnDamage; }
    }
}
