using UnityEngine;

namespace Starfall
{
    public sealed class PrototypeEnemy : MonoBehaviour
    {
        StarfallGame game;
        bool shooter;
        float damageScale = 1;
        Rigidbody2D body;
        SpriteRenderer view, warning, healthBar;
        Vector2 velocity, lockedAim;
        float attackTimer, warningLeft, hitLeft;
        bool winding;
        public Damageable Health { get; private set; }
        public bool Alive => Health != null && Health.State.Alive;
        public void Initialize(StarfallGame owner, bool ranged, bool elite = false, float strength = 1)
        {
            game = owner; shooter = ranged; damageScale = strength;
            body = PrototypeVisuals.Body(gameObject, .33f);
            view = PrototypeVisuals.Draw(transform, ranged ? "Shooter" : "Chaser", Vector2.zero, new Vector2(.8f, .8f),
                ranged ? new Color(.88f, .56f, .92f) : PrototypeVisuals.Enemy, 5, ranged ? "shooter" : "chaser");
            warning = PrototypeVisuals.Draw(transform, "Attack warning", Vector2.zero, Vector2.one, PrototypeVisuals.Gold, 4); warning.enabled = false;
            healthBar = PrototypeVisuals.Draw(transform, "Enemy health", new Vector2(0, .62f), new Vector2(.7f, .07f), PrototypeVisuals.Enemy, 8);
            Health = gameObject.AddComponent<Damageable>(); Health.Initialize(Faction.Enemy, ranged ? game.Config.shooterHealth : game.Config.chaserHealth, 0);
            if (elite) { Health.State.SetMaximum(Health.State.Maximum * 2); view.transform.localScale *= 1.25f; }
            Health.Hit += OnHit; Health.Died += OnDeath; attackTimer = ranged ? .9f : .3f;
        }
        void OnHit() { hitLeft = .09f; healthBar.transform.localScale = new Vector3(.7f * Health.State.Health / Health.State.Maximum, .07f, 1); }
        void OnDeath()
        {
            body.linearVelocity = Vector2.zero; GetComponent<Collider2D>().enabled = false; warning.enabled = false;
            game.EnemyKilled(this); gameObject.SetActive(false);
        }
        void Update()
        {
            if (game == null || !game.CanAct || !Alive || !game.Player.Health.State.Alive) return;
            float dt = Time.deltaTime; Vector2 from = body.position, target = game.Player.Body.position;
            Vector2 toward = target - from; float distance = toward.magnitude;
            bool sight = !Physics2D.Raycast(from, toward.normalized, distance, 1);
            hitLeft -= dt;
            view.color = hitLeft > 0 ? Color.white : shooter ? new Color(.88f, .56f, .92f) : PrototypeVisuals.Enemy;
            if (winding)
            {
                velocity = Vector2.zero; warningLeft -= dt;
                warning.color = Color.Lerp(PrototypeVisuals.Gold, PrototypeVisuals.Enemy, 1 - warningLeft / (shooter ? game.Config.shooterWarning : .4f));
                if (warningLeft <= 0)
                {
                    winding = false; warning.enabled = false; attackTimer = shooter ? game.Config.shooterRecovery : .8f;
                    if (shooter)
                    {
                        if (!Physics2D.Raycast(from, lockedAim, .65f, 1))
                            game.Projectiles.Spawn(from + lockedAim * .6f, lockedAim, Faction.Enemy, game.Config.enemyBulletDamage * damageScale, game.Config.enemyBulletSpeed);
                    }
                    else if (distance < 1.2f && sight) game.Player.Health.Receive(new DamageContext(game.Config.contactDamage * damageScale, Faction.Enemy));
                }
                return;
            }
            attackTimer -= dt;
            if (attackTimer <= 0 && sight && distance < (shooter ? 8.5f : 1.05f))
            {
                winding = true; warningLeft = shooter ? game.Config.shooterWarning : .4f;
                lockedAim = toward.normalized; warning.enabled = true;
                warning.transform.localScale = shooter ? new Vector3(3, .07f, 1) : new Vector3(1.3f, 1.3f, 1);
                warning.sprite = PrototypeVisuals.Sprite(shooter ? "square" : "orb");
                warning.transform.localPosition = shooter ? lockedAim * 1.9f : Vector2.zero;
                warning.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(lockedAim.y, lockedAim.x) * Mathf.Rad2Deg);
                velocity = Vector2.zero; return;
            }
            velocity = (shooter && distance < 6 && sight) ? Vector2.zero : game.Room.PathDirection(from, target) * (shooter ? game.Config.shooterSpeed : game.Config.chaserSpeed);
        }
        void FixedUpdate() { if (body != null) body.linearVelocity = game != null && game.CanAct && Alive ? velocity : Vector2.zero; }
        public void Stop() { velocity = Vector2.zero; if (body != null) body.linearVelocity = Vector2.zero; }
        void OnDestroy() { if (Health != null) { Health.Hit -= OnHit; Health.Died -= OnDeath; } }
    }
}
