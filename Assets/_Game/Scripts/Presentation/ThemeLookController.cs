using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Starfall
{
    public sealed class ThemeLookController : MonoBehaviour
    {
        StarfallGame game;
        Light2D global;
        readonly Light2D[] focal = new Light2D[3];
        Volume volume;
        VolumeProfile standard, minimal;
        static readonly Color[] Hues = { new Color(.76f, .94f, .86f), new Color(1, .83f, .69f), new Color(.73f, .88f, 1), new Color(.91f, .77f, .98f), new Color(.77f, .87f, 1), new Color(.95f, .94f, .86f) };
        public void Initialize(StarfallGame owner)
        {
            game = owner;
            game.GameCamera.allowHDR = true;
            var data = game.GameCamera.GetUniversalAdditionalCameraData(); data.renderPostProcessing = true; data.volumeLayerMask = 1;
            var sky = new GameObject("Ambient light"); sky.transform.SetParent(transform, false);
            global = sky.AddComponent<Light2D>(); global.lightType = Light2D.LightType.Global; global.intensity = .88f;
            for (int i = 0; i < focal.Length; i++) {
                var go = new GameObject("Ruins focal light " + i); go.transform.SetParent(transform, false);
                focal[i] = go.AddComponent<Light2D>(); focal[i].lightType = Light2D.LightType.Point; focal[i].pointLightOuterRadius = 6; focal[i].pointLightInnerRadius = 1.2f;
                go.transform.position = i == 0 ? new Vector3(-8, 4, 0) : i == 1 ? new Vector3(7, 2, 0) : new Vector3(0, -5, 0);
                focal[i].intensity = i == 0 ? .35f : .22f;
                focal[i].shadowIntensity = .18f;
            }
            volume = new GameObject("M5 world post processing").AddComponent<Volume>(); volume.transform.SetParent(transform, false); volume.isGlobal = true;
            if (M5Art.Catalog?.standard != null) standard = Instantiate(M5Art.Catalog.standard);
            if (M5Art.Catalog?.minimal != null) minimal = Instantiate(M5Art.Catalog.minimal);
        }
        void Update()
        {
            if (game == null || M5Art.Catalog == null) return;
            int stage = game.Adventure?.Stage ?? 1;
            global.color = Hues[stage - 1]; global.intensity = game.Settings.minimalEffects ? 1 : .9f;
            foreach (var light in focal) { light.color = stage == 2 ? new Color(1, .53f, .22f) : stage == 4 ? new Color(.9f, .4f, .8f) : new Color(.33f, .9f, .76f); light.enabled = !game.Settings.minimalEffects; }
            volume.profile = game.Settings.minimalEffects ? minimal : standard;
            if (volume.profile != null && volume.profile.TryGet<Vignette>(out var vignette)) vignette.active = game.Settings.vignette;
        }
        void OnDestroy() { if (standard != null) Destroy(standard); if (minimal != null) Destroy(minimal); }
    }
}
