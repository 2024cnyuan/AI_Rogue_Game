using UnityEngine;

namespace Starfall
{
    // Bounded, reusable areas. All clocks use effective game time and are cleared at room boundaries.
    public sealed class CombatEffects : MonoBehaviour
    {
        sealed class Area { public SpriteRenderer View; public Vector2 At; public float Left, Radius, Damage, Duration, Slow; public bool Active, Triggered, Hostile; public string Source; }
        readonly Area[] areas = new Area[24];
        readonly Collider2D[] contacts = new Collider2D[64];
        readonly Damageable[] visited = new Damageable[16];
        StarfallGame game;
        Damageable decoy;
        float decoyLeft;
        public int ActiveCount { get { int count = Decoy != null ? 1 : 0; foreach (var area in areas) if (area != null && area.Active) count++; return count; } }
        public Damageable Decoy => decoy != null && decoy.gameObject.activeSelf && decoy.State.Alive ? decoy : null;
        public void Initialize(StarfallGame owner)
        {
            game = owner;
            for (int i = 0; i < areas.Length; i++) {
                areas[i] = new Area { View = PrototypeVisuals.Draw(transform, "Pooled area", Vector2.zero, Vector2.one, PrototypeVisuals.Gold, -6, "orb") };
                areas[i].View.gameObject.SetActive(false);
            }
            var go = new GameObject("Decoy beacon"); go.transform.SetParent(transform, false);
            PrototypeVisuals.Body(go, .35f).bodyType = RigidbodyType2D.Kinematic;
            M5Art.Draw(go.transform,"decoy",Vector2.zero,1.2f,45); M5Art.Shadow(go.transform,.8f);
            decoy = go.AddComponent<Damageable>(); decoy.Initialize(Faction.Player, 60, .2f); decoy.Game = game; go.SetActive(false);
        }
        public bool TryActivate(string id, Vector2 origin, Vector2 aim)
        {
            var item = game.Catalog.Find(id);
            if (id == "decoy") {
                Vector2 at = Landing(origin, aim, 2);
                if (!game.Room.IsClear(at, .4f)) return false;
                decoy.Initialize(Faction.Player, item.value, .2f); decoy.ClearStatus(); decoy.transform.position = at; decoy.gameObject.SetActive(true); decoyLeft = item.duration; Physics2D.SyncTransforms(); return true;
            }
            if (id == "shock") {
                if (!Add(origin, item.radius, .2f, 0, 0, 1, id)) return false;
                HitArea(origin, item.radius, item.value, id, item.duration, 1, true); return true;
            }
            if (id == "slow") return Add(Landing(origin, aim, 1.8f), item.radius, item.duration, 0, item.duration, 1 - item.value / 100, id);
            if (id == "grenade") return Add(Landing(origin, aim, 3), item.radius * game.Loadout.BlastMultiplier, item.delay, item.value, 0, 1, id);
            return false;
        }
        Vector2 Landing(Vector2 origin, Vector2 aim, float distance) {
            var wall = Physics2D.CircleCast(origin, .25f, aim, distance, 1);
            return origin + aim * (wall.collider != null ? Mathf.Max(0, wall.distance - .1f) : distance);
        }
        bool Add(Vector2 at, float radius, float left, float damage, float duration, float slow, string source, bool hostile = false)
        {
            foreach (var area in areas) if (!area.Active) {
                area.Active = true; area.Triggered = false; area.At = at; area.Radius = radius; area.Left = left; area.Damage = damage;
                area.Duration = duration; area.Slow = slow; area.Source = source; area.Hostile = hostile;
                area.View.transform.position = at; area.View.transform.localScale = new Vector3(radius * 2, radius * 2, 1);
                area.View.color = hostile ? new Color(.8f, .35f, .65f, .32f) : new Color(.3f, .85f, .8f, .3f); area.View.gameObject.SetActive(true); return true;
            }
            return false;
        }
        public bool Explosion(Vector2 at, float damage, float radius, float delay, string source) => Add(at, radius, delay, damage, 0, 1, source);
        public bool HostileArea(Vector2 at, float radius, float warning, float duration, string source) => Add(at, radius, warning, 8, duration, 1, source, true);
        void Update()
        {
            if (game == null || !game.CanAct) return;
            if (Decoy != null) { decoyLeft -= Time.deltaTime; if (decoyLeft <= 0) decoy.gameObject.SetActive(false); }
            else if (decoy.gameObject.activeSelf) decoy.gameObject.SetActive(false);
            foreach (var area in areas) if (area.Active) {
                area.Left -= Time.deltaTime;
                if (area.Duration > 0 && !area.Hostile) HitArea(area.At, area.Radius, 0, area.Source, .12f, area.Slow, false);
                if (area.Hostile && area.Triggered) HitArea(area.At, area.Radius, area.Damage, area.Source, 0, 1, false, true);
                if (area.Left > 0) continue;
                if (area.Hostile && !area.Triggered) { area.Triggered = true; area.Left = area.Duration; area.View.color = new Color(.9f, .2f, .45f, .5f); }
                else { if (area.Damage > 0 && !area.Hostile) HitArea(area.At, area.Radius, area.Damage, area.Source, 0, 1, false); Return(area); }
            }
        }
        bool Sight(Vector2 from, Damageable target) {
            Vector2 delta = (Vector2)target.transform.position - from;
            var wall = Physics2D.Raycast(from, delta.normalized, delta.magnitude, 1);
            return wall.collider == null || wall.collider.GetComponent<Damageable>() == target;
        }
        void HitArea(Vector2 at, float radius, float damage, string source, float control, float slow, bool knock, bool hostile = false)
        {
            int count = Physics2D.OverlapCircle(at, radius, new ContactFilter2D { useLayerMask = true, layerMask = 5, useTriggers = false }, contacts);
            for (int i = 0; i < count; i++) {
                var target = contacts[i].GetComponent<Damageable>();
                if (target == null || !target.State.Alive || target.Faction != (hostile ? Faction.Player : Faction.Enemy) || !Sight(at, target)) continue;
                if (control > 0) target.Control(control, slow);
                if (damage > 0) target.Receive(new DamageContext(damage, hostile ? Faction.Enemy : Faction.Player, source, 1,
                    knock ? ((Vector2)target.transform.position - at).normalized * 5 : Vector2.zero));
            }
        }
        public int Chain(Damageable first, float damage, int links, float radius)
        {
            int total = 0; visited[0] = first; Vector2 at = first.transform.position;
            for (int hop = 0; hop < Mathf.Min(links, 6); hop++) {
                int count = Physics2D.OverlapCircle(at, radius, new ContactFilter2D { useLayerMask = true, layerMask = 5, useTriggers = false }, contacts);
                Damageable nearest = null; float best = float.MaxValue;
                for (int i = 0; i < count; i++) {
                    var target = contacts[i].GetComponent<Damageable>(); if (target == null || target.Faction != Faction.Enemy || !target.State.Alive || !Sight(at, target)) continue;
                    bool seen = false; for (int j = 0; j <= total; j++) if (visited[j] == target) seen = true;
                    float distance = ((Vector2)target.transform.position - at).sqrMagnitude;
                    if (!seen && distance < best) { nearest = target; best = distance; }
                }
                if (nearest == null) break;
                Explosion((at + (Vector2)nearest.transform.position) / 2, 0, .2f, .12f, "arc.visual");
                visited[++total] = nearest; nearest.Receive(new DamageContext(damage, Faction.Player, "arc.chain", 1)); at = nearest.transform.position;
            }
            for (int i = 0; i < visited.Length; i++) visited[i] = null; return total;
        }
        void Return(Area area) { area.Active = area.Triggered = false; area.Source = null; area.Left = area.Damage = area.Duration = 0; area.View.gameObject.SetActive(false); }
        public void ClearActive() { foreach (var area in areas) if (area.Active && !area.Hostile) Return(area); if (decoy != null) { decoy.gameObject.SetActive(false); decoy.ClearStatus(); } game?.ClearCombatStatus(); }
        public void Clear() { foreach (var area in areas) Return(area); ClearActive(); }
        public void ClearHostile() { foreach (var area in areas) if (area.Hostile) Return(area); }
    }
}
