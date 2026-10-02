using UnityEngine;
using UnityEngine.EventSystems;

namespace Starfall
{
    public sealed class ExplorerController : MonoBehaviour
    {
        StarfallGame game;
        PrototypeConfig config;
        Rigidbody2D body;
        SpriteRenderer view, gun, flash;
        Vector2 move, aim = Vector2.right, dodgeDirection;
        float dodgeLeft, dodgeAge, cooldown, bufferLeft, shotLeft, hitLeft, flashLeft;
        public Damageable Health { get; private set; }
        public bool IsDodging => dodgeLeft > 0;
        public float DodgeCooldown => cooldown;
        public Vector2 Aim => aim;
        public Rigidbody2D Body => body;
        public bool DodgeProtected => IsDodging && dodgeAge < config.dodgeInvulnerability;
        public void Initialize(StarfallGame owner, PrototypeConfig settings)
        {
            game = owner; config = settings;
            view = PrototypeVisuals.Draw(transform, "Explorer", Vector2.zero, new Vector2(.8f, .9f), PrototypeVisuals.Teal, 5, "player");
            PrototypeVisuals.Draw(transform, "Shadow", new Vector2(0, -.25f), new Vector2(.65f, .23f), new Color(0, 0, 0, .35f), 3, "orb");
            gun = PrototypeVisuals.Draw(transform, "Pistol", new Vector2(.42f, 0), new Vector2(.42f, .13f), PrototypeVisuals.Gold, 7);
            flash = PrototypeVisuals.Draw(transform, "Muzzle flash", Vector2.zero, Vector2.one * .22f, Color.white, 8, "cross"); flash.enabled = false;
            body = PrototypeVisuals.Body(gameObject, .3f);
            Health = gameObject.AddComponent<Damageable>(); Health.Initialize(Faction.Player, config.health, config.hitProtection);
            Health.Hit += OnHit; Health.Died += OnDeath;
            Health.Evaded += OnEvaded;
        }
        void OnHit() { hitLeft = .18f; game.Notify("combat.hit"); }
        void OnDeath() { body.linearVelocity = Vector2.zero; GetComponent<Collider2D>().enabled = false; view.color = new Color(.35f, .4f, .45f); Flush(); }
        void OnEvaded(DamageContext hit) { if (DodgeProtected) game.Tutorial?.DodgedAttack(); }
        public void RestoreAt(Vector2 at)
        {
            Health.State.SetMaximum(config.health + game.Loadout.ExtraHealth); Health.State.Restore(); Health.Invulnerable = game.Loadout.Invincible;
            GetComponent<Collider2D>().enabled = true; body.position = at; dodgeAge = dodgeLeft = cooldown = shotLeft = bufferLeft = 0; Flush();
        }
        public void ApplyStats()
        {
            Health.State.SetMaximum(config.health + game.Loadout.ExtraHealth);
            Health.Invulnerable = DodgeProtected || game.Loadout.ShieldLeft > 0 || game.Loadout.Invincible;
        }
        public void ResetCooldowns() { dodgeAge = dodgeLeft = cooldown = shotLeft = 0; Health.Invulnerable = game.Loadout.Invincible || game.Loadout.ShieldLeft > 0; }
        public void Flush() { move = Vector2.zero; bufferLeft = 0; if (body != null) body.linearVelocity = Vector2.zero; }
        void Update()
        {
            if (game == null || !game.CanAct || !Health.State.Alive) return;
            float dt = Time.deltaTime; move = game.Input.Move;
            game.Loadout.Tick(dt);
            if (game.Input.PistolPressed) { game.Loadout.Switch(false); game.EquipmentChanged(); }
            if (game.Input.SpecialPressed) { game.Loadout.Switch(true); game.EquipmentChanged(); }
            if (game.Input.ActivePressed)
            {
                string failure = game.Loadout.TryUse(Health.State, IsDodging);
                game.Notify(failure ?? "active.used"); if (failure == null) game.Tutorial?.ActiveUsed();
            }
            Vector2 target = game.GameCamera.ScreenToWorldPoint(game.Input.Pointer);
            if ((target - body.position).sqrMagnitude > .01f) aim = (target - body.position).normalized;
            cooldown = Mathf.Max(0, cooldown - dt); shotLeft = Mathf.Max(0, shotLeft - dt);
            bufferLeft = Mathf.Max(0, bufferLeft - dt); if (game.Input.DodgePressed) bufferLeft = .1f;
            if (bufferLeft > 0 && cooldown <= 0 && !IsDodging)
            {
                bufferLeft = 0; cooldown = config.dodgeCooldown * game.Loadout.DodgeMultiplier; dodgeLeft = config.dodgeDuration; dodgeAge = 0;
                dodgeDirection = move.sqrMagnitude > .01f ? move.normalized : aim;
            }
            if (game.Input.Attack && !IsDodging && shotLeft <= 0 && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())) Fire();
            float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
            gun.transform.localPosition = aim * .4f; gun.transform.rotation = Quaternion.Euler(0, 0, angle);
            flash.transform.localPosition = aim * .68f; flashLeft -= dt; flash.enabled = flashLeft > 0;
            hitLeft -= dt;
            view.color = hitLeft > 0 ? Color.white : IsDodging || game.Loadout.ShieldLeft > 0 ? new Color(.64f, 1, 1) : PrototypeVisuals.Teal;
            view.transform.localScale = new Vector3(.8f, IsDodging ? .62f : .9f + Mathf.Sin(Time.time * 12) * .025f * move.magnitude, 1);
        }
        void Fire()
        {
            var weapon = game.Catalog.Find(game.Loadout.Weapon);
            // Trace from the body, not the muzzle: no shooting through adjacent cover.
            var block = Physics2D.Raycast(body.position, aim, .7f, 1);
            if (block.collider != null) return;
            shotLeft = weapon.interval * game.Loadout.FireRateMultiplier;
            if (!game.Loadout.SpendEnergy(weapon.energyCost)) { game.Notify("weapon.noEnergy"); game.Tutorial?.EnergyFailed(); return; }
            for (int pellet = 0; pellet < weapon.pellets; pellet++)
            {
                float angle = weapon.pellets > 1 ? Mathf.Lerp(-weapon.spread, weapon.spread, (float)pellet / (weapon.pellets - 1)) : 0;
                Vector2 direction = Quaternion.Euler(0, 0, angle) * aim;
                game.Projectiles.Spawn(body.position + aim * .68f, direction, Faction.Player, weapon.damage, config.bulletSpeed, weapon.id);
            }
            flashLeft = .045f;
        }
        void FixedUpdate()
        {
            if (game == null || !game.CanAct || !Health.State.Alive) { if (body != null) body.linearVelocity = Vector2.zero; return; }
            Vector2 velocity = move * config.moveSpeed;
            if (IsDodging)
            {
                Health.Invulnerable = dodgeAge < config.dodgeInvulnerability || game.Loadout.ShieldLeft > 0 || game.Loadout.Invincible;
                float activeStep = Mathf.Min(Time.fixedDeltaTime, dodgeLeft);
                velocity = dodgeDirection * (config.dodgeDistance / config.dodgeDuration) * (activeStep / Time.fixedDeltaTime);
                dodgeLeft = Mathf.Max(0, dodgeLeft - Time.fixedDeltaTime); dodgeAge += Time.fixedDeltaTime;
            }
            else Health.Invulnerable = game.Loadout.ShieldLeft > 0 || game.Loadout.Invincible;
            // Rigidbody continuous collision plus a wall sweep bounds high-speed displacement.
            float distance = velocity.magnitude * Time.fixedDeltaTime;
            if (distance > 0)
            {
                var hit = Physics2D.CircleCast(body.position, .3f, velocity.normalized, distance + .015f, 1);
                if (hit.collider != null)
                {
                    float allowed = Mathf.Max(0, hit.distance - .015f);
                    if (IsDodging || dodgeAge > 0 && dodgeLeft == 0) velocity *= Mathf.Clamp01(allowed / distance);
                    else if (allowed < .01f) velocity -= hit.normal * Mathf.Min(0, Vector2.Dot(velocity, hit.normal));
                }
            }
            if (!IsDodging) dodgeAge = 0;
            body.linearVelocity = velocity;
        }
        void OnDestroy() { if (Health != null) { Health.Hit -= OnHit; Health.Died -= OnDeath; Health.Evaded -= OnEvaded; } }
    }
}
