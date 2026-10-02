using UnityEngine;
namespace Starfall
{
    public sealed class DestructibleCover : MonoBehaviour
    {
        public Rect Bounds;
        PrototypeRoom room;
        Damageable health;
        public void Initialize(StarfallGame game, PrototypeRoom owner) {
            room = owner; health = gameObject.AddComponent<Damageable>(); health.Initialize(Faction.Enemy, 70, 0); health.Game = game; health.Died += Break;
        }
        void Break() { GetComponent<Collider2D>().enabled = false; room.RemoveCover(Bounds); gameObject.SetActive(false); }
        void OnDestroy() { if (health != null) health.Died -= Break; }
    }
}
