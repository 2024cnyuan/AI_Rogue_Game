using UnityEngine;
using UnityEngine.UI;
namespace Starfall
{
    public sealed class LoadoutInterface : MonoBehaviour
    {
        StarfallGame game;
        GameObject panel;
        Text title;
        readonly Button[] choices = new Button[6];
        Button discard;
        public void Initialize(StarfallGame owner) {
            game=owner; panel=new GameObject("Passive replacement",typeof(RectTransform),typeof(Image)); panel.transform.SetParent(game.Interface.transform,false);
            var rect=(RectTransform)panel.transform; rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero; panel.GetComponent<Image>().color=new Color(.02f,.03f,.06f,.96f);
            title=Label("Replace passive",new Vector2(0,225),new Vector2(1000,90),22);
            for (int i=0;i<6;i++) { int index=i; choices[i]=Button(new Vector2((i%2==0?-1:1)*230,110-i/2*85),new Vector2(440,72)); choices[i].onClick.AddListener(()=> { int at=0; string old=null; foreach (var pair in game.Loadout.Passives) { if (at++==index) { old=pair.Key; break; } } if(old!=null) game.ReplacePassive(old); }); }
            discard=Button(new Vector2(0,-210),new Vector2(440,50)); discard.onClick.AddListener(game.CancelPassive);
            panel.SetActive(false);
        }
        Text Label(string name,Vector2 at,Vector2 size,int font) { var go=new GameObject(name,typeof(RectTransform),typeof(Text)); go.transform.SetParent(panel.transform,false); var text=go.GetComponent<Text>(); text.font=game.Interface.SharedFont; text.fontSize=font; text.color=Color.white; text.alignment=TextAnchor.MiddleCenter; text.supportRichText=false; text.raycastTarget=false; text.rectTransform.anchoredPosition=at; text.rectTransform.sizeDelta=size; return text; }
        Button Button(Vector2 at,Vector2 size) { var go=new GameObject("Replacement choice",typeof(RectTransform),typeof(Image),typeof(Button)); go.transform.SetParent(panel.transform,false); var rect=(RectTransform)go.transform; rect.anchoredPosition=at; rect.sizeDelta=size; var img=go.GetComponent<Image>(); img.color=new Color(.15f,.3f,.36f); var button=go.GetComponent<Button>(); button.targetGraphic=img; var text=Label("Choice text",Vector2.zero,size-new Vector2(20,8),18); text.transform.SetParent(go.transform,false); return button; }
        void LateUpdate() => RefreshNow();
        public void RefreshNow() {
            bool open=game.PendingPassive!=null; panel.SetActive(open); if(!open) return;
            panel.transform.SetAsLastSibling(); title.text=game.Text.Get("item.replaceTitle",("item",game.Text.Get(game.Catalog.Find(game.PendingPassive).nameKey)),("effect",game.Catalog.Describe(game.Text,game.Catalog.Find(game.PendingPassive))));
            int i=0; foreach(var pair in game.Loadout.Passives) { choices[i].gameObject.SetActive(true); choices[i++].GetComponentInChildren<Text>().text=game.Text.Get("item.replaceOld",("item",game.Text.Get(game.Catalog.Find(pair.Key).nameKey)),("layers",pair.Value.ToString())); }
            for (;i<6;i++) choices[i].gameObject.SetActive(false); discard.GetComponentInChildren<Text>().text=game.Text.Get("item.discard");
        }
    }
}
