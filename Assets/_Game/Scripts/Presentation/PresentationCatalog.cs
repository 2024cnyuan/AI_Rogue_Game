using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;

namespace Starfall
{
    [Serializable] public sealed class ArtEntry { public string id; public Sprite sprite; public GameObject prefab; }
    [CreateAssetMenu(menuName = "Starfall/Presentation Catalog")]
    public sealed class PresentationCatalog : ScriptableObject
    {
        public ArtEntry[] art;
        public Sprite[] explorer;
        public GameObject explorerVisual;
        public Sprite menu;
        public Sprite[] stageCards;
        public TMP_FontAsset body, emphasis, title;
        public Material lit, unlit;
        public VolumeProfile standard, minimal;
        public AudioClip[] sounds;
        public AudioClip[] weapons;
        public AudioClip ambience;
        public AudioMixer mixer;
        public AudioMixerGroup effectsGroup, musicGroup, uiGroup;
    }
    public static class M5Art
    {
        static PresentationCatalog catalog;
        static Dictionary<string, Sprite> sprites;
        static Dictionary<string,GameObject> prefabs;
        public static PresentationCatalog Catalog => catalog != null ? catalog : catalog = Resources.Load<PresentationCatalog>("PresentationCatalog");
        public static Sprite Get(string id)
        {
            if (Catalog == null || id == null) return null;
            if (sprites == null) { sprites = new Dictionary<string, Sprite>(); foreach (var entry in Catalog.art) if (entry.sprite != null) sprites[entry.id] = entry.sprite; }
            return sprites.TryGetValue(id, out var sprite) ? sprite : null;
        }
        public static SpriteRenderer Draw(Transform parent, string id, Vector2 at, float height, int order, bool lit = true)
        {
            var sprite = Get(id); if (sprite == null) return null;
            if(prefabs==null) { prefabs=new Dictionary<string,GameObject>(); foreach(var entry in Catalog.art) if(entry.prefab!=null) prefabs[entry.id]=entry.prefab; }
            var go = prefabs.TryGetValue(id,out var source) ? UnityEngine.Object.Instantiate(source,parent,false) : new GameObject("Art " + id); go.name="Art "+id; go.transform.SetParent(parent,false); go.transform.localPosition=at;
            var r = go.GetComponent<SpriteRenderer>() ?? go.AddComponent<SpriteRenderer>(); r.sprite = sprite; r.sortingOrder = order; r.color = Color.white;
            go.transform.localScale = Vector3.one * (height / Mathf.Max(.01f, sprite.bounds.size.y));
            if (Catalog != null) r.sharedMaterial = lit ? Catalog.lit : Catalog.unlit;
            return r;
        }
        public static SpriteRenderer Shadow(Transform parent, float width)
        {
            return PrototypeVisuals.Draw(parent, "Contact shadow", new Vector2(0, -.12f), new Vector2(width, width * .3f), new Color(.01f, .025f, .03f, .35f), 2, "orb");
        }
        public static void Apply(SpriteRenderer r, string id, float height)
        {
            var sprite = Get(id); if (r == null || sprite == null) return;
            r.sprite = sprite; r.color = Color.white; r.sharedMaterial = Catalog.lit;
            r.transform.localScale = Vector3.one * height / Mathf.Max(.01f, sprite.bounds.size.y);
            r.gameObject.AddComponent<PaintedActorMotion>();
        }
        public static void ResetCache() { catalog = null; sprites = null; prefabs=null; }
    }
    public sealed class PaintedActorMotion : MonoBehaviour
    {
        SpriteRenderer view;
        Vector3 initial;
        void Start() { view = GetComponent<SpriteRenderer>(); initial = transform.localPosition; }
        void LateUpdate()
        {
            if (view == null || Time.timeScale == 0) return;
            transform.localPosition = initial + new Vector3(0, Mathf.Sin(Time.time * 4 + transform.parent.position.x) * .035f, 0);
            view.sortingOrder = 40 - Mathf.RoundToInt(transform.parent.position.y * 4);
        }
    }
}
