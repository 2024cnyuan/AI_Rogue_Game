using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Starfall
{
    public enum FrontPage { None, Menu, Stages, Preparation, Reward }
    public sealed class FrontEndInterface : MonoBehaviour
    {
        StarfallGame game;
        GameObject root;
        FrontPage page = FrontPage.Menu, preparationReturn = FrontPage.Stages;
        ItemKind category;
        string selectedItem = "pistol", receiptId;
        LoadoutState prepared;
        Vector2 screen;
        readonly Dictionary<FrontPage,string> focusMemory=new Dictionary<FrontPage,string>();
        readonly Dictionary<ItemKind,string> itemMemory=new Dictionary<ItemKind,string>();
        FrontPage renderedPage; bool wasVisible;
        public bool IsOpen => page != FrontPage.None;
        public FrontPage Page => page;
        public void RefreshLayout() => Rebuild();
        public void Initialize(StarfallGame owner) { game = owner; game.Text.Changed += Rebuild; Rebuild(); }
        string T(string key) => game.Text.Get(key);
        public void OpenMenu() { page = FrontPage.Menu; Rebuild(); }
        public void OpenStages() { page = FrontPage.Stages; game.ModeUI.HideIntroduction(); Rebuild(); }
        public void OpenPreparation() { preparationReturn = page == FrontPage.Menu ? FrontPage.Menu : FrontPage.Stages; prepared = game.Profile.Prepared(); page = FrontPage.Preparation; Rebuild(); }
        public void Close() { page = FrontPage.None; if (root != null) root.SetActive(false); }
        void Update()
        {
            if (game == null || root == null) return;
            bool visible = IsOpen && game.Context == null && !game.Interface.FirstLaunch && !game.Interface.SettingsOpen && !game.AdventureUI.RecordsOpen;
            if(!visible && wasVisible) RememberFocus(); root.SetActive(visible);
            if(visible && !wasVisible) RestoreFocus(); wasVisible=visible;
            if (!visible) return;
            var canvas = (RectTransform)game.Interface.transform;
            if (Vector2.Distance(screen, canvas.rect.size) > 1) Rebuild();
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true && game.Interface.EscapeConsumedFrame != Time.frameCount) {
                if (page == FrontPage.Stages) OpenMenu();
                else if (page == FrontPage.Preparation) { page = preparationReturn; Rebuild(); }
                else if (page == FrontPage.Reward) OpenStages();
            }
        }
        void Rebuild()
        {
            if (game == null || M5Art.Catalog == null) return;
            RememberFocus();
            if (root != null) { root.SetActive(false); Destroy(root); }
            screen = ((RectTransform)game.Interface.transform).rect.size;
            var r = M5UI.Rect(game.Interface.transform, "M5 " + page, Vector2.zero, Vector2.zero); M5UI.Stretch(r); root = r.gameObject;
            var bg = M5UI.Box(root.transform, "Painted starport", Vector2.zero, screen, Color.white, true); bg.sprite = M5Art.Catalog.menu;
            var veil = M5UI.Box(root.transform, "Backdrop veil", Vector2.zero, screen, new Color(.025f, .06f, .065f, page == FrontPage.Menu ? .3f : .91f), true);
            if (page == FrontPage.Menu) BuildMenu(); else if (page == FrontPage.Stages) BuildStages(); else if (page == FrontPage.Preparation) BuildPreparation(); else if (page == FrontPage.Reward) BuildReward();
            renderedPage=page;
            if (game.Context != null || game.Interface.FirstLaunch || game.Interface.SettingsOpen || game.AdventureUI.RecordsOpen || page == FrontPage.None) root.SetActive(false);
            else RestoreFocus();
        }
        void RememberFocus() { var selected=EventSystem.current?.currentSelectedGameObject; if(root!=null && selected!=null && selected.transform.IsChildOf(root.transform)) focusMemory[renderedPage]=selected.name; }
        void RestoreFocus() { if(root==null || !root.activeSelf) return; string name=focusMemory.TryGetValue(page,out var old)?old:page==FrontPage.Menu?"m5.play":page==FrontPage.Stages?"Start selected stage":"Category "+category; foreach(var b in root.GetComponentsInChildren<Button>()) if(b.name==name && b.interactable) { M5UI.Focus(b); return; } }
        void SelectCategory(ItemKind kind) { itemMemory[category]=selectedItem; category=kind; if(itemMemory.TryGetValue(kind,out var id)) selectedItem=id; else foreach(var i in game.Catalog.items) if(i.kind==kind && i.implemented) { selectedItem=i.id; break; } Rebuild(); }
        Button Button(string key, Vector2 at, Vector2 size, System.Action callback, bool primary = false) => M5UI.Button(root.transform, key, T(key), at, size, () => { game.Audio.Play(GameSound.Ui); callback(); }, primary);
        void Header(string key, System.Action back)
        {
            M5UI.Text(root.transform, "Page title", T(key), new Vector2(60, screen.y / 2 - 64), new Vector2(screen.x - 360, 52), 34, true);
            Button("button.back", new Vector2(-screen.x / 2 + 92, screen.y / 2 - 64), new Vector2(120, 44), back);
        }
        void BuildMenu()
        {
            float x = -screen.x / 2 + 245;
            M5UI.Text(root.transform, "Game title", T("title").Replace(" ","\n"), new Vector2(x, 230), new Vector2(420, 112), 38, true);
            M5UI.Text(root.transform, "Menu subtitle", T("m5.menuSubtitle"), new Vector2(x, 147), new Vector2(420, 50), 18);
            Button("m5.play", new Vector2(x, 82), new Vector2(380, 56), OpenStages, true);
            Button("m5.preparation", new Vector2(x, 12), new Vector2(380, 48), OpenPreparation);
            Button("m5.trainingMenu", new Vector2(x - 99, -48), new Vector2(182, 44), game.StartTraining);
            Button("m5.tutorialMenu", new Vector2(x + 99, -48), new Vector2(182, 44), game.StartTutorial);
            Button("menu.records", new Vector2(x, -108), new Vector2(380, 44), () => game.AdventureUI.OpenRecords());
            Button("menu.settings", new Vector2(x - 99, -166), new Vector2(182, 44), game.Interface.OpenSettings);
            Button("menu.quit", new Vector2(x + 99, -166), new Vector2(182, 44), game.Quit);
            if (game.Checkpoints.HasEntry) M5UI.Button(root.transform, "Continue selected stage", T("menu.continue") + " · " + game.Checkpoints.Current.stage, new Vector2(x, -228), new Vector2(380, 42), () => { game.ContinueAdventure(); });
            var sprite = M5Art.Catalog.explorer[0]; M5UI.Art(root.transform, "Explorer showcase", sprite, new Vector2(screen.x * .19f, 0), new Vector2(330, 430));
            var gear = game.Profile.Prepared(); M5UI.Art(root.transform, "Equipped showcase weapon", M5Art.Get(gear.SpecialWeapon ?? "pistol"), new Vector2(screen.x * .27f, -16), new Vector2(150, 90));
            M5UI.Text(root.transform, "Collection count", T("m5.collection") + " " + game.Profile.Data.unlocked.Count + "/25", new Vector2(screen.x * .2f, -250), new Vector2(340, 38), 20, false, TextAlignmentOptions.Center);
        }
        void BuildStages()
        {
            Header("m5.selectStage", OpenMenu);
            bool narrow = screen.x / screen.y < 1.5f;
            int columns = narrow ? 2 : 3;
            float cardWidth = narrow ? 330 : 252, cardHeight = narrow ? 194 : 218;
            float left = -screen.x / 2 + 32 + cardWidth / 2, top = screen.y / 2 - 200;
            for (int i = 0; i < 6; i++) {
                int stage = i + 1; var at = new Vector2(left + i % columns * (cardWidth + 16), top - i / columns * (cardHeight + 16));
                var card = M5UI.Box(root.transform, "Stage card " + stage, at, new Vector2(cardWidth, cardHeight), game.SelectedStage == stage ? M5UI.Accent : M5UI.Ink, true);
                var button = card.gameObject.AddComponent<Button>(); button.targetGraphic = card; button.onClick.AddListener(() => { game.SelectedStage = stage; game.Audio.Play(GameSound.Ui); Rebuild(); });
                M5UI.CoverArt(card.transform, "Stage artwork", M5Art.Catalog.stageCards[i], new Vector2(0, 39), new Vector2(cardWidth - 12, cardHeight - 90));
                M5UI.Text(card.transform, "Stage name", T("stage." + StageRewards.StageIds[i]), new Vector2(0, -cardHeight / 2 + 51), new Vector2(cardWidth - 28, 58), 19, true);
                M5UI.Text(card.transform, "Stage state", T(game.Profile.IsClear(stage) ? "m5.cleared" : stage == 1 ? "m5.recommended" : "m5.open") + (game.Profile.HasPending(stage) ? " · " + T("m5.pending") : ""), new Vector2(0, -cardHeight / 2 + 12), new Vector2(cardWidth - 28, 26), 16);
            }
            float x = narrow ? screen.x / 2 - 252 : screen.x / 2 - 218;
            float w = narrow ? 410 : 360;
            var detail = M5UI.Box(root.transform, "Selected stage detail", new Vector2(x, -20), new Vector2(w, screen.y - 194), M5UI.Ink);
            M5UI.Text(detail.transform, "Selected title", T("stage." + StageRewards.StageIds[game.SelectedStage - 1]), new Vector2(0, screen.y / 2 - 157), new Vector2(w - 40, 88), 28, true);
            M5UI.Text(detail.transform, "Stage feature", T("m5.stageFeature." + game.SelectedStage), new Vector2(0, screen.y / 2 - 240), new Vector2(w - 40, 90), 19);
            M5UI.Text(detail.transform, "Clear reward heading", T("m5.unlockReward"), new Vector2(0, 46), new Vector2(w - 40, 32), 20);
            M5UI.Art(detail.transform, "Signature reward", M5Art.Get(StageRewards.Signature[game.SelectedStage - 1]), new Vector2(-w / 2 + 66, -20), new Vector2(90, 72));
            M5UI.Text(detail.transform, "Reward name", T("item." + StageRewards.Signature[game.SelectedStage - 1]), new Vector2(52, -20), new Vector2(w - 140, 72), 20);
            var best = game.Records.Find(StageRewards.StageIds[game.SelectedStage - 1], game.LevelConfig.timingVersion, game.LevelConfig.balanceVersion,"stage_select");
            M5UI.Text(detail.transform, "Personal best", T("records.title") + "\n" + (best == null ? T("records.none") : LevelTimer.Format(best.milliseconds)), new Vector2(0, -93), new Vector2(w - 40, 64), 18);
            var start = M5UI.Button(detail.transform, "Start selected stage", T("m5.startStage"), new Vector2(0, -(screen.y - 194) / 2 + 40), new Vector2(w - 40, 52), () => game.StartStage(game.SelectedStage), true);
            Button("m5.preparation", new Vector2(x, -screen.y / 2 + 55), new Vector2(w, 44), OpenPreparation);
            if (game.Profile.HasPending(game.SelectedStage)) Button("m5.claimPending", new Vector2(-screen.x / 2 + 200, -screen.y / 2 + 55), new Vector2(330, 44), () => { receiptId = game.Profile.Pending(game.SelectedStage).id; page = FrontPage.Reward; Rebuild(); });
            M5UI.Text(root.transform, "All stages open", T("m5.allStagesOpen"), new Vector2(-screen.x / 2 + 418, -screen.y / 2 + 96), new Vector2(740, 35), 17);
        }
        void BuildPreparation()
        {
            if (prepared == null) prepared = game.Profile.Prepared();
            Header("m5.preparation", () => { page = preparationReturn; Rebuild(); });
            M5UI.Art(root.transform, "Preparation explorer", M5Art.Catalog.explorer[0], new Vector2(-460, 45), new Vector2(225, 330));
            M5UI.Art(root.transform, "Preparation weapon", M5Art.Get(prepared.SpecialWeapon ?? "pistol"), new Vector2(-408, 65), new Vector2(110, 80));
            M5UI.Text(root.transform, "Preparation rules", T("m5.preparationRules"), new Vector2(-438, -225), new Vector2(292, 135), 17);
            string[] slots = new string[9]; slots[0] = "pistol"; slots[1] = prepared.SpecialWeapon; slots[2] = prepared.Active;
            int index = 3; foreach (var p in prepared.Passives) slots[index++] = p.Key;
            for (int i = 0; i < slots.Length; i++) {
                string id = slots[i]; var at = new Vector2(-215 + i % 3 * 110, 168 - i / 3 * 120);
                var box = M5UI.Box(root.transform, "Loadout slot " + i, at, new Vector2(96, 106), M5UI.Ink);
                if (id != null) M5UI.Art(box.transform, "Slot icon", M5Art.Get(id), new Vector2(0, 17), new Vector2(66, 50));
                M5UI.Text(box.transform, "Slot label", id == null ? T("m5.empty") : T("m5.item." + id), new Vector2(0, -27), new Vector2(90, 48), 16, false, TextAlignmentOptions.Center);
            }
            int c = 0; foreach (ItemKind k in System.Enum.GetValues(typeof(ItemKind))) { var kind = k; M5UI.Button(root.transform, "Category " + k, T("m5.kind." + k.ToString().ToLowerInvariant()), new Vector2(156 + c++ * 144, 230), new Vector2(134, 40), () => SelectCategory(kind)); }
            int n = 0;
            foreach (var item in game.Catalog.items) if (item.kind == category && item.implemented) {
                string id = item.id; bool owned = game.Profile.Owns(id); var at = new Vector2(144 + n % 4 * 110, 144 - n / 4 * 120); n++;
                var b = M5UI.Button(root.transform, "Gear " + id, "", at, new Vector2(98, 112), () => { selectedItem = id; Rebuild(); });
                b.GetComponent<Image>().color=id==selectedItem?M5UI.Accent:M5UI.Ink;
                M5UI.Art(b.transform, "Gear icon", M5Art.Get(id), new Vector2(0, 10), new Vector2(62, 36)).color = owned ? Color.white : new Color(.55f, .62f, .62f);
                if(!owned) M5UI.Text(b.transform,"Locked badge",T("m5.locked"),new Vector2(0,43),new Vector2(90,22),16,false,TextAlignmentOptions.Center);
                M5UI.Text(b.transform, "Gear name", T("m5.item."+id), new Vector2(0, -31), new Vector2(90, 46), 16, false, TextAlignmentOptions.Center);
            }
            var selected = game.Catalog.Find(selectedItem);
            M5UI.Text(root.transform, "Item detail", T(selected.nameKey) + "\n" + game.Catalog.Describe(game.Text, selected) + "\n" + (game.Profile.Owns(selectedItem) ? T("m5.permanent") : T("m5.source") + " " + StageRewards.Source(selectedItem)), new Vector2(310, -218), new Vector2(442, 105), 18);
            var equip = Button("m5.equip", new Vector2(211, -screen.y / 2 + 56), new Vector2(198, 46), () => { if (game.Profile.Owns(selectedItem) && prepared.Equip(selectedItem) && game.Profile.SavePreparation(prepared)) Rebuild(); }, true); equip.interactable = game.Profile.Owns(selectedItem) && (selected.kind != ItemKind.Passive || prepared.Layers(selectedItem) == 0 && prepared.Passives.Count < 6);
            Button("m5.remove", new Vector2(425, -screen.y / 2 + 56), new Vector2(198, 46), () => { if (selected.kind == ItemKind.Passive) prepared.RemovePassive(selectedItem); else if (selectedItem == prepared.SpecialWeapon || selectedItem == prepared.Active) prepared.Unequip(selected.kind); game.Profile.SavePreparation(prepared); Rebuild(); });
        }
        void BuildReward()
        {
            Header("m5.claimPending", OpenStages);
            var choices = game.Profile.Choices(receiptId);
            M5UI.Text(root.transform, "Pending reward explanation", T("m5.pendingHint"), new Vector2(0, 172), new Vector2(880, 85), 24);
            for (int i = 0; i < choices.Count; i++) {
                string id = choices[i]; var at = new Vector2(-310 + i * 310, -10);
                var card = M5UI.Button(root.transform, "Unlock " + id, "", at, new Vector2(280, 280), () => { if (game.Profile.Choose(receiptId, id)) OpenStages(); }, true);
                M5UI.Art(card.transform, "Reward icon", M5Art.Get(id), new Vector2(0, 48), new Vector2(140, 120));
                M5UI.Text(card.transform, "Reward description", T("item." + id) + "\n" + game.Catalog.Describe(game.Text, game.Catalog.Find(id)), new Vector2(0, -77), new Vector2(248, 112), 19);
            }
            if (choices.Count == 0) { game.Profile.Choose(receiptId, null); M5UI.Text(root.transform, "Collected", T("m5.allCollected"), Vector2.zero, new Vector2(800, 80), 28, true); }
        }
        void OnDestroy() { if (game?.Text != null) game.Text.Changed -= Rebuild; }
    }
}
