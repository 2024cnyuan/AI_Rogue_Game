using UnityEngine;

namespace Starfall
{
    public sealed class ThemeEnvironment : MonoBehaviour
    {
        StarfallGame game;
        int stage;
        bool mechanism, practice;
        float clock;
        SpriteRenderer danger;
        public bool Running { get; private set; } = true;
        public bool OnIce(Vector2 at) => Running && stage == 3 && new Rect(-6, -3, 12, 6).Contains(at);
        public Vector2 Push(Vector2 at) => Running && stage == 2 && new Rect(-6, -.85f, 12, 1.7f).Contains(at) ? Vector2.right * game.CampaignConfig.conveyorSpeed : Vector2.zero;
        public void Initialize(StarfallGame owner, int theme, bool relay = false, bool sample = false)
        {
            game = owner; stage = theme; mechanism = relay; practice = sample;
            PrototypeVisuals.Draw(transform, theme == 2 ? "Conveyor" : "Ice", Vector2.zero, theme == 2 ? new Vector2(12, 1.7f) : new Vector2(12, 6),
                theme == 2 ? new Color(.38f, .23f, .12f) : new Color(.27f, .48f, .62f), -12);
            for (int i = 0; i < 6; i++) PrototypeVisuals.Draw(transform, "Surface marker", new Vector2(-5 + i * 2, 0), new Vector2(.5f, .5f),
                theme == 2 ? PrototypeVisuals.Gold : new Color(.65f, .85f, .95f), -11, theme == 2 ? "arrow" : "cross");
            if (relay)
            {
                danger = PrototypeVisuals.Draw(transform, "Timed hazard", new Vector2(0, theme == 2 ? 2 : -4), new Vector2(16, .8f), PrototypeVisuals.Gold, -10); danger.enabled = false;
            }
        }
        public void Stop() { Running = false; if (danger != null) danger.enabled = false; }
        void Update()
        {
            if (!Running || game == null || !game.CanAct) return;
            clock += Time.deltaTime;
            if (!mechanism || !practice && game.Adventure.Progress.IsClear("seal")) { if (danger != null) danger.enabled = false; return; }
            float phase = clock % 5;
            danger.enabled = phase < 1.5f; danger.color = phase < .9f ? PrototypeVisuals.Gold : PrototypeVisuals.Enemy;
            if (phase >= .9f && phase < 1.5f && Mathf.Abs(game.Player.Body.position.y - (stage == 2 ? 2 : -4)) < .55f && Mathf.Abs(game.Player.Body.position.x) < 8)
                game.Player.Health.Receive(new DamageContext(6, Faction.Enemy));
        }
    }
}
