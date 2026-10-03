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
        float dodgeLeft, dodgeAge, cooldown, bufferLeft, shotLeft, hitLeft, flashLeft, coldLeft;
        Vector2 surfaceVelocity;
        float charge;
        float recoil;
        string visibleWeapon;
        readonly SpriteRenderer[] ghosts=new SpriteRenderer[4];
        readonly float[] ghostAges=new float[4];
        float ghostClock; int ghostIndex;
        public float ChargeProgress => charge;
        public Damageable Health { get; private set; }
        public bool IsDodging => dodgeLeft > 0;
        public float DodgeCooldown => cooldown;
        public Vector2 Aim => aim;
        public Rigidbody2D Body => body;
        public bool DodgeProtected => IsDodging && dodgeAge < config.dodgeInvulnerability;
        public void Initialize(StarfallGame owner, PrototypeConfig settings)
        {
            game = owner; config = settings;
            view = M5Art.Catalog?.explorerVisual!=null ? Instantiate(M5Art.Catalog.explorerVisual,transform,false).GetComponent<SpriteRenderer>() : PrototypeVisuals.Draw(transform, "Explorer", Vector2.zero, new Vector2(.8f, .9f), PrototypeVisuals.Teal, 5, "player");
            PrototypeVisuals.Draw(transform, "Shadow", new Vector2(0, -.25f), new Vector2(.65f, .23f), new Color(0, 0, 0, .35f), 3, "orb");
            gun = M5Art.Draw(transform,"pistol",Vector2.zero,.25f,7);
            flash = PrototypeVisuals.Draw(transform, "Muzzle flash", Vector2.zero, Vector2.one * .22f, Color.white, 8, "cross"); flash.enabled = false;
            body = PrototypeVisuals.Body(gameObject, .3f);
            Health = gameObject.AddComponent<Damageable>(); Health.Initialize(Faction.Player, config.health, config.hitProtection);
            Health.Game = game;
            Health.Hit += OnHit; Health.Died += OnDeath;
            Health.Evaded += OnEvaded;
            Health.Damaged += OnDamaged;
            if (M5Art.Catalog?.explorer?.Length == 16) { view.sprite = M5Art.Catalog.explorer[0]; view.color = Color.white; view.sharedMaterial = M5Art.Catalog.lit; }
            for(int i=0;i<ghosts.Length;i++) { var go=new GameObject("Dodge silhouette"); go.transform.SetParent(transform.parent,false); ghosts[i]=go.AddComponent<SpriteRenderer>(); ghosts[i].sharedMaterial=M5Art.Catalog?.unlit; go.SetActive(false); }
        }
        void OnDamaged(DamageContext hit, float amount) { game.Adventure?.PlayerDamaged(); game.Training?.PlayerDamaged(); if (hit.Weapon == "frost") coldLeft = 1.2f; }
        void OnHit() { hitLeft = .18f; game.Notify("combat.hit"); game.Audio?.Play(GameSound.Hurt); game.ShakeCamera(.12f); }
        void OnDeath() { body.linearVelocity = Vector2.zero; GetComponent<Collider2D>().enabled = false; view.color = new Color(.35f, .4f, .45f); view.transform.localRotation=Quaternion.Euler(0,0,-72); gun.enabled=flash.enabled=false; Flush(); }
        void OnEvaded(DamageContext hit) { if (DodgeProtected) game.Tutorial?.DodgedAttack(); }
        public void RestoreAt(Vector2 at)
        {
            Health.State.SetMaximum(config.health + game.Loadout.ExtraHealth); Health.State.Restore(); Health.Invulnerable = game.Loadout.Invincible;
            GetComponent<Collider2D>().enabled = true; body.position = at; coldLeft = 0; dodgeAge = dodgeLeft = cooldown = shotLeft = bufferLeft = 0; Flush();
        }
        public void ApplyStats()
        {
            Health.State.SetMaximum(config.health + game.Loadout.ExtraHealth);
            Health.Invulnerable = DodgeProtected || game.Loadout.ShieldLeft > 0 || game.Loadout.Invincible;
        }
        public void MoveTo(Vector2 at)
        {
            body.position = at; dodgeLeft = dodgeAge = bufferLeft = coldLeft = 0; Flush(); Health.Invulnerable = game.Loadout.Invincible || game.Loadout.ShieldLeft > 0;
        }
        public void ResetCooldowns() { dodgeAge = dodgeLeft = cooldown = shotLeft = 0; Health.Invulnerable = game.Loadout.Invincible || game.Loadout.ShieldLeft > 0; }
        public void Flush() { move = surfaceVelocity = Vector2.zero; bufferLeft = charge = 0; if (body != null) body.linearVelocity = Vector2.zero; foreach(var ghost in ghosts) if(ghost!=null) ghost.gameObject.SetActive(false); System.Array.Clear(ghostAges,0,ghostAges.Length); }
        void Update()
        {
            if (game == null || !game.CanAct || !Health.State.Alive) return;
            float dt = Time.deltaTime; move = game.Input.Move;
            coldLeft = Mathf.Max(0, coldLeft - dt);
            game.Loadout.Tick(dt);
            if (game.Input.PistolPressed) { game.Loadout.Switch(false); game.EquipmentChanged(); }
            if (game.Input.SpecialPressed) { game.Loadout.Switch(true); game.EquipmentChanged(); }
            Vector2 target = game.GameCamera.ScreenToWorldPoint(game.Input.Pointer);
            if ((target - body.position).sqrMagnitude > .01f) aim = (target - body.position).normalized;
            if (game.Input.ActivePressed)
            {
                string failure = game.Loadout.TryUse(Health.State, IsDodging, () => game.Effects.TryActivate(game.Loadout.Active, body.position, aim));
                if (failure == null) game.Audio?.Play(GameSound.Pickup);
                game.Notify(failure ?? "active.used"); if (failure == null) game.Tutorial?.ActiveUsed();
            }
            cooldown = Mathf.Max(0, cooldown - dt); shotLeft = Mathf.Max(0, shotLeft - dt);
            bufferLeft = Mathf.Max(0, bufferLeft - dt); if (game.Input.DodgePressed) bufferLeft = .1f;
            if (bufferLeft > 0 && cooldown <= 0 && !IsDodging)
            {
                bufferLeft = 0; cooldown = config.dodgeCooldown * game.Loadout.DodgeMultiplier; dodgeLeft = config.dodgeDuration; dodgeAge = 0;
                dodgeDirection = move.sqrMagnitude > .01f ? move.normalized : aim;
                game.Audio?.Play(GameSound.Dodge);
            }
            bool firing = game.Input.Attack && !IsDodging && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject());
            if (firing) {
                var weapon = game.Catalog.Find(game.Loadout.Weapon);
                if (weapon.id == "crossbow") { charge = Mathf.Min(weapon.delay,charge+dt); if (shotLeft<=0 && charge>=weapon.delay) { Fire(); charge=0; } }
                else { charge=0; if(shotLeft<=0) Fire(); }
            } else charge = 0;
            gun.color = charge > 0 ? Color.Lerp(Color.white, new Color(.65f, 1, 1), charge / game.Catalog.Find("crossbow").delay) : Color.white;
            float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
            recoil = Mathf.MoveTowards(recoil, 0, dt * .65f);
            gun.transform.localPosition = aim * (.23f - recoil) + Vector2.up * .32f; gun.transform.rotation = Quaternion.Euler(0, 0, angle); gun.flipY = aim.x < 0;
            if (visibleWeapon != game.Loadout.Weapon) {
                visibleWeapon = game.Loadout.Weapon; var sprite = M5Art.Get(visibleWeapon);
                if (sprite != null) { gun.sprite = sprite; gun.sharedMaterial = M5Art.Catalog.lit; float width = visibleWeapon == "pistol" ? .6f : 1.05f; gun.transform.localScale = Vector3.one * width / sprite.bounds.size.x; }
            }
            flash.transform.localPosition = MuzzleOffset; flash.sortingOrder = gun.sortingOrder + 1; flashLeft -= dt; flash.enabled = flashLeft > 0 && !game.Settings.reduceFlash;
            hitLeft -= dt;
            view.color = hitLeft > 0 && !game.Settings.reduceFlash ? new Color(1, .58f, .5f) : IsDodging || game.Loadout.ShieldLeft > 0 ? new Color(.64f, 1, 1) : Color.white;
            if (M5Art.Catalog?.explorer?.Length == 16) {
                int direction = Mathf.Abs(aim.y) > Mathf.Abs(aim.x) ? aim.y > 0 ? 1 : 0 : aim.x < 0 ? 2 : 3;
                int frame = move.sqrMagnitude > .01f ? Mathf.FloorToInt(Time.time * 10) % 4 : 0;
                view.sprite = M5Art.Catalog.explorer[direction * 4 + frame];
                view.transform.localScale = Vector3.one * 1.9f / view.sprite.bounds.size.y;
                view.sortingOrder = 40 - Mathf.RoundToInt(body.position.y * 4); gun.sortingOrder = view.sortingOrder + (aim.y > .3f ? -1 : 1);
                view.transform.localRotation = Quaternion.Euler(0, 0, IsDodging ? -12 * Mathf.Sign(aim.x) : 0);
            }
            for(int i=0;i<ghosts.Length;i++) { ghostAges[i]=Mathf.Max(0,ghostAges[i]-dt); ghosts[i].gameObject.SetActive(ghostAges[i]>0 && !game.Settings.minimalEffects); ghosts[i].color=new Color(.55f,.9f,.86f,ghostAges[i]); }
            ghostClock-=dt;
            if(IsDodging && !game.Settings.minimalEffects && ghostClock<=0) { int i=ghostIndex++%ghosts.Length; ghostClock=.05f; ghostAges[i]=.22f; ghosts[i].sprite=view.sprite; ghosts[i].transform.SetPositionAndRotation(view.transform.position,view.transform.rotation); ghosts[i].transform.localScale=view.transform.localScale; ghosts[i].sortingOrder=view.sortingOrder-1; }
        }
        Vector2 MuzzleOffset => aim * (game.Loadout.Weapon == "pistol" ? .53f : .755f) + Vector2.up * .32f;
        void Fire()
        {
            var weapon = game.Catalog.Find(game.Loadout.Weapon);
            // Trace from the body, not the muzzle: no shooting through adjacent cover.
            var block = Physics2D.Raycast(body.position, MuzzleOffset.normalized, MuzzleOffset.magnitude, 1);
            if (block.collider != null) return;
            shotLeft = weapon.interval * game.Loadout.FireRateMultiplier;
            if (!game.Loadout.SpendEnergy(weapon.energyCost)) { game.Notify("weapon.noEnergy"); game.Tutorial?.EnergyFailed(); return; }
            for (int pellet = 0; pellet < weapon.pellets; pellet++)
            {
                float angle = weapon.pellets > 1 ? Mathf.Lerp(-weapon.spread, weapon.spread, (float)pellet / (weapon.pellets - 1)) : 0;
                Vector2 direction = Quaternion.Euler(0, 0, angle) * aim;
                game.Projectiles.Spawn(body.position + MuzzleOffset, direction, Faction.Player, weapon.damage, config.bulletSpeed, weapon.id);
            }
            flashLeft = .065f; recoil = .065f; game.ShakeCamera(weapon.id == "shotgun" || weapon.id == "launcher" ? .1f : .025f);
            game.Audio?.Play(GameSound.Shot);
        }
        void FixedUpdate()
        {
            if (game == null || !game.CanAct || !Health.State.Alive) { if (body != null) body.linearVelocity = Vector2.zero; return; }
            Vector2 desired = move * config.moveSpeed * (coldLeft > 0 ? .8f : 1);
            bool ice = game.Surface != null && game.Surface.OnIce(body.position);
            surfaceVelocity = ice ? Vector2.Lerp(surfaceVelocity, desired, 1 - Mathf.Exp(-Time.fixedDeltaTime / game.CampaignConfig.iceResponse)) : desired;
            Vector2 velocity = surfaceVelocity * Health.SpeedMultiplier + Health.Impulse + (game.Surface != null ? game.Surface.Push(body.position) : Vector2.zero);
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
        void OnDestroy() { if (Health != null) { Health.Hit -= OnHit; Health.Died -= OnDeath; Health.Evaded -= OnEvaded; Health.Damaged -= OnDamaged; } }
    }
}
