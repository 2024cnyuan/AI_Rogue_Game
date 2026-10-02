using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Starfall
{
    public enum PracticePanel { None, Equipment, Range, Dodge, Simulation, Environment }
    public sealed class PracticeInterface : MonoBehaviour
    {
        readonly List<(Text view, string key)> labels = new List<(Text, string)>();
        readonly Dictionary<string, Text> itemLabels = new Dictionary<string, Text>();
        StarfallGame game;
        GameObject tutorialBubble, trainingBar, completion, trainingPanel, introduction, zoneLabels;
        readonly List<(Text view, Vector2 point)> zones = new List<(Text, Vector2)>();
        bool showIntroduction;
        Transform content;
        Text instruction, hint, summary, trainingStatus, activeStatus, liveDamage, equipmentStatus, description;
        Button skipDodge;
        PracticePanel current;
        float refreshLeft;
        string selectedItem = "shotgun";
        public bool IsOpen => current != PracticePanel.None;
        public void Initialize(StarfallGame owner)
        {
            game = owner; BuildZones(); BuildTutorial(); BuildTrainingBar(); BuildCompletion(); BuildIntroduction(); game.Text.Changed += RefreshNow; RefreshNow();
        }
        RectTransform Rect(Transform parent, string name)
        {
            var rect = new GameObject(name ?? "Practice UI", typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); return rect;
        }
        Image Box(Transform parent, string name, Color color, bool raycast = false)
        {
            var rect = Rect(parent, name); var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = raycast; return image;
        }
        Text Label(Transform parent, string key, float height = 32, int size = 18)
        {
            var rect = Rect(parent, key); var text = rect.gameObject.AddComponent<Text>(); text.font = game.Interface.SharedFont; text.fontSize = size;
            text.color = new Color(.9f, .95f, .94f); text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false; text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow;
            var element = text.gameObject.AddComponent<LayoutElement>(); element.preferredHeight = element.minHeight = height;
            if (key != null) labels.Add((text, key)); return text;
        }
        Button Button(Transform parent, string key, Action callback, float height = 34, bool enabled = true)
        {
            var box = Box(parent, key, new Color(.16f, .31f, .36f), true); var layout = box.gameObject.AddComponent<LayoutElement>(); layout.preferredHeight = layout.minHeight = height;
            var button = box.gameObject.AddComponent<Button>(); button.targetGraphic = box; button.interactable = enabled;
            var navigation = button.navigation; navigation.mode = Navigation.Mode.None; button.navigation = navigation;
            button.onClick.AddListener(() => callback?.Invoke()); var label = Label(box.transform, key, height);
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one; label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero; return button;
        }
        Transform Row(Transform parent, float height = 35)
        {
            var row = Rect(parent, "Action row"); row.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            var group = row.gameObject.AddComponent<HorizontalLayoutGroup>(); group.spacing = 8; group.childControlHeight = group.childControlWidth = true;
            group.childForceExpandWidth = true; group.childForceExpandHeight = false; return row;
        }
        void Layout(Transform parent, int padding = 18)
        {
            var group = parent.gameObject.AddComponent<VerticalLayoutGroup>(); group.padding = new RectOffset(padding, padding, padding, padding);
            group.spacing = 7; group.childControlHeight = group.childControlWidth = true; group.childForceExpandHeight = false; group.childForceExpandWidth = true;
        }
        Transform Panel(string name, float width, float height, out GameObject root)
        {
            var overlay = Box(game.Interface.transform, name, new Color(.015f, .025f, .05f, .86f), true);
            overlay.rectTransform.anchorMin = Vector2.zero; overlay.rectTransform.anchorMax = Vector2.one; overlay.rectTransform.offsetMin = overlay.rectTransform.offsetMax = Vector2.zero;
            var inner = Box(overlay.transform, name + " body", new Color(.07f, .13f, .19f, .99f), true); inner.rectTransform.sizeDelta = new Vector2(width, height);
            Layout(inner.transform, 12); root = overlay.gameObject; return inner.transform;
        }
        void BuildTutorial()
        {
            var box = Box(game.Interface.transform, "Tutorial guide", new Color(.07f, .13f, .18f, .94f)); tutorialBubble = box.gameObject;
            box.rectTransform.anchorMin = box.rectTransform.anchorMax = new Vector2(.5f, 1); box.rectTransform.pivot = new Vector2(.5f, 1);
            box.rectTransform.anchoredPosition = new Vector2(0, -78); box.rectTransform.sizeDelta = new Vector2(1100, 110); Layout(box.transform, 6);
            box.GetComponent<VerticalLayoutGroup>().spacing = 4;
            instruction = Label(box.transform, null, 44, 16); hint = Label(box.transform, null, 20, 14);
            var row = Row(box.transform, 26); Button(row, "tutorial.retry", () => game.Tutorial?.Retry(), 26);
            skipDodge = Button(row, "tutorial.skipDodge", () => game.Tutorial?.SkipDodge(), 26); Button(row, "tutorial.skipAll", () => game.Tutorial?.SkipAll(), 26);
        }
        void BuildTrainingBar()
        {
            var box = Box(game.Interface.transform, "Practice status", new Color(.06f, .13f, .18f, .96f)); trainingBar = box.gameObject;
            box.rectTransform.anchorMin = box.rectTransform.anchorMax = new Vector2(.5f, 0); box.rectTransform.pivot = new Vector2(.5f, 0);
            box.rectTransform.anchoredPosition = new Vector2(0, 75); box.rectTransform.sizeDelta = new Vector2(1040, 110); Layout(box.transform, 6);
            box.GetComponent<VerticalLayoutGroup>().spacing = 2;
            trainingStatus = Label(box.transform, null, 20, 14); activeStatus = Label(box.transform, null, 20, 14); liveDamage = Label(box.transform, null, 20, 14);
            var row = Row(box.transform, 28); Button(row, "zone.equipment", () => Open(PracticePanel.Equipment), 28);
            Button(row, "training.controls", () => Open(PracticePanel.Simulation), 28); Button(row, "training.reset", () => game.Training.Reset(), 28);
            Button(row, "button.menu", game.ReturnToMenu, 28);
        }
        void BuildZones()
        {
            zoneLabels = Rect(game.Interface.transform, "Training zone names").gameObject;
            var rect = (RectTransform)zoneLabels.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            string[] names = { "equipment", "range", "dodge", "simulation", "environment" };
            Vector2[] points = { new Vector2(-7.5f, -2.4f), new Vector2(-7, 5.6f), new Vector2(6, 5.6f), new Vector2(7, -1), new Vector2(-.8f, -2.4f) };
            for (int index = 0; index < names.Length; index++)
            {
                var label = Label(zoneLabels.transform, "zone." + names[index], 22, 16); label.color = new Color(.9f, .95f, .94f);
                label.rectTransform.anchorMin = label.rectTransform.anchorMax = Vector2.zero; label.rectTransform.sizeDelta = new Vector2(210, 22);
                zones.Add((label, points[index]));
            }
        }
        void BuildCompletion()
        {
            var panel = Panel("Tutorial complete", 760, 425, out completion); Label(panel, "tutorial.complete", 54, 27);
            Label(panel, "tutorial.completeBody", 81); summary = Label(panel, null, 37);
            Button(panel, "menu.start", () => { game.ReturnToMenu(); game.StartAdventure(); }, 42);
            Button(panel, "menu.training", () => { game.ReturnToMenu(); game.StartTraining(); }, 42); Button(panel, "button.menu", game.ReturnToMenu, 42);
        }
        void BuildIntroduction()
        {
            var panel = Panel("Choose first activity", 730, 360, out introduction);
            Label(panel, "intro.title", 60, 27); Label(panel, "intro.body", 85);
            Button(panel, "menu.tutorial", () => game.StartTutorial(), 45); Button(panel, "intro.adventure", () => { game.SaveTutorialMark(false, true); game.StartAdventure(); }, 45);
        }
        public void ShowIntroduction() { showIntroduction = true; RefreshNow(); }
        public void HideIntroduction() { showIntroduction = false; RefreshNow(); }
        public void Open(PracticePanel panel)
        {
            if (game.Training == null) return; Close(); current = panel; game.SetPause(PauseReason.PracticePanel, true);
            content = Panel("Training console", 1100, 650, out trainingPanel);
            Label(content, "zone." + panel.ToString().ToLowerInvariant(), 35, 24);
            equipmentStatus = Label(content, null, 48, 17);
            if (panel == PracticePanel.Equipment) BuildEquipment();
            else if (panel == PracticePanel.Range)
            {
                Label(content, "training.dpsRule", 90); description = Label(content, null, 45, 23);
                Button(content, "training.resetStats", game.ResetPracticeStats); Label(content, "training.targetKinds", 65);
            }
            else if (panel == PracticePanel.Dodge)
            {
                Label(content, "training.hazardRule", 78);
                var row = Row(content); for (int strength = 1; strength <= 3; strength++) { int level = strength; Button(row, "training.intensity." + strength, () => game.Hazards.Begin(level)); }
                Button(content, "training.hazardStop", () => { game.Hazards.Stop(); game.Projectiles.Clear(); });
            }
            else if (panel == PracticePanel.Simulation) BuildSimulation();
            else Label(content, "training.environmentPending", 130);
            var aids = Row(content); Button(aids, "training.toggleEnergy", () => game.Training.ToggleEnergy());
            Button(aids, "training.toggleCharges", () => game.Training.ToggleCharges()); Button(aids, "training.toggleInvincible", () => game.Training.ToggleInvincible());
            var reset = Row(content); Button(reset, "training.restore", () => game.Training.Restore()); Button(reset, "training.reset", () => game.Training.Reset());
            Button(reset, "training.defaults", () => game.Training.Reset(true));
            var language = Row(content); Button(language, "language.label", () => game.SetLanguage(game.Text.Language == "zh-CN" ? "en" : "zh-CN"));
            Button(language, "button.back", Close); RefreshNow();
        }
        void BuildEquipment()
        {
            foreach (ItemKind kind in Enum.GetValues(typeof(ItemKind)))
            {
                var row = Row(content, 38);
                foreach (var item in game.Catalog.items)
                {
                    if (item.kind != kind || !item.implemented) continue;
                    string id = item.id; var button = Button(row, item.nameKey, () => { selectedItem = id; game.EquipItem(id); }, 38);
                    itemLabels[id] = button.GetComponentInChildren<Text>();
                }
            }
            description = Label(content, null, 52, 17);
            var remove = Row(content); foreach (var item in game.Catalog.items)
            {
                if (!item.implemented || item.kind != ItemKind.Passive) continue;
                string id = item.id; var button = Button(remove, "remove." + id, () => { selectedItem = id; game.Loadout.RemovePassive(id); game.EquipmentChanged(); });
            }
            Label(content, "training.futureItems", 60, 15);
        }
        void BuildSimulation()
        {
            Label(content, "training.simulationRule", 70);
            var count = Row(content); foreach (int number in new[] { 3, 6, 12 })
            { int value = number; Button(count, "training.count." + number, () => game.Training.SetCount(value)); }
            var kinds = Row(content); foreach (var type in new[] { "chaser", "shooter", "elite", "mix" })
            { string id = type; Button(kinds, "enemy." + type, () => game.Training.Simulate(id)); }
            Button(content, "training.stop", () => game.Training.StopSimulation());
            var row = Row(content); Button(row, "zone.range", () => Open(PracticePanel.Range)); Button(row, "zone.dodge", () => Open(PracticePanel.Dodge));
        }
        public void Close()
        {
            current = PracticePanel.None;
            if (trainingPanel != null)
            {
                labels.RemoveAll(pair => pair.view == null || pair.view.transform.IsChildOf(trainingPanel.transform)); itemLabels.Clear();
                trainingPanel.SetActive(false); Destroy(trainingPanel); trainingPanel = null; equipmentStatus = description = null;
            }
            if (game != null) game.SetPause(PauseReason.PracticePanel, false);
        }
        void Update() { refreshLeft -= Time.unscaledDeltaTime; if (refreshLeft <= 0) { refreshLeft = .1f; RefreshNow(); } }
        public void RefreshNow()
        {
            if (game == null || tutorialBubble == null) return;
            foreach (var label in labels) if (label.view != null) label.view.text = game.Text.Get(label.key);
            bool tutorial = game.Tutorial != null, training = game.Training != null;
            zoneLabels.SetActive(training && game.CanAct);
            foreach (var zone in zones) zone.view.rectTransform.anchoredPosition = (Vector2)game.GameCamera.WorldToScreenPoint(zone.point) / game.Interface.Canvas.scaleFactor;
            introduction.SetActive(showIntroduction && game.Context == null && !game.Interface.FirstLaunch);
            tutorialBubble.SetActive(tutorial && game.CanAct);
            trainingBar.SetActive(training && !game.Pause.Has(PauseReason.Menu) && !game.Pause.Has(PauseReason.Map) && !game.Interface.SettingsOpen && !IsOpen);
            completion.SetActive(tutorial && game.Context.Phase == RunPhase.Complete);
            if (tutorial)
            {
                instruction.text = game.Text.Get(game.Tutorial.ObjectiveKey);
                hint.text = game.Tutorial.Step == TutorialStep.MapBeacon && game.Tutorial.PassiveComparison != null ? game.Tutorial.PassiveComparison : game.Text.Get(game.Tutorial.StrengthenedHint ? "tutorial.strongHint" : "tutorial.safeHint");
                skipDodge.interactable = game.Tutorial.Step == TutorialStep.Dodge && game.Tutorial.StrengthenedHint;
                summary.text = game.Text.Get(game.Tutorial.StepSkipped ? "tutorial.completedWithSkip" : "tutorial.completedAll");
            }
            if (!training) return;
            string status = game.Text.Get("training.aids", ("energy", Flag(game.Loadout.InfiniteEnergy)), ("charges", Flag(game.Loadout.InfiniteCharges)), ("invincible", Flag(game.Loadout.Invincible)));
            trainingStatus.text = status;
            string activeItem = game.Loadout.Active == null ? game.Text.Get("active.none") : game.Text.Get(game.Catalog.Find(game.Loadout.Active).nameKey);
            activeStatus.text = game.Text.Get("training.active", ("item", activeItem), ("charges", game.Loadout.Charges.ToString()), ("cooldown", game.Loadout.ActiveCooldown.ToString("0.0")), ("shield", game.Loadout.ShieldLeft.ToString("0.0")));
            liveDamage.text = DamageText();
            if (equipmentStatus != null) equipmentStatus.text = status + "\n" + game.Text.Get("training.equipment", ("weapon", game.Text.Get(game.Catalog.Find(game.Loadout.Weapon).nameKey)), ("count", game.Loadout.Passives.Count.ToString()), ("simulation", game.Training.SimulationCount.ToString()));
            if (description != null) description.text = current == PracticePanel.Range ? DamageText() : game.Catalog.Describe(game.Text, game.Catalog.Find(selectedItem));
            foreach (var pair in itemLabels)
            {
                var item = game.Catalog.Find(pair.Key); string name = game.Text.Get(item.nameKey);
                pair.Value.text = item.kind == ItemKind.Passive ? game.Text.Get("training.layers", ("name", name), ("layers", game.Loadout.Layers(item.id).ToString())) : name;
            }
        }
        string Flag(bool enabled) => game.Text.Get(enabled ? "flag.on" : "flag.off");
        string DamageText() => game.Text.Get("training.damage", ("last", game.PracticeStats.Last.ToString("0.0")), ("total", game.PracticeStats.Total.ToString("0.0")), ("dps", game.PracticeStats.Dps(game.EffectivePracticeTime).ToString("0.0")));
        void OnDestroy() { if (game != null && game.Text != null) game.Text.Changed -= RefreshNow; }
    }
}
