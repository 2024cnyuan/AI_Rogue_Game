using System;
using UnityEngine;

namespace Starfall
{
    public sealed class PracticeInteractable : MonoBehaviour
    {
        public string Id { get; private set; }
        public string NameKey { get; private set; }
        public Action Action { get; private set; }
        public bool IsPickup { get; private set; }
        public bool Completed { get; private set; }
        SpriteRenderer view;
        StarfallGame game;
        public void Invoke() { if (!Completed) { if(!IsPickup) game?.Audio?.Play(GameSound.Ui); Action?.Invoke(); } }
        public void Complete() { Completed = true; if (view != null) view.color = new Color(.45f, .55f, .55f, .7f); }
        public void Initialize(string id, string nameKey, Action action, Color color)
        {
            Id = id; NameKey = nameKey; Action = action; IsPickup = nameKey.StartsWith("item.");
            game=GetComponentInParent<StarfallGame>();
            string item = IsPickup ? nameKey.Substring(5) : id.StartsWith("route.") ? "gate" : id.StartsWith("beacon.") || id.Contains("relay") || id.Contains("beacon") ? "beacon" : id.Contains("chest") ? "chest" : "workstation";
            view = M5Art.Draw(transform, item, Vector2.zero, IsPickup ? .8f : item == "gate" ? 2.1f : 1.4f, 4);
            if (view == null) view = PrototypeVisuals.Draw(transform, "Station", Vector2.zero, Vector2.one * .55f, color, 3, "cross");
            M5Art.Shadow(transform, IsPickup ? .55f : .9f);
        }
        void LateUpdate() {
            if(view==null || game==null || Completed) return;
            bool selected=game.PickupInteraction==this || game.Interaction==this;
            view.color=selected ? new Color(1,1,.78f) : Color.white;
            view.sortingOrder=40-Mathf.RoundToInt(transform.position.y*4);
            if(IsPickup && game.CanAct) view.transform.localPosition=new Vector2(0,.13f+Mathf.Sin(Time.time*3+transform.position.x)*.07f);
        }
        void OnDestroy() => Action = null;
    }
}
