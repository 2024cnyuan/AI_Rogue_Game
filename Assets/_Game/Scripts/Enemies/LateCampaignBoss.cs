using UnityEngine;
namespace Starfall
{
    public sealed class LateCampaignBoss : MonoBehaviour
    {
        StarfallGame game;
        SpriteRenderer view;
        readonly SpriteRenderer[] marks = new SpriteRenderer[3];
        readonly Rect[] blocks = new Rect[2];
        readonly BoxCollider2D[] colliders = new BoxCollider2D[2];
        int stage, sequence;
        float left, burstClock;
        bool executing, recovering, transitioning, stopped, hasBlocks;
        Vector2 dashEnd, aim;
        public Damageable Health { get; private set; }
        public int Move { get; private set; }
        public int Phase { get; private set; } = 1;
        public bool Warning => !stopped && !executing && !recovering;
        public bool Transitioning => transitioning;
        public string MoveKey => transitioning ? "boss.phase" : "boss.s" + stage + "." + Move;
        public void Initialize(StarfallGame owner, int theme) {
            game = owner; stage = theme;
            PrototypeVisuals.Body(gameObject, .65f).bodyType = RigidbodyType2D.Kinematic;
            view = PrototypeVisuals.Draw(transform, "Campaign guardian", Vector2.zero, Vector2.one * 1.7f, stage == 4 ? new Color(.6f,.4f,.8f) : stage == 5 ? PrototypeVisuals.Gold : new Color(.9f,.86f,.72f), 5, stage == 4 ? "orb" : "shooter");
            M5Art.Apply(view, "boss." + stage, 3.2f); M5Art.Shadow(transform, 1.6f);
            for (int i = 0; i < 3; i++) marks[i] = PrototypeVisuals.Draw(transform, "Guardian warning", Vector2.zero, Vector2.one, PrototypeVisuals.Gold, 3);
            for (int i = 0; i < 2; i++) { colliders[i] = marks[i].gameObject.AddComponent<BoxCollider2D>(); colliders[i].enabled = false; }
            Health = gameObject.AddComponent<Damageable>(); Health.Initialize(Faction.Enemy, stage == 4 ? game.CampaignConfig.sporeHealth : stage == 5 ? game.CampaignConfig.railHealth : game.CampaignConfig.starcoreHealth, 0); Health.Game = game;
            Health.Died += Die; Begin();
        }
        void Hide() { foreach (var mark in marks) mark.enabled = false; }
        void RemoveBlocks() { if (!hasBlocks) return; for (int i = 0; i < 2; i++) { colliders[i].enabled = false; game.Room.RemoveCover(blocks[i]); } hasBlocks = false; }
        void Rectangle(int index, Rect rect) {
            marks[index].sprite = PrototypeVisuals.Sprite("square"); marks[index].transform.position = rect.center; marks[index].transform.rotation = Quaternion.identity;
            marks[index].transform.localScale = new Vector3(rect.width, rect.height, 1); marks[index].enabled = true;
        }
        void Begin() {
            RemoveBlocks(); Hide(); Move = sequence++ % 3; executing = recovering = false; left = stage == 5 && Move == 0 ? .9f : game.CampaignConfig.warning;
            aim = (game.Player.Body.position - (Vector2)transform.position).normalized;
            foreach (var mark in marks) mark.color = PrototypeVisuals.Gold;
            if (stage == 4 && Move == 0 || stage == 6 && Move == 1) {
                blocks[0] = FirstLevelPlan.R(-2, 1.2f, .65f, 3.2f); blocks[1] = FirstLevelPlan.R(2, -1.2f, .65f, 3.2f);
                for (int i = 0; i < 2; i++) Rectangle(i, blocks[i]);
            }
            else if (stage == 5 && Move == 0 || stage == 6 && Move == 2 && Phase == 3) {
                dashEnd = new Vector2(Mathf.Clamp(game.Player.Body.position.x,-8,8), Mathf.Clamp(game.Player.Body.position.y,-5,5));
                Vector2 from = transform.position, delta = dashEnd - from;
                marks[0].sprite = PrototypeVisuals.Sprite("square"); marks[0].transform.position = (from + dashEnd)/2;
                marks[0].transform.localScale = new Vector3(delta.magnitude, 1.3f, 1); marks[0].transform.rotation = Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg); marks[0].enabled = true;
            }
            else if (stage == 5 && Move == 1) { Rectangle(0, FirstLevelPlan.R(0,2.5f,15,1)); Rectangle(1, FirstLevelPlan.R(0,-2.5f,15,1)); }
            else { marks[2].sprite = PrototypeVisuals.Sprite("orb"); marks[2].transform.position = transform.position; marks[2].transform.localScale = Vector3.one * 3; marks[2].enabled = true; }
        }
        void Execute() {
            executing = true; left = 1; burstClock = 0;
            foreach (var mark in marks) mark.color = new Color(.8f,.3f,.55f,.8f);
            if (stage == 4 && Move == 0 || stage == 6 && Move == 1) {
                // Two short sectors leave both outer lanes and the center crossing accessible.
                hasBlocks = true; for (int i = 0; i < 2; i++) { colliders[i].enabled = true; game.Room.AddCover(blocks[i]); } left = 1.6f;
            }
            else if (stage == 4 && Move == 1) {
                int allowance = Mathf.Max(0, game.CampaignConfig.summonLimit - game.LivingEnemies);
                foreach (var at in new[] { new Vector2(-5,3), new Vector2(3,4) }) if (allowance-- > 0) game.SpawnEnemy(at, false, false, 1, EnemyStyle.Sporelet, false);
                Hide();
            }
            else if (stage == 4 && Move == 2 || stage == 6 && Move == 0) { Ring(Phase == 3 ? 16 : 12); Hide(); }
        }
        void Ring(int count) { for (int i = 0; i < count; i++) Shoot(Quaternion.Euler(0,0,i*360f/count + sequence*9) * Vector2.right, 3.8f); }
        void Shoot(Vector2 direction, float speed) => game.Projectiles.Spawn((Vector2)transform.position + direction * .85f, direction, Faction.Enemy, game.CampaignConfig.bossDamage, speed, "guardian");
        void Update() {
            if (stopped || game == null || !game.CanAct) return;
            int desired = stage == 6 ? Health.State.Health / Health.State.Maximum <= .34f ? 3 : Health.State.Health / Health.State.Maximum <= .67f ? 2 : 1 : 1;
            if (!transitioning && desired > Phase) {
                Phase++; transitioning = true; Health.Invulnerable = true; executing = recovering = false; left = 1;
                RemoveBlocks(); Hide(); game.Projectiles.Clear(); game.Effects.ClearHostile(); game.ClearEnemies();
                marks[2].sprite = PrototypeVisuals.Sprite("orb"); marks[2].transform.position = transform.position; marks[2].transform.localScale = Vector3.one * 4;
                marks[2].color = PrototypeVisuals.Gold; marks[2].enabled = true; return;
            }
            left -= Time.deltaTime;
            if (transitioning) { if (left <= 0) { transitioning = false; Health.Invulnerable = false; Begin(); } return; }
            if (Warning && left <= 0) Execute();
            if (executing) {
                Vector2 from = transform.position, player = game.Player.Body.position;
                if (stage == 5 && Move == 0 || stage == 6 && Move == 2 && Phase == 3) {
                    Vector2 step = Vector2.MoveTowards(from,dashEnd,14 * Time.deltaTime) - from;
                    var wall = Physics2D.CircleCast(from,.65f,step.normalized,step.magnitude,1); if (wall.collider == null) transform.position = from + step;
                    if (Vector2.Distance(transform.position,player) < 1) game.Player.Health.Receive(new DamageContext(10,Faction.Enemy,"rail.dash"));
                }
                else if (stage == 5 && Move == 1) { if (Mathf.Abs(player.x) < 7.5f && Mathf.Abs(Mathf.Abs(player.y) - 2.5f) < .65f) game.Player.Health.Receive(new DamageContext(8,Faction.Enemy,"rail.field")); }
                else if (stage == 5 && Move == 2 || stage == 6 && (Move == 1 && Phase > 1 || Move == 2 && Phase < 3)) {
                    burstClock -= Time.deltaTime; if (burstClock <= 0) { burstClock = .3f; Shoot(aim,4.5f); Shoot(Quaternion.Euler(0,0,25)*aim,4.5f); Shoot(Quaternion.Euler(0,0,-25)*aim,4.5f); }
                }
                if (left <= 0) { executing = false; recovering = true; left = game.CampaignConfig.recovery; RemoveBlocks(); Hide(); }
            } else if (recovering && left <= 0) Begin();
        }
        void Die() { Stop(); GetComponent<Collider2D>().enabled = false; view.color = new Color(.3f,.3f,.4f); game.Projectiles.Clear(); game.Effects.ClearHostile(); game.ClearEnemies(); }
        public void Stop() { stopped = true; Health.Invulnerable = false; RemoveBlocks(); Hide(); }
        void OnDestroy() { if (Health != null) Health.Died -= Die; }
    }
}
