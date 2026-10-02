using UnityEngine;

namespace Starfall
{
    public sealed class PracticeHazards : MonoBehaviour
    {
        StarfallGame game;
        SpriteRenderer charge, ground;
        float clock, bulletClock;
        public bool Running { get; private set; }
        public int Intensity { get; private set; } = 1;
        public void Initialize(StarfallGame owner)
        {
            game = owner;
            charge = PrototypeVisuals.Draw(transform, "Charge lane warning", new Vector2(5, 4), new Vector2(5, .55f), PrototypeVisuals.Gold, 2);
            ground = PrototypeVisuals.Draw(transform, "Ground warning", new Vector2(7, 2.3f), new Vector2(3, .7f), PrototypeVisuals.Gold, 2);
            charge.enabled = ground.enabled = false;
        }
        public void Begin(int strength) { Stop(); Intensity = Mathf.Clamp(strength, 1, 3); Running = true; }
        public void Stop() { Running = false; clock = bulletClock = 0; if (charge != null) charge.enabled = ground.enabled = false; }
        void Update()
        {
            if (!Running || !game.CanAct) return;
            clock += Time.deltaTime; bulletClock -= Time.deltaTime;
            if (bulletClock <= 0)
            {
                bulletClock = 1.3f / Intensity;
                Vector2 origin = game.Context.Mode == GameMode.Tutorial ? new Vector2(1, 0) : new Vector2(9.6f, 4);
                game.Projectiles.Spawn(origin, Vector2.left, Faction.Enemy, 5 * Intensity, 3.5f);
            }
            if (game.Context.Mode == GameMode.Tutorial) return;
            float phase = clock % 5; bool windup = phase < .8f, active = phase >= .8f && phase < 1.3f;
            ground.enabled = windup || active; ground.color = active ? PrototypeVisuals.Enemy : PrototypeVisuals.Gold;
            bool chargeWarning = phase >= 2.5f && phase < 3.3f, chargeActive = phase >= 3.3f && phase < 3.6f;
            charge.enabled = chargeWarning || chargeActive; charge.color = chargeActive ? PrototypeVisuals.Enemy : PrototypeVisuals.Gold;
            Vector2 player = game.Player.Body.position;
            if (active && new Rect(5.5f, 1.95f, 3, .7f).Contains(player) || chargeActive && new Rect(2.5f, 3.725f, 5, .55f).Contains(player))
                game.Player.Health.Receive(new DamageContext(5 * Intensity, Faction.Enemy));
        }
    }
}
