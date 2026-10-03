using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Text = TMPro.TextMeshProUGUI;
namespace Starfall
{
    public sealed class LoadoutInterface : MonoBehaviour
    {
        StarfallGame game;
        GameObject panel;
        Text title;
        readonly Button[] choices = new Button[6];
        Button discard;
        GameObject equipmentPanel;
        TMPro.TextMeshProUGUI oldDescription, newDescription;
        TMPro.TextMeshProUGUI comparisonTitle;
        Image oldIcon, newIcon;
        public void Initialize(StarfallGame owner) {
            game=owner; panel=new GameObject("Passive replacement",typeof(RectTransform),typeof(Image)); panel.transform.SetParent(game.Interface.transform,false);
            var rect=(RectTransform)panel.transform; rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero; panel.GetComponent<Image>().color=new Color(.02f,.03f,.06f,.96f);
            title=Label("Replace passive",new Vector2(0,225),new Vector2(1000,100),22);
            for (int i=0;i<6;i++) { int index=i; choices[i]=Button(new Vector2((i%2==0?-1:1)*230,110-i/2*85),new Vector2(440,72)); choices[i].onClick.AddListener(()=> { int at=0; string old=null; foreach (var pair in game.Loadout.Passives) { if (at++==index) { old=pair.Key; break; } } if(old!=null) game.ReplacePassive(old); }); }
            discard=Button(new Vector2(0,-210),new Vector2(440,50)); discard.onClick.AddListener(game.CancelPassive);
            panel.SetActive(false);
            var overlay = M5UI.Box(game.Interface.transform,"Equipment comparison",Vector2.zero,Vector2.zero,new Color(.02f,.05f,.06f,.95f),true); M5UI.Stretch(overlay.rectTransform); equipmentPanel = overlay.gameObject;
            comparisonTitle=M5UI.Text(overlay.transform,"Comparison title",game.Text.Get("m5.compare"),new Vector2(0,240),new Vector2(900,60),32,true,TextAlignmentOptions.Center);
            var left = M5UI.Box(overlay.transform,"Current gear card",new Vector2(-220,30),new Vector2(400,340),M5UI.Ink);
            var right = M5UI.Box(overlay.transform,"New gear card",new Vector2(220,30),new Vector2(400,340),M5UI.Ink);
            oldIcon = M5UI.Art(left.transform,"Current icon",null,new Vector2(0,72),new Vector2(170,130)); newIcon = M5UI.Art(right.transform,"New icon",null,new Vector2(0,72),new Vector2(170,130));
            oldDescription = M5UI.Text(left.transform,"Current description","",new Vector2(0,-82),new Vector2(350,152),20); newDescription = M5UI.Text(right.transform,"New description","",new Vector2(0,-82),new Vector2(350,152),20);
            M5UI.Button(overlay.transform,"m5.confirmReplace",game.Text.Get("m5.confirmReplace"),new Vector2(220,-220),new Vector2(360,52),()=>game.ConfirmEquipmentReplacement(),true);
            M5UI.Button(overlay.transform,"m5.cancelReplace",game.Text.Get("m5.cancelReplace"),new Vector2(-220,-220),new Vector2(360,52),game.CancelEquipmentReplacement);
            equipmentPanel.SetActive(false);
            foreach(var choice in choices) { var text=choice.GetComponentInChildren<Text>(); text.rectTransform.anchoredPosition=new Vector2(32,0); text.rectTransform.sizeDelta-=new Vector2(64,0); M5UI.Art(choice.transform,"Passive icon",null,new Vector2(-175,0),new Vector2(56,56)); }
        }
        Text Label(string name,Vector2 at,Vector2 size,int font) { var go=new GameObject(name,typeof(RectTransform),typeof(Text)); go.transform.SetParent(panel.transform,false); var text=go.GetComponent<Text>(); text.font=game.Interface.SharedFont; text.fontSize=font; text.color=Color.white; text.alignment=TextAlignmentOptions.Center; text.richText=false; text.raycastTarget=false; text.rectTransform.anchoredPosition=at; text.rectTransform.sizeDelta=size; return text; }
        Button Button(Vector2 at,Vector2 size) { var go=new GameObject("Replacement choice",typeof(RectTransform),typeof(Image),typeof(Button)); go.transform.SetParent(panel.transform,false); var rect=(RectTransform)go.transform; rect.anchoredPosition=at; rect.sizeDelta=size; var img=go.GetComponent<Image>(); img.color=new Color(.15f,.3f,.36f); var button=go.GetComponent<Button>(); button.targetGraphic=img; var text=Label("Choice text",Vector2.zero,size-new Vector2(20,8),18); text.transform.SetParent(go.transform,false); return button; }
        void LateUpdate() => RefreshNow();
        public void RefreshNow() {
            equipmentPanel.SetActive(game.PendingEquipment != null);
            if(game.PendingEquipment != null) {
                comparisonTitle.text=game.Text.Get("m5.compare");
                equipmentPanel.transform.SetAsLastSibling(); var next=game.Catalog.Find(game.PendingEquipment); string old=next.kind==ItemKind.Weapon?game.Loadout.SpecialWeapon:game.Loadout.Active;
                oldIcon.sprite=M5Art.Get(old); newIcon.sprite=M5Art.Get(next.id);
                oldDescription.text=game.Text.Get("m5.current")+"\n"+game.Text.Get("item."+old)+"\n"+game.Catalog.Describe(game.Text,game.Catalog.Find(old));
                newDescription.text=game.Text.Get("m5.new")+"\n"+game.Text.Get(next.nameKey)+"\n"+game.Catalog.Describe(game.Text,next);
                foreach(var button in equipmentPanel.GetComponentsInChildren<Button>()) button.GetComponentInChildren<Text>().text=game.Text.Get(button.name);
            }
            bool open=game.PendingPassive!=null; panel.SetActive(open); if(!open) return;
            panel.transform.SetAsLastSibling(); title.text=game.Text.Get("item.replaceTitle",("item",game.Text.Get(game.Catalog.Find(game.PendingPassive).nameKey)),("effect",game.Catalog.DescribeShort(game.Text,game.Catalog.Find(game.PendingPassive))));
            int i=0; foreach(var pair in game.Loadout.Passives) { choices[i].gameObject.SetActive(true); choices[i].transform.Find("Passive icon").GetComponent<Image>().sprite=M5Art.Get(pair.Key); choices[i++].GetComponentInChildren<Text>().text=game.Text.Get("item.replaceOld",("item",game.Text.Get(game.Catalog.Find(pair.Key).nameKey)),("layers",pair.Value.ToString())); }
            for (;i<6;i++) choices[i].gameObject.SetActive(false); discard.GetComponentInChildren<Text>().text=game.Text.Get("item.discard");
        }
    }
}
