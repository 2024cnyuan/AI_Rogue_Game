using UnityEngine;

namespace Starfall
{
    public sealed class ThemeBoss : MonoBehaviour
    {
        StarfallGame game;
        SpriteRenderer view;
        readonly SpriteRenderer[] marks = new SpriteRenderer[3];
        readonly Vector2[] targets = new Vector2[3];
        int stage, sequence;
        float remaining, attackClock;
        Vector2 aim, reflected, bounce;
        bool executing, recovering, stopped;
        public Damageable Health { get; private set; }
        public int Move { get; private set; }
        public bool Warning => !stopped && !executing && !recovering;
        public string MoveKey => "boss.s" + stage + "." + Move;
        public void Initialize(StarfallGame owner, int theme)
        {
            game = owner; stage = theme; PrototypeVisuals.Body(gameObject, .62f).bodyType = RigidbodyType2D.Kinematic;
            view = PrototypeVisuals.Draw(transform, "Theme guardian", Vector2.zero, new Vector2(1.6f, 1.6f), stage == 2 ? new Color(.85f, .42f, .16f) : new Color(.4f, .72f, .9f), 5, stage == 2 ? "shooter" : "cross");
            for (int i = 0; i < marks.Length; i++) marks[i] = PrototypeVisuals.Draw(transform, "Guardian telegraph", Vector2.zero, Vector2.one, PrototypeVisuals.Gold, 3, "orb");
            Health = gameObject.AddComponent<Damageable>(); Health.Initialize(Faction.Enemy, stage == 2 ? game.CampaignConfig.furnaceHealth : game.CampaignConfig.mirrorHealth, 0);
            Health.Hit += Hit; Health.Died += Die; Begin();
        }
        void Hit() => game.Audio?.Play(GameSound.Hit);
        void Hide() { foreach (var mark in marks) mark.enabled = false; }
        void Die() { Stop(); GetComponent<Collider2D>().enabled = false; view.color = new Color(.2f, .3f, .35f); game.Projectiles.Clear(); }
        void Line(SpriteRenderer mark, Vector2 from, Vector2 to)
        {
            mark.sprite = PrototypeVisuals.Sprite("square"); mark.transform.position = (from + to) / 2;
            mark.transform.localScale = new Vector3(Vector2.Distance(from, to), .22f, 1);
            mark.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg); mark.enabled = true;
        }
        void Begin()
        {
            Move = sequence++ % 3; remaining = game.CampaignConfig.warning; executing = recovering = false; Hide();
            Vector2 from = transform.position; aim = (game.Player.Body.position - from).normalized;
            for (int i = 0; i < 3; i++)
            {
                targets[i] = game.Player.Body.position + new Vector2((i - 1) * 2, i == 1 ? 1.5f : -.5f);
                targets[i] = new Vector2(Mathf.Clamp(targets[i].x, -8, 8), Mathf.Clamp(targets[i].y, -5, 5));
                marks[i].transform.rotation = Quaternion.identity; marks[i].color = PrototypeVisuals.Gold;
            }
            if (stage == 2 && Move == 0)
            {
                for (int i = 0; i < 3; i++) { marks[i].sprite = PrototypeVisuals.Sprite("orb"); marks[i].transform.position = targets[i]; marks[i].transform.localScale = new Vector3(2.4f, 2.4f, 1); marks[i].enabled = true; }
            }
            else if (stage == 3 && Move == 0)
            {
                var hit = Physics2D.Raycast(from, aim, 30, 1); bounce = hit.collider != null ? hit.point : from + aim * 15;
                reflected = hit.collider != null ? Vector2.Reflect(aim, hit.normal) : -aim;
                var second = Physics2D.Raycast(bounce + reflected * .05f, reflected, 20, 1);
                targets[0] = second.collider != null ? second.point : bounce + reflected * 15;
                Line(marks[0], from, bounce); Line(marks[1], bounce, targets[0]);
            }
            else if (Move == 1 && stage == 2)
            {
                for (int i = 0; i < 3; i++) Line(marks[i], from, from + (Vector2)(Quaternion.Euler(0, 0, (i - 1) * 65) * aim) * 9);
            }
            else { marks[0].sprite = PrototypeVisuals.Sprite("orb"); marks[0].transform.position = from; marks[0].transform.localScale = new Vector3(3, 3, 1); marks[0].enabled = true; }
            game.Audio?.Play(GameSound.Boss);
        }
        static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b) => Vector2.Distance(p, a + (b - a) * Mathf.Clamp01(Vector2.Dot(p - a, b - a) / Mathf.Max(.001f, (b - a).sqrMagnitude)));
        void Update()
        {
            if (stopped || game == null || !game.CanAct) return;
            remaining -= Time.deltaTime;
            if (Warning && remaining <= 0)
            {
                executing = true; attackClock = 0; remaining = stage == 2 && Move == 1 ? 1.5f : .6f;
                if (stage == 2 && Move == 0 || stage == 3 && Move == 0) foreach (var mark in marks) mark.color = PrototypeVisuals.Enemy;
                else Hide();
                if (stage == 2 && Move == 2 || stage == 3 && Move == 1)
                {
                    for (int i = 0; i < 16; i++) Shoot(Quaternion.Euler(0, 0, i * 22.5f) * Vector2.right, 3.6f);
                }
            }
            if (executing)
            {
                Vector2 player = game.Player.Body.position, from = transform.position;
                if (stage == 2 && Move == 0) { foreach (var point in targets) if (Vector2.Distance(point, player) < 1.3f) Damage(); }
                else if (stage == 3 && Move == 0) { if (SegmentDistance(player, from, bounce) < .45f || SegmentDistance(player, bounce, targets[0]) < .45f) Damage(); }
                else if (stage == 2 && Move == 1 || stage == 3 && Move == 2)
                {
                    attackClock -= Time.deltaTime;
                    if (attackClock <= 0)
                    {
                        attackClock = .2f;
                        float angle = stage == 2 ? Mathf.Lerp(-65, 65, 1 - remaining / 1.5f) : 0;
                        Shoot(Quaternion.Euler(0, 0, angle) * aim, stage == 2 ? 4 : 5);
                        if (stage == 3) { Shoot(Quaternion.Euler(0, 0, 20) * aim, 5); Shoot(Quaternion.Euler(0, 0, -20) * aim, 5); }
                    }
                }
                if (remaining <= 0) { executing = false; recovering = true; remaining = game.CampaignConfig.recovery; Hide(); }
            }
            else if (recovering && remaining <= 0) Begin();
        }
        void Damage() => game.Player.Health.Receive(new DamageContext(game.CampaignConfig.bossDamage, Faction.Enemy));
        void Shoot(Vector2 direction, float speed) => game.Projectiles.Spawn((Vector2)transform.position + direction * .85f, direction, Faction.Enemy, game.CampaignConfig.bossDamage, speed, stage == 3 ? "frost" : "pistol");
        public void Stop() { stopped = true; Hide(); }
        void OnDestroy() { if (Health != null) { Health.Hit -= Hit; Health.Died -= Die; } }
    }
}
