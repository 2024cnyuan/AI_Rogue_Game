using UnityEngine;

namespace Starfall
{
    public enum GameSound { Shot, Hit, Hurt, Dodge, Pickup, Ui, Clear, Boss }
    public sealed class GameAudio : MonoBehaviour
    {
        StarfallGame game;
        readonly AudioSource[] voices = new AudioSource[24];
        readonly double[] last = new double[8];
        readonly float[] weights=new float[24]; float uiWeight=1;
        AudioSource music, ui;
        int next;
        public int AudibleEvents { get; private set; }
        public void Initialize(StarfallGame owner)
        {
            game = owner; var c = M5Art.Catalog;
            for (int i = 0; i < voices.Length; i++) { voices[i] = gameObject.AddComponent<AudioSource>(); voices[i].playOnAwake = false; voices[i].outputAudioMixerGroup = c.effectsGroup; }
            ui = gameObject.AddComponent<AudioSource>(); ui.playOnAwake = false; ui.ignoreListenerPause = true; ui.outputAudioMixerGroup = c.uiGroup;
            music = gameObject.AddComponent<AudioSource>(); music.clip = c.ambience; music.loop = true; music.playOnAwake = false; music.outputAudioMixerGroup = c.musicGroup;
            game.GameCamera.gameObject.AddComponent<AudioOutputLimiter>(); ApplySettings(); music.Play();
        }
        public void Play(GameSound sound)
        {
            int index = (int)sound; var c = M5Art.Catalog; var clip = c.sounds[index];
            if (sound == GameSound.Shot && game.Loadout != null) {
                string[] ids = { "pistol", "shotgun", "smg", "crossbow", "launcher", "arc", "workshop_smg" };
                int weapon = System.Array.IndexOf(ids, game.Loadout.Weapon); if (weapon >= 0) clip = c.weapons[weapon];
            }
            double now = Time.realtimeSinceStartupAsDouble;
            if (clip == null || now - last[index] < .035) return; last[index] = now;
            var voice = sound == GameSound.Ui ? ui : voices[next++ % voices.Length]; voice.Stop(); voice.clip = clip;
            voice.pitch = sound == GameSound.Shot ? Random.Range(.97f, 1.03f) : 1;
            float weight=sound==GameSound.Shot?.72f:.9f; if(sound==GameSound.Ui) uiWeight=weight; else weights[(next-1)%voices.Length]=weight;
            voice.volume = game.Settings.masterVolume * game.Settings.effectsVolume * weight; voice.Play(); AudibleEvents++;
        }
        public void TestSound() { ui.clip = M5Art.Catalog.sounds[(int)GameSound.Clear]; ui.pitch = 1; uiWeight=1; ui.volume = game.Settings.masterVolume * game.Settings.effectsVolume; ui.Play(); AudibleEvents++; }
        public void ApplySettings() { if (music != null) music.volume = game.Settings.masterVolume * game.Settings.musicVolume; float level=game.Settings.masterVolume*game.Settings.effectsVolume; for(int i=0;i<voices.Length;i++) if(voices[i]!=null) voices[i].volume=level*weights[i]; if(ui!=null) ui.volume=level*uiWeight; }
        public void Pause(bool value) { AudioListener.pause = value; }
        void OnDestroy() { AudioListener.pause = false; }
    }
}
