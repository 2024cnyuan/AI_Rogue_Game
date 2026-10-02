using System.Collections.Generic;
using UnityEngine;

namespace Starfall
{
    public sealed class ProjectilePool : MonoBehaviour
    {
        sealed class Shot
        {
            public SpriteRenderer View;
            public Vector2 Position, Direction;
            public float Speed, Damage, Life;
            public Faction Faction;
            public string Weapon;
            public bool Active;
        }
        readonly List<Shot> shots = new List<Shot>(180);
        readonly RaycastHit2D[] hits = new RaycastHit2D[24];
        readonly ContactFilter2D filter = new ContactFilter2D { useLayerMask = true, layerMask = (1 << 0) | (1 << 2), useTriggers = false };
        public StarfallGame Game { get; set; }
        public int ActiveCount { get; private set; }
        void Awake() { for (int i = 0; i < 48; i++) Create(); }
        Shot Create()
        {
            var shot = new Shot { View = PrototypeVisuals.Draw(transform, "Pooled projectile", Vector2.zero, Vector2.one, Color.white, 10) };
            shot.View.gameObject.SetActive(false); shots.Add(shot); return shot;
        }
        public bool Spawn(Vector2 origin, Vector2 direction, Faction faction, float damage, float speed, string weapon = "pistol")
        {
            if (Game == null || !Game.CanAct || direction.sqrMagnitude < .01f) return false;
            Shot shot = null;
            foreach (var candidate in shots) if (!candidate.Active) { shot = candidate; break; }
            if (shot == null) { if (shots.Count >= 256) return false; shot = Create(); }
            shot.Active = true; shot.Position = origin; shot.Direction = direction.normalized;
            shot.Speed = speed; shot.Damage = damage; shot.Faction = faction; shot.Life = 3;
            shot.Weapon = weapon;
            shot.View.sprite = PrototypeVisuals.Sprite(faction == Faction.Player ? "square" : "orb");
            shot.View.transform.localScale = faction == Faction.Player ? new Vector3(.3f, .1f, 1) : new Vector3(.27f, .27f, 1);
            shot.View.transform.position = origin; shot.View.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            shot.View.color = faction == Faction.Player ? PrototypeVisuals.Gold : weapon == "frost" ? new Color(.55f, .8f, 1) : PrototypeVisuals.Enemy;
            if (faction == Faction.Enemy && weapon == "frost") shot.View.sprite = PrototypeVisuals.Sprite("cross");
            shot.View.gameObject.SetActive(true); ActiveCount++; return true;
        }
        void FixedUpdate()
        {
            if (Game == null || !Game.CanAct) return;
            foreach (var shot in shots)
            {
                if (!shot.Active) continue;
                float step = shot.Speed * Time.fixedDeltaTime;
                int count = Physics2D.CircleCast(shot.Position, .08f, shot.Direction, filter, hits, step);
                RaycastHit2D nearest = default; float nearestDistance = float.MaxValue;
                for (int i = 0; i < count; i++)
                {
                    var hit = hits[i]; var target = hit.collider.GetComponent<Damageable>();
                    if (target != null && (target.Faction == shot.Faction || !target.State.Alive)) continue;
                    if (hit.distance < nearestDistance) { nearest = hit; nearestDistance = hit.distance; }
                }
                if (nearest.collider != null)
                {
                    var target = nearest.collider.GetComponent<Damageable>();
                    if (target != null) target.Receive(new DamageContext(shot.Damage, shot.Faction, shot.Weapon));
                    Return(shot); continue;
                }
                shot.Position += shot.Direction * step; shot.View.transform.position = shot.Position;
                shot.Life -= Time.fixedDeltaTime; if (shot.Life <= 0) Return(shot);
            }
        }
        void Return(Shot shot)
        {
            if (!shot.Active) return;
            shot.Active = false; shot.Life = shot.Damage = shot.Speed = 0; shot.Position = shot.Direction = Vector2.zero;
            shot.Weapon = null;
            shot.View.gameObject.SetActive(false); ActiveCount--;
        }
        public void Clear() { foreach (var shot in shots) Return(shot); }
    }
}
