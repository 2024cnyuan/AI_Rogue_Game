using UnityEngine;

namespace Starfall
{
    public enum EnemyStyle { Basic, Turret, Shield, Bomber, Frost, Flanker }
    public sealed class PrototypeEnemy : MonoBehaviour
    {
        StarfallGame game;
        bool shooter;
        EnemyStyle style;
        Vector2 blastTarget;
        float damageScale = 1;
        Rigidbody2D body;
        SpriteRenderer view, warning, healthBar;
        Vector2 velocity, lockedAim;
        float attackTimer, warningLeft, hitLeft;
        bool winding;
        public Damageable Health { get; private set; }
        public bool Alive => Health != null && Health.State.Alive;
        public void Initialize(StarfallGame owner, bool ranged, bool elite = false, float strength = 1, EnemyStyle type = EnemyStyle.Basic)
        {
            game = owner; shooter = ranged || type == EnemyStyle.Turret || type == EnemyStyle.Bomber || type == EnemyStyle.Frost; damageScale = strength; style = type;
            body = PrototypeVisuals.Body(gameObject, .33f);
            view = PrototypeVisuals.Draw(transform, ranged ? "Shooter" : "Chaser", Vector2.zero, new Vector2(.8f, .8f),
                ranged ? new Color(.88f, .56f, .92f) : PrototypeVisuals.Enemy, 5, ranged ? "shooter" : "chaser");
            warning = PrototypeVisuals.Draw(transform, "Attack warning", Vector2.zero, Vector2.one, PrototypeVisuals.Gold, 4); warning.enabled = false;
            healthBar = PrototypeVisuals.Draw(transform, "Enemy health", new Vector2(0, .62f), new Vector2(.7f, .07f), PrototypeVisuals.Enemy, 8);
            Health = gameObject.AddComponent<Damageable>(); Health.Initialize(Faction.Enemy, ranged ? game.Config.shooterHealth : game.Config.chaserHealth, 0);
            if (elite) { Health.State.SetMaximum(Health.State.Maximum * 2); view.transform.localScale *= 1.25f; }
            Health.Hit += OnHit; Health.Died += OnDeath; attackTimer = ranged ? .9f : .3f;
            if (style == EnemyStyle.Shield) PrototypeVisuals.Draw(transform, "Shield plate", new Vector2(.45f, 0), new Vector2(.16f, .9f), new Color(.5f, .65f, .72f), 6);
            if (style == EnemyStyle.Turret) PrototypeVisuals.Draw(transform, "Turret base", Vector2.zero, new Vector2(1.1f, 1.1f), new Color(.5f, .35f, .2f), 3);
            if (style == EnemyStyle.Flanker) view.transform.localScale = new Vector3(.6f, .9f, 1);
        }
        void OnHit() { hitLeft = .09f; game.Audio?.Play(GameSound.Hit); healthBar.transform.localScale = new Vector3(.7f * Health.State.Health / Health.State.Maximum, .07f, 1); }
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
            if (style == EnemyStyle.Shield)
            {
                // Armor faces right; move behind the visible plate for full damage.
                Health.DamageMultiplier = toward.x > 0 ? .5f : 1;
            }
            hitLeft -= dt;
            view.color = hitLeft > 0 && !game.Settings.reduceFlash ? Color.white : shooter ? new Color(.88f, .56f, .92f) : PrototypeVisuals.Enemy;
            if (winding)
            {
                velocity = Vector2.zero; warningLeft -= dt;
                warning.color = Color.Lerp(PrototypeVisuals.Gold, PrototypeVisuals.Enemy, 1 - warningLeft / (shooter ? game.Config.shooterWarning : .4f));
                if (warningLeft <= 0)
                {
                    winding = false; warning.enabled = false; attackTimer = shooter ? game.Config.shooterRecovery : .8f;
                    if (style == EnemyStyle.Bomber)
                    {
                        if (Vector2.Distance(game.Player.Body.position, blastTarget) < 1.4f) game.Player.Health.Receive(new DamageContext(10 * damageScale, Faction.Enemy));
                        attackTimer = 1.8f;
                    }
                    else if (shooter)
                    {
                        if (!Physics2D.Raycast(from, lockedAim, .65f, 1))
                            game.Projectiles.Spawn(from + lockedAim * .6f, lockedAim, Faction.Enemy, game.Config.enemyBulletDamage * damageScale, game.Config.enemyBulletSpeed, style == EnemyStyle.Frost ? "frost" : "pistol");
                    }
                    else if (distance < 1.2f && sight) game.Player.Health.Receive(new DamageContext(game.Config.contactDamage * damageScale, Faction.Enemy));
                }
                return;
            }
            attackTimer -= dt;
            if (attackTimer <= 0 && sight && distance < (shooter ? 8.5f : 1.05f))
            {
                winding = true; warningLeft = shooter ? game.Config.shooterWarning : .4f;
                if (style == EnemyStyle.Bomber) warningLeft = .8f;
                lockedAim = toward.normalized; warning.enabled = true;
                warning.transform.localScale = shooter ? new Vector3(3, .07f, 1) : new Vector3(1.3f, 1.3f, 1);
                warning.sprite = PrototypeVisuals.Sprite(shooter ? "square" : "orb");
                warning.transform.localPosition = shooter ? lockedAim * 1.9f : Vector2.zero;
                warning.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(lockedAim.y, lockedAim.x) * Mathf.Rad2Deg);
                if (style == EnemyStyle.Bomber)
                {
                    blastTarget = target; warning.transform.position = target; warning.transform.rotation = Quaternion.identity;
                    warning.sprite = PrototypeVisuals.Sprite("orb"); warning.transform.localScale = new Vector3(2.8f, 2.8f, 1);
                }
                velocity = Vector2.zero; return;
            }
            velocity = (shooter && distance < 6 && sight) ? Vector2.zero : game.Room.PathDirection(from, target) * (shooter ? game.Config.shooterSpeed : game.Config.chaserSpeed);
            if (style == EnemyStyle.Turret) velocity = Vector2.zero;
            if (style == EnemyStyle.Shield) velocity *= .75f;
            if (style == EnemyStyle.Flanker)
            {
                Vector2 flank = target + new Vector2(toward.y, -toward.x).normalized * 2;
                if (game.Room.IsClear(flank, .5f) && distance > 2) velocity = game.Room.PathDirection(from, flank) * game.Config.chaserSpeed * 1.15f;
            }
        }
        void FixedUpdate() { if (body != null) body.linearVelocity = game != null && game.CanAct && Alive ? velocity : Vector2.zero; }
        public void Stop() { velocity = Vector2.zero; if (body != null) body.linearVelocity = Vector2.zero; }
        void OnDestroy() { if (Health != null) { Health.Hit -= OnHit; Health.Died -= OnDeath; } }
    }
}
