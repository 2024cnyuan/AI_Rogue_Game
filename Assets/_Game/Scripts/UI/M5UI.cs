using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Starfall
{
    public static class M5UI
    {
        public static readonly Color Ink = new Color(.055f, .105f, .115f, .98f), Ivory = new Color(.94f, .92f, .84f), Muted = new Color(.57f, .69f, .67f), Accent = new Color(.2f, .53f, .48f);
        public static RectTransform Rect(Transform parent, string name, Vector2 at, Vector2 size, Vector2? anchor = null)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = anchor ?? new Vector2(.5f, .5f); r.anchoredPosition = at; r.sizeDelta = size; return r;
        }
        public static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
        public static Image Box(Transform parent, string name, Vector2 at, Vector2 size, Color color, bool hit = false)
        {
            var r = Rect(parent, name, at, size); var image = r.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = hit; return image;
        }
        public static TextMeshProUGUI Text(Transform parent, string name, string value, Vector2 at, Vector2 size, int fontSize = 20, bool title = false, TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            var r = Rect(parent, name, at, size); var text = r.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value; text.font = title ? M5Art.Catalog.title : M5Art.Catalog.body; text.fontSize = Mathf.Max(16, fontSize);
            text.color = Ivory; text.alignment = align; text.raycastTarget = false; text.richText = false;
            text.textWrappingMode = TextWrappingModes.Normal; text.overflowMode = TextOverflowModes.Overflow; return text;
        }
        public static Image Art(Transform parent, string name, Sprite sprite, Vector2 at, Vector2 size)
        {
            var image = Box(parent, name, at, size, Color.white); image.sprite = sprite; image.preserveAspect = true; return image;
        }
        public static Image CoverArt(Transform parent, string name, Sprite sprite, Vector2 at, Vector2 size)
        {
            var frame=Box(parent,name+" frame",at,size,Color.clear); frame.gameObject.AddComponent<RectMask2D>();
            float ratio=sprite.rect.width/sprite.rect.height; float height=Mathf.Max(size.y,size.x/ratio);
            return Art(frame.transform,name,sprite,Vector2.zero,new Vector2(height*ratio,height));
        }
        public static Button Button(Transform parent, string name, string value, Vector2 at, Vector2 size, Action action, bool primary = false)
        {
            var image = Box(parent, name, at, size, primary ? Accent : new Color(.1f, .2f, .21f), true);
            var outline = image.gameObject.AddComponent<Outline>(); outline.effectColor = primary ? new Color(.58f, .74f, .63f, .6f) : new Color(.48f, .62f, .6f, .25f); outline.effectDistance = new Vector2(1, -1);
            var b = image.gameObject.AddComponent<Button>(); b.targetGraphic = image;
            var colors = b.colors; colors.highlightedColor = new Color(.74f, .93f, .84f); colors.selectedColor = colors.highlightedColor; colors.pressedColor = new Color(.52f, .8f, .65f); colors.disabledColor = new Color(.5f, .55f, .55f); b.colors = colors;
            var label = Text(image.transform, name + " label", value, Vector2.zero, size - new Vector2(24, 8), 20, false, TextAlignmentOptions.Center);
            b.onClick.AddListener(() => { action?.Invoke(); }); return b;
        }
        public static void Focus(Button button) { if (EventSystem.current != null && button != null) EventSystem.current.SetSelectedGameObject(button.gameObject); }
    }
}
