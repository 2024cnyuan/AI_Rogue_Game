using UnityEngine;

namespace Starfall
{
    public enum GameSound { Shot, Hit, Hurt, Dodge, Pickup, Ui, Clear, Boss }
    public sealed class GameAudio : MonoBehaviour
    {
        StarfallGame game;
        readonly AudioSource[] voices = new AudioSource[8];
        readonly AudioClip[] clips = new AudioClip[8];
        readonly double[] last = new double[8];
        AudioSource music, ui;
        AudioClip ambience;
        int next;
        public void Initialize(StarfallGame owner)
        {
            game = owner;
            for (int i = 0; i < voices.Length; i++) { voices[i] = gameObject.AddComponent<AudioSource>(); voices[i].playOnAwake = false; }
            ui = gameObject.AddComponent<AudioSource>(); ui.playOnAwake = false; ui.ignoreListenerPause = true;
            float[] frequencies = { 720, 290, 120, 540, 990, 800, 660, 180 };
            for (int i = 0; i < clips.Length; i++) clips[i] = Tone("Starfall " + (GameSound)i, frequencies[i], i == 6 ? .45f : .12f, i);
            ambience = Tone("Starfall ambient", 110, 8, -1); music = gameObject.AddComponent<AudioSource>();
            music.clip = ambience; music.loop = true; music.playOnAwake = false; ApplySettings(); music.Play();
        }
        static AudioClip Tone(string name, float frequency, float length, int kind)
        {
            const int rate = 22050; var samples = new float[(int)(rate * length)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / rate, normalized = t / length;
                float envelope = kind < 0 ? Mathf.Sin(Mathf.PI * normalized) * .12f : Mathf.Min(1, t * 80) * Mathf.Pow(1 - normalized, 2) * .22f;
                float wave = Mathf.Sin(2 * Mathf.PI * frequency * t * (kind == 0 ? 1 - normalized * .3f : 1));
                if (kind == 6) wave += Mathf.Sin(2 * Mathf.PI * frequency * 1.25f * t) * .5f;
                if (kind < 0) wave += Mathf.Sin(2 * Mathf.PI * frequency * 1.5f * t) * .25f;
                samples[i] = wave * envelope;
            }
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
        public void Play(GameSound sound)
        {
            int index = (int)sound; double now = Time.realtimeSinceStartupAsDouble;
            if (clips[index] == null || now - last[index] < .04) return; last[index] = now;
            if (sound == GameSound.Ui) { ui.Stop(); ui.clip = clips[index]; ui.volume = game.Settings.masterVolume * game.Settings.effectsVolume; ui.Play(); return; }
            var voice = voices[next++ % voices.Length]; voice.Stop(); voice.clip = clips[index]; voice.volume = game.Settings.masterVolume * game.Settings.effectsVolume; voice.Play();
        }
        public void ApplySettings() { if (music != null) music.volume = game.Settings.masterVolume * game.Settings.musicVolume; }
        public void Pause(bool value) { AudioListener.pause = value; }
        void OnDestroy() { AudioListener.pause = false; foreach (var clip in clips) if (clip != null) Destroy(clip); if (ambience != null) Destroy(ambience); }
    }
}
