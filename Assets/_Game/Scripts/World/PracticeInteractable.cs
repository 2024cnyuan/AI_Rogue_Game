using System;
using UnityEngine;

namespace Starfall
{
    public sealed class PracticeInteractable : MonoBehaviour
    {
        public string Id { get; private set; }
        public string NameKey { get; private set; }
        public Action Action { get; private set; }
        public void Initialize(string id, string nameKey, Action action, Color color)
        {
            Id = id; NameKey = nameKey; Action = action;
            PrototypeVisuals.Draw(transform, "Station base", Vector2.zero, Vector2.one * .8f, new Color(.06f, .1f, .15f), 2);
            PrototypeVisuals.Draw(transform, "Station", Vector2.zero, Vector2.one * .55f, color, 3, "cross");
        }
        void OnDestroy() => Action = null;
    }
}
