using System.Collections.Generic;
using UnityEngine;

namespace Starfall
{
    public static class PrototypeVisuals
    {
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        public static readonly Color Gold = new Color(.94f, .74f, .36f);
        public static readonly Color Teal = new Color(.3f, .87f, .84f);
        public static readonly Color Enemy = new Color(1f, .42f, .32f);
        public static Sprite Sprite(string kind)
        {
            if (sprites.TryGetValue(kind, out var cached) && cached != null) return cached;
            const int size = 16;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "M1_" + kind };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                bool solid = true;
                if (kind == "player") solid = (x >= 3 && x <= 12 && y >= 2 && y <= 12) || (x >= 5 && x <= 10 && y >= 13 && y <= 14);
                if (kind == "chaser") solid = Mathf.Abs(x - 7.5f) + Mathf.Abs(y - 7.5f) < 8;
                if (kind == "shooter") solid = x >= 2 && x <= 13 && y >= 2 && y <= 13 && !(x < 5 && y > 10) && !(x > 10 && y < 5);
                if (kind == "orb" || kind == "coin") solid = (x - 7.5f) * (x - 7.5f) + (y - 7.5f) * (y - 7.5f) < 53;
                if (kind == "cross") solid = (x >= 6 && x <= 9 && y >= 2 && y <= 13) || (y >= 6 && y <= 9 && x >= 2 && x <= 13);
                if (kind == "arrow") solid = (x >= 2 && x <= 10 && y >= 6 && y <= 9) || (x >= 8 && x <= 14 && Mathf.Abs(y - 7.5f) <= (14 - x));
                pixels[y * size + x] = solid ? Color.white : Color.clear;
                if (solid && kind == "player" && y >= 8 && y <= 10 && x >= 5 && x <= 11) pixels[y * size + x] = new Color(.17f, .23f, .28f);
                if (solid && (kind == "shooter" || kind == "chaser") && y >= 7 && y <= 8 && x >= 5 && x <= 10) pixels[y * size + x] = new Color(.2f, .14f, .18f);
            }
            texture.SetPixels(pixels); texture.Apply();
            cached = UnityEngine.Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
            cached.name = "M1_" + kind; sprites[kind] = cached; return cached;
        }
        public static SpriteRenderer Draw(Transform parent, string name, Vector2 position, Vector2 size, Color color, int order, string kind = "square")
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = position;
            go.transform.localScale = new Vector3(size.x, size.y, 1);
            var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = Sprite(kind); renderer.color = color; renderer.sortingOrder = order;
            var material = Resources.Load<Material>("PrototypeSprite"); if (material != null) renderer.sharedMaterial = material;
            return renderer;
        }
        public static Rigidbody2D Body(GameObject go, float radius)
        {
            go.layer = 2; // Built-in Ignore Raycast: actors, never solid room geometry.
            var collider = go.AddComponent<CircleCollider2D>(); collider.radius = radius;
            var body = go.AddComponent<Rigidbody2D>(); body.gravityScale = 0; body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous; body.interpolation = RigidbodyInterpolation2D.Interpolate;
            var material = new PhysicsMaterial2D("M1 frictionless") { friction = 0, bounciness = 0 };
            collider.sharedMaterial = material;
            go.AddComponent<OwnedPhysicsMaterial>().Material = material;
            return body;
        }
        public static void Release()
        {
            foreach (var sprite in sprites.Values) if (sprite != null) { Object.Destroy(sprite.texture); Object.Destroy(sprite); }
            sprites.Clear();
        }
    }
    public sealed class OwnedPhysicsMaterial : MonoBehaviour
    {
        public PhysicsMaterial2D Material;
        void OnDestroy() { if (Material != null) Destroy(Material); }
    }
}
