using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Starfall
{
    public sealed class FeedbackPresenter : MonoBehaviour
    {
        sealed class Toast { public string id; public float amount, left; public bool item; }
        readonly List<Toast> messages = new List<Toast>();
        readonly List<GameObject> cards = new List<GameObject>();
        readonly List<TextMeshProUGUI> labels = new List<TextMeshProUGUI>();
        readonly List<Image> icons = new List<Image>();
        readonly List<TextMeshProUGUI> numbers = new List<TextMeshProUGUI>();
        readonly float[] ages = new float[24];
        readonly Vector2[] positions = new Vector2[24];
        readonly Damageable[] targets = new Damageable[24];
        readonly bool[] criticals = new bool[24];
        readonly float[] amounts = new float[24];
        StarfallGame game;
        GameObject root;
        int next;
        public void Initialize(StarfallGame owner)
        {
            game = owner; var r = M5UI.Rect(game.Interface.transform, "Pickup and combat feedback", Vector2.zero, Vector2.zero); M5UI.Stretch(r); root = r.gameObject;
            for (int i = 0; i < 3; i++) {
                var card = M5UI.Box(root.transform, "Pickup toast " + i, Vector2.zero, new Vector2(300, 82), M5UI.Ink);
                card.rectTransform.anchorMin = card.rectTransform.anchorMax = new Vector2(1, 0); card.rectTransform.pivot = new Vector2(1, 0);
                card.rectTransform.anchoredPosition = new Vector2(-24, 28 + i * 94); cards.Add(card.gameObject);
                icons.Add(M5UI.Art(card.transform, "Pickup icon", null, new Vector2(-111, 0), new Vector2(58, 58)));
                labels.Add(M5UI.Text(card.transform, "Pickup name and amount", "", new Vector2(32, 0), new Vector2(205, 70), 18));
            }
            for (int i = 0; i < 24; i++) {
                var t = M5UI.Text(root.transform, "Damage number " + i, "", Vector2.zero, new Vector2(130, 42), 23, false, TextAlignmentOptions.Center); numbers.Add(t); t.gameObject.SetActive(false);
            }
        }
        public void Item(string id) { messages.Add(new Toast { id = id, item = true, left = 2.8f }); }
        public void Resource(string id, float amount)
        {
            if (amount <= 0) return;
            if (messages.Count > 0) { var last = messages[messages.Count - 1]; if (!last.item && last.id == id && last.left > 1.5f) { last.amount += amount; last.left = 1.8f; return; } }
            messages.Add(new Toast { id = id, amount = amount, left = 1.8f });
        }
        public void Damage(Damageable target, float amount, bool critical)
        {
            if (!game.Settings.damageNumbers || target.Faction == Faction.Player || amount <= 0) return;
            int slot = -1; for(int i=0;i<24;i++) if(ages[i]>.57f && targets[i]==target && criticals[i]==critical) { slot=i; break; }
            if(slot<0) { slot = next++ % 24; amounts[slot]=0; }
            positions[slot] = target.transform.position; ages[slot] = .65f; targets[slot]=target; criticals[slot]=critical; amounts[slot]+=amount;
            numbers[slot].text = (critical ? "! " : "") + Mathf.CeilToInt(amounts[slot]); numbers[slot].color = critical ? PrototypeVisuals.Gold : M5UI.Ivory;
        }
        public void Clear() { messages.Clear(); for (int i = 0; i < ages.Length; i++) ages[i] = 0; }
        void Update()
        {
            if (game == null) return;
            root.SetActive(game.Context != null && !game.Pause.IsPaused);
            if (!root.activeSelf) return;
            for (int i = Mathf.Min(3, messages.Count) - 1; i >= 0; i--) { messages[i].left -= Time.deltaTime; if (messages[i].left <= 0) messages.RemoveAt(i); }
            for (int i = 0; i < 3; i++) {
                cards[i].SetActive(i < messages.Count); if (i >= messages.Count) continue;
                var m = messages[i]; icons[i].sprite = M5Art.Get(m.id); var item = game.Catalog.Find(m.id);
                labels[i].text = m.item ? game.Text.Get("m5.obtained", ("item",game.Text.Get(item.nameKey))) + (item.kind == ItemKind.Passive ? " · " + game.Loadout.Layers(m.id) + "/2" : "") : game.Text.Get("m5.resource." + m.id, ("count",m.amount.ToString("0.#")));
            }
            var canvas = (RectTransform)game.Interface.transform;
            for (int i = 0; i < numbers.Count; i++) {
                ages[i] = Mathf.Max(0, ages[i] - Time.deltaTime); numbers[i].gameObject.SetActive(ages[i] > 0 && game.Settings.damageNumbers);
                if (ages[i] <= 0) continue;
                var at = game.GameCamera.WorldToScreenPoint(positions[i] + Vector2.up * (1.05f + (.65f - ages[i])));
                numbers[i].rectTransform.anchoredPosition = (Vector2)at / game.Interface.Canvas.scaleFactor - canvas.rect.size / 2;
                var c = numbers[i].color; c.a = Mathf.Clamp01(ages[i] * 5); numbers[i].color = c;
            }
        }
    }
}
