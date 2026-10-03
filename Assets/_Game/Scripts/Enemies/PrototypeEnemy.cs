using UnityEngine;

namespace Starfall
{
    public enum EnemyStyle { Basic, Turret, Shield, Bomber, Frost, Flanker, Spore, Blocker, Assault, Sniper, Drone, Sporelet }
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
        float chargeLeft, senseLeft, stuckLeft, supportLeft;
        Vector2 lastPosition;
        public bool GivesReward { get; set; } = true;
        public EnemyStyle Style => style;
        public Damageable Health { get; private set; }
        public bool Alive => Health != null && Health.State.Alive;
        public void Initialize(StarfallGame owner, bool ranged, bool elite = false, float strength = 1, EnemyStyle type = EnemyStyle.Basic)
        {
            game = owner; shooter = ranged || type == EnemyStyle.Turret || type == EnemyStyle.Bomber || type == EnemyStyle.Frost || type == EnemyStyle.Blocker || type == EnemyStyle.Sniper || type == EnemyStyle.Drone; damageScale = strength; style = type;
            body = PrototypeVisuals.Body(gameObject, .33f);
            view = PrototypeVisuals.Draw(transform, ranged ? "Shooter" : "Chaser", Vector2.zero, new Vector2(.8f, .8f),
                ranged ? new Color(.88f, .56f, .92f) : PrototypeVisuals.Enemy, 5, ranged ? "shooter" : "chaser");
            warning = PrototypeVisuals.Draw(transform, "Attack warning", Vector2.zero, Vector2.one, PrototypeVisuals.Gold, 4); warning.enabled = false;
            healthBar = PrototypeVisuals.Draw(transform, "Enemy health", new Vector2(0, 1.3f), new Vector2(.7f, .045f), PrototypeVisuals.Enemy, 150);
            Health = gameObject.AddComponent<Damageable>(); Health.Initialize(Faction.Enemy, ranged ? game.Config.shooterHealth : game.Config.chaserHealth, 0);
            Health.Game = game; lastPosition = body.position;
            if (elite) { Health.State.SetMaximum(Health.State.Maximum * 2); view.transform.localScale *= 1.25f; }
            Health.Hit += OnHit; Health.Died += OnDeath; attackTimer = ranged ? .9f : .3f;
            if (style == EnemyStyle.Flanker) view.transform.localScale = new Vector3(.6f, .9f, 1);
            if (style == EnemyStyle.Spore || style == EnemyStyle.Sporelet) { view.sprite = PrototypeVisuals.Sprite("orb"); view.transform.localScale *= style == EnemyStyle.Sporelet ? .6f : 1.2f; if (style == EnemyStyle.Sporelet) { Health.State.SetMaximum(24); Health.State.Restore(); } }
            if (style == EnemyStyle.Drone) view.sprite = PrototypeVisuals.Sprite("cross");
            string art = style == EnemyStyle.Basic ? ranged ? "enemy.archer" : "enemy.basic" : "enemy." + style.ToString().ToLowerInvariant();
            M5Art.Apply(view, art, elite ? 1.9f : style == EnemyStyle.Sporelet ? .8f : 1.5f); M5Art.Shadow(transform, .8f);
        }
        void OnHit() { hitLeft = .09f; game.Audio?.Play(GameSound.Hit); healthBar.transform.localScale = new Vector3(.7f * Health.State.Health / Health.State.Maximum, .045f, 1); }
        void OnDeath()
        {
            body.linearVelocity = Vector2.zero; GetComponent<Collider2D>().enabled = false; warning.enabled = false;
            if (style == EnemyStyle.Spore && GivesReward) {
                // Children register synchronously before the director evaluates room completion. They cannot split or grant rewards.
                foreach (var offset in new[] { Vector2.up, Vector2.down }) {
                    Vector2 at = body.position + offset * .8f;
                    if (game.Room.IsClear(at, .4f)) game.SpawnEnemy(at, false, false, damageScale, EnemyStyle.Sporelet, false);
                }
            }
            game.EnemyKilled(this); gameObject.SetActive(false);
        }
        void Update()
        {
            if (game == null || !game.CanAct || !Alive || !game.Player.Health.State.Alive) return;
            float dt = Time.deltaTime; Vector2 from = body.position;
            var decoy = game.Effects.Decoy; Damageable victim = decoy != null ? decoy : game.Player.Health;
            Vector2 target = victim.transform.position;
            Vector2 toward = target - from; float distance = toward.magnitude;
            bool sight = !Physics2D.Raycast(from, toward.normalized, distance, 1);
            senseLeft -= dt;
            if (senseLeft <= 0) {
                senseLeft = 2;
                if (style != EnemyStyle.Turret && !winding && !shooter && distance > 1.5f) {
                    stuckLeft = Vector2.Distance(lastPosition, from) < .15f ? stuckLeft + 2 : 0;
                    if (stuckLeft >= 8) { foreach (var at in new[] { new Vector2(7,4), new Vector2(-6,4), new Vector2(7,-4), new Vector2(-6,-4) }) if (game.Room.IsClear(at, .4f) && Vector2.Distance(at, target) > 2) { body.position = at; break; } stuckLeft = 0; }
                }
                lastPosition = body.position;
            }
            if (style == EnemyStyle.Drone) { supportLeft -= dt; if (supportLeft <= 0) { supportLeft = 3; foreach (var ally in game.Enemies) if (ally != this && ally.Alive && Vector2.Distance(ally.transform.position, from) < 4) ally.Health.Heal(12); } }
            if (chargeLeft > 0) { chargeLeft -= dt; velocity = lockedAim * 7; if (Physics2D.CircleCast(from, .35f, lockedAim, .3f, 1)) { chargeLeft = 0; velocity = Vector2.zero; } if (distance < 1) victim.Receive(new DamageContext(9, Faction.Enemy, "assault")); return; }
            if (style == EnemyStyle.Shield)
            {
                // Armor faces right; move behind the visible plate for full damage.
                Health.DamageMultiplier = toward.x > 0 ? .5f : 1;
            }
            hitLeft -= dt;
            view.color = hitLeft > 0 && !game.Settings.reduceFlash ? new Color(1, .66f, .54f) : Color.white;
            if (winding)
            {
                velocity = Vector2.zero; warningLeft -= dt;
                warning.color = Color.Lerp(PrototypeVisuals.Gold, PrototypeVisuals.Enemy, 1 - warningLeft / (shooter ? game.Config.shooterWarning : .4f));
                if (warningLeft <= 0)
                {
                    winding = false; warning.enabled = false; attackTimer = shooter ? game.Config.shooterRecovery : .8f;
                    if (style == EnemyStyle.Assault) { chargeLeft = .35f; attackTimer = 1.8f; }
                    else if (style == EnemyStyle.Blocker) { game.Effects.HostileArea(blastTarget, 1.5f, .8f, 1.4f, "blocker"); attackTimer = 2.8f; }
                    else if (style == EnemyStyle.Bomber)
                    {
                        if (Vector2.Distance(victim.transform.position, blastTarget) < 1.4f) victim.Receive(new DamageContext(10 * damageScale, Faction.Enemy));
                        attackTimer = 1.8f;
                    }
                    else if (shooter)
                    {
                        if (!Physics2D.Raycast(from, lockedAim, .65f, 1))
                            game.Projectiles.Spawn(from + lockedAim * .6f, lockedAim, Faction.Enemy, (style == EnemyStyle.Sniper ? 12 : game.Config.enemyBulletDamage) * damageScale, style == EnemyStyle.Sniper ? 9 : game.Config.enemyBulletSpeed, style == EnemyStyle.Frost ? "frost" : "pistol");
                    }
                    else if (distance < 1.2f && sight) victim.Receive(new DamageContext(game.Config.contactDamage * damageScale, Faction.Enemy));
                }
                return;
            }
            attackTimer -= dt;
            if (attackTimer <= 0 && sight && distance < (shooter ? 10 : style == EnemyStyle.Assault ? 4 : 1.05f))
            {
                winding = true; warningLeft = shooter ? game.Config.shooterWarning : .4f;
                if (style == EnemyStyle.Bomber) warningLeft = .8f;
                if (style == EnemyStyle.Sniper) warningLeft = .9f;
                if (style == EnemyStyle.Assault) warningLeft = .65f;
                lockedAim = toward.normalized; warning.enabled = true;
                warning.transform.localScale = shooter ? new Vector3(3, .07f, 1) : new Vector3(1.3f, 1.3f, 1);
                warning.sprite = PrototypeVisuals.Sprite(shooter ? "square" : "orb");
                warning.transform.localPosition = shooter ? lockedAim * 1.9f : Vector2.zero;
                warning.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(lockedAim.y, lockedAim.x) * Mathf.Rad2Deg);
                if (style == EnemyStyle.Bomber || style == EnemyStyle.Blocker)
                {
                    blastTarget = target; warning.transform.position = target; warning.transform.rotation = Quaternion.identity;
                    warning.sprite = PrototypeVisuals.Sprite("orb"); warning.transform.localScale = new Vector3(2.8f, 2.8f, 1);
                }
                velocity = Vector2.zero; return;
            }
            velocity = (shooter && distance < 6 && sight) ? Vector2.zero : game.Room.PathDirection(from, target) * (shooter ? game.Config.shooterSpeed : game.Config.chaserSpeed);
            if (style == EnemyStyle.Turret) velocity = Vector2.zero;
            if (style == EnemyStyle.Shield) velocity *= .75f;
            if (style == EnemyStyle.Assault) velocity *= 1.5f;
            if (style == EnemyStyle.Sporelet) velocity *= 1.2f;
            if (style == EnemyStyle.Flanker)
            {
                Vector2 flank = target + new Vector2(toward.y, -toward.x).normalized * 2;
                if (game.Room.IsClear(flank, .5f) && distance > 2) velocity = game.Room.PathDirection(from, flank) * game.Config.chaserSpeed * 1.15f;
            }
        }
        void FixedUpdate() { if (body != null) body.linearVelocity = game != null && game.CanAct && Alive ? velocity * Health.SpeedMultiplier + Health.Impulse : Vector2.zero; }
        public void Stop() { velocity = Vector2.zero; if (body != null) body.linearVelocity = Vector2.zero; }
        void OnDestroy() { if (Health != null) { Health.Hit -= OnHit; Health.Died -= OnDeath; } }
    }
}
