using UnityEngine;

namespace Starfall
{
    public enum CaptainMove { Fan, Charge, Ring }
    public sealed class StoneCaptain : MonoBehaviour
    {
        StarfallGame game;
        Rigidbody2D body;
        SpriteRenderer view, warning;
        float remaining, burstClock;
        int burst, sequence;
        bool executing, recovering, stopped;
        Vector2 direction;
        public Damageable Health { get; private set; }
        public CaptainMove Move { get; private set; }
        public bool Warning => !stopped && !executing && !recovering;
        public void Initialize(StarfallGame owner)
        {
            game = owner; body = PrototypeVisuals.Body(gameObject, .62f);
            view = PrototypeVisuals.Draw(transform, "Stone Captain", Vector2.zero, new Vector2(1.5f, 1.5f), new Color(.5f, .7f, .5f), 5, "chaser");
            warning = PrototypeVisuals.Draw(transform, "Captain warning", Vector2.zero, Vector2.one, PrototypeVisuals.Gold, 4, "orb");
            Health = gameObject.AddComponent<Damageable>(); Health.Initialize(Faction.Enemy, game.LevelConfig.bossHealth, 0);
            Health.Hit += Hit; Health.Died += Die; BeginWarning();
        }
        void Hit() { game.Audio?.Play(GameSound.Hit); }
        void Die()
        {
            stopped = true; body.linearVelocity = Vector2.zero; warning.enabled = false; GetComponent<Collider2D>().enabled = false;
            view.color = new Color(.24f, .3f, .25f); game.Projectiles.Clear(); game.Audio?.Play(GameSound.Clear);
        }
        void BeginWarning()
        {
            Move = (CaptainMove)(sequence++ % 3); executing = recovering = false;
            remaining = game.LevelConfig.bossWarning; direction = (game.Player.Body.position - body.position).normalized;
            warning.enabled = true; warning.color = PrototypeVisuals.Gold;
            warning.transform.localPosition = Move == CaptainMove.Charge ? (Vector3)(direction * 2) : Vector3.zero;
            warning.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            warning.transform.localScale = Move == CaptainMove.Charge ? new Vector3(5, .6f, 1) : Move == CaptainMove.Ring ? new Vector3(2.5f, 2.5f, 1) : new Vector3(2, 1, 1);
            game.Audio?.Play(GameSound.Boss);
        }
        void Update()
        {
            if (stopped || game == null || !game.CanAct) { if (body != null) body.linearVelocity = Vector2.zero; return; }
            remaining -= Time.deltaTime;
            if (Warning)
            {
                warning.color = Color.Lerp(PrototypeVisuals.Enemy, PrototypeVisuals.Gold, Mathf.Clamp01(remaining / game.LevelConfig.bossWarning));
                if (remaining > 0) return;
                executing = true; warning.enabled = false; burstClock = 0; burst = 0; remaining = Move == CaptainMove.Charge ? .65f : .75f;
            }
            if (executing)
            {
                burstClock -= Time.deltaTime;
                if (Move != CaptainMove.Charge && burstClock <= 0 && burst < (Move == CaptainMove.Fan ? 3 : 1))
                {
                    burst++; burstClock = .24f;
                    int count = Move == CaptainMove.Fan ? 7 : 12;
                    for (int i = 0; i < count; i++)
                    {
                        float angle = Move == CaptainMove.Fan ? Mathf.Lerp(-60, 60, (float)i / (count - 1)) : i * 360f / count;
                        Vector2 shot = Quaternion.Euler(0, 0, angle) * (Move == CaptainMove.Fan ? direction : Vector2.right);
                        game.Projectiles.Spawn(body.position + shot * .8f, shot, Faction.Enemy, game.LevelConfig.bossBulletDamage, 4);
                    }
                }
                if (remaining <= 0) { executing = false; recovering = true; remaining = game.LevelConfig.bossRecovery; body.linearVelocity = Vector2.zero; }
            }
            else if (recovering && remaining <= 0) BeginWarning();
        }
        void FixedUpdate()
        {
            if (stopped || !game.CanAct || !executing || Move != CaptainMove.Charge) { body.linearVelocity = Vector2.zero; return; }
            float speed = 10;
            var wall = Physics2D.CircleCast(body.position, .62f, direction, speed * Time.fixedDeltaTime + .02f, 1);
            if (wall.collider != null) speed = Mathf.Min(speed, Mathf.Max(0, wall.distance - .02f) / Time.fixedDeltaTime);
            body.linearVelocity = direction * speed;
            if (Vector2.Distance(body.position, game.Player.Body.position) < 1.05f) game.Player.Health.Receive(new DamageContext(game.LevelConfig.bossChargeDamage, Faction.Enemy));
        }
        public void Stop() { stopped = true; body.linearVelocity = Vector2.zero; warning.enabled = false; }
        void OnDestroy() { if (Health != null) { Health.Hit -= Hit; Health.Died -= Die; } }
    }
}
