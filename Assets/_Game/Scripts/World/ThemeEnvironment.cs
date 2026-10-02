using UnityEngine;
namespace Starfall
{
    public sealed class ThemeEnvironment : MonoBehaviour
    {
        StarfallGame game;
        int stage;
        bool mechanism, practice;
        float clock;
        readonly SpriteRenderer[] danger = new SpriteRenderer[2];
        public bool Running { get; private set; } = true;
        public bool HasPulse => Running && danger[0] != null && !(mechanism && !practice && game.Adventure.Progress.IsClear("seal"));
        public bool Warning => Running && stage >= 4 && clock % 6 >= 4 && clock % 6 < 5.2f;
        public bool Electrified => Running && stage >= 4 && clock % 6 >= 5.2f;
        public float Countdown => stage >= 4 ? Mathf.Max(0, 5.2f - clock % 6) : Mathf.Max(0, .9f - clock % 5);
        public string StateKey => Electrified ? "environment.live" : Warning ? "environment.warning" : "environment.safe";
        public bool OnIce(Vector2 at) => Running && (stage == 3 || stage == 6) && new Rect(-6,-3,12,6).Contains(at);
        public Vector2 Push(Vector2 at) => Running && stage == 2 && new Rect(-6,-.85f,12,1.7f).Contains(at) ? Vector2.right * game.CampaignConfig.conveyorSpeed : Vector2.zero;
        public void Initialize(StarfallGame owner, int theme, bool relay = false, bool sample = false) {
            game = owner; stage = theme; mechanism = relay; practice = sample;
            if (stage == 2 || stage == 3 || stage == 6) {
                PrototypeVisuals.Draw(transform,"Surface",Vector2.zero,stage == 2 ? new Vector2(12,1.7f) : new Vector2(12,6), stage == 2 ? new Color(.38f,.23f,.12f) : new Color(.27f,.48f,.62f),-12);
                for (int i = 0; i < 6; i++) PrototypeVisuals.Draw(transform,"Surface marker",new Vector2(-5+i*2,0),Vector2.one*.5f,PrototypeVisuals.Gold,-11,stage == 2 ? "arrow" : "cross");
            }
            if (stage == 4) {
                foreach (var at in new[] {new Vector2(-3,0), new Vector2(3,0), new Vector2(-8,-4)}) PrototypeVisuals.Draw(transform,"Safe island",at,new Vector2(2.8f,2.8f),new Color(.3f,.55f,.3f),-9,"orb");
            }
            if (relay || stage == 4 || stage == 5) {
                for (int i = 0; i < danger.Length; i++) {
                    Vector2 at = stage == 4 ? new Vector2(0,i == 0 ? 1.15f : -1.15f) : stage >= 5 ? new Vector2(0,i == 0 ? 2.5f : -2.5f) : new Vector2(0,stage == 2 ? 2 : -4);
                    danger[i] = PrototypeVisuals.Draw(transform,"Timed hazard",at,stage == 4 ? new Vector2(12,2.3f) : new Vector2(16,stage >= 5 ? 1.2f : 1.1f),PrototypeVisuals.Gold,-10); danger[i].enabled = false;
                    if (stage < 4) break;
                }
            }
        }
        public bool Dangerous(Vector2 at) {
            if (stage == 4) return Mathf.Abs(at.x) < 6 && Mathf.Abs(at.y) < 2.3f && Vector2.Distance(at,new Vector2(-3,0)) > 1.4f && Vector2.Distance(at,new Vector2(3,0)) > 1.4f;
            if (stage >= 5) return Mathf.Abs(at.x) < 8 && Mathf.Abs(Mathf.Abs(at.y) - 2.5f) < .6f;
            return Mathf.Abs(at.x) < 8 && Mathf.Abs(at.y - (stage == 2 ? 2 : -4)) < .55f;
        }
        public void Stop() { Running = false; foreach (var mark in danger) if (mark != null) mark.enabled = false; }
        void Update() {
            if (!Running || game == null || !game.CanAct) return;
            clock += Time.deltaTime;
            bool activeRule = mechanism || stage == 4 || stage == 5;
            if (mechanism && !practice && game.Adventure.Progress.IsClear("seal")) activeRule = false;
            if (!activeRule) { foreach (var mark in danger) if (mark != null) mark.enabled = false; return; }
            bool warning = stage >= 4 ? Warning : clock % 5 < .9f;
            bool live = stage >= 4 ? Electrified : clock % 5 >= .9f && clock % 5 < 1.5f;
            foreach (var mark in danger) if (mark != null) { mark.enabled = warning || live; mark.color = live ? new Color(.88f,.25f,.5f,.65f) : new Color(.9f,.8f,.3f,.6f); }
            if (live && Dangerous(game.Player.Body.position)) game.Player.Health.Receive(new DamageContext(6,Faction.Enemy,stage == 4 ? "poison" : "grid"));
        }
    }
}
