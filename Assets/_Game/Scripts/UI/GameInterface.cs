using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Text = TMPro.TextMeshProUGUI;

namespace Starfall
{
    public sealed class GameInterface : MonoBehaviour
    {
        sealed class Binding { public Text View; public string Key; }
        readonly List<Binding> bindings = new List<Binding>();
        readonly List<GameObject> overlays = new List<GameObject>();
        readonly List<(Image view, string language)> languageChoices = new List<(Image, string)>();
        StarfallGame game;
        TMP_FontAsset font;
        GameObject menu, first, settings, pause, map, death, complete, hud, confirm;
        Button continueButton;
        Text checkpointHint;
        Text health, coins, dodge, objective, notice, interact, saveError, displayMode, deathStats, clearStats, pauseTitle;
        Image healthFill, healthLag, weaponIcon, activeIcon;
        float lagRatio=1; ExplorerController lagPlayer;
        Text minimalSetting, vignetteSetting, numbersSetting;
        RectTransform mapPlayer;
        float refreshLeft;
        public bool FirstLaunch { get; private set; }
        public bool SettingsOpen { get; private set; }
        public Canvas Canvas { get; private set; }
        public int EscapeConsumedFrame { get; set; } = -1;
        public TMP_FontAsset SharedFont => font;
        Text weaponLabel, activeLabel, flashSetting, timerSetting;
        public void Initialize(StarfallGame owner)
        {
            game = owner; FirstLaunch = !game.Settings.languageSelected;
            font = M5Art.Catalog.body;
            Canvas = gameObject.AddComponent<Canvas>(); Canvas.renderMode = RenderMode.ScreenSpaceOverlay; Canvas.sortingOrder = 100;
            var scaler = gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            gameObject.AddComponent<GraphicRaycaster>();
            BuildHud(); BuildMenu(); BuildFirst(); BuildSettings(); BuildPause(); BuildMap(); BuildResults();
            var confirmPanel = Panel("New run confirmation", 700, 330, out confirm);
            Label(confirmPanel, "checkpoint.overwriteTitle", 26, 45); Label(confirmPanel, "checkpoint.overwriteBody", 19, 100);
            Button(confirmPanel, "checkpoint.confirm", game.ConfirmNewRun, true, true, 40); Button(confirmPanel, "button.back", game.CancelNewRun, true, false, 40);
            saveError = FixedText(transform, "Save status", new Vector2(.5f, 0), new Vector2(0, 12), new Vector2(1050, 42), 16, TextAlignmentOptions.Center);
            saveError.color = PrototypeVisuals.Gold;
            game.Text.Changed += RefreshLanguage; RefreshLanguage(); Refresh();
        }
        RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name ?? "Dynamic label", typeof(RectTransform)); var rect = (RectTransform)go.transform; rect.SetParent(parent, false); return rect;
        }
        static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        Image Image(Transform parent, string name, Color color)
        {
            var rect = Rect(parent, name); var view = rect.gameObject.AddComponent<Image>(); view.color = color; view.raycastTarget = false; return view;
        }
        Transform Panel(string name, float width, float height, out GameObject overlay)
        {
            var dim = Image(transform, name + " Overlay", new Color(.02f, .035f, .06f, .86f)); Stretch(dim.rectTransform); dim.raycastTarget = true;
            overlay = dim.gameObject; overlays.Add(overlay);
            var panel = Image(dim.transform, name, new Color(.075f, .13f, .18f, .98f)); panel.rectTransform.sizeDelta = new Vector2(width, height);
            panel.rectTransform.anchorMin = panel.rectTransform.anchorMax = new Vector2(.5f, .5f); panel.raycastTarget = true;
            var outline = panel.gameObject.AddComponent<Outline>(); outline.effectColor = new Color(.45f, .57f, .52f, .6f); outline.effectDistance = new Vector2(1, -1);
            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>(); layout.padding = new RectOffset(26, 26, 22, 22);
            layout.spacing = 7; layout.childAlignment = TextAnchor.UpperCenter; layout.childControlHeight = true; layout.childControlWidth = true;
            layout.childForceExpandHeight = false; layout.childForceExpandWidth = true;
            return panel.transform;
        }
        Text Label(Transform parent, string key, int size = 20, float height = 30, Color? color = null)
        {
            var rect = Rect(parent, key); var view = rect.gameObject.AddComponent<Text>(); ConfigureText(view, size, TextAlignmentOptions.Center);
            view.color = color ?? new Color(.87f, .92f, .9f);
            var element = rect.gameObject.AddComponent<LayoutElement>(); element.preferredHeight = height; element.minHeight = height;
            if (key != null) bindings.Add(new Binding { View = view, Key = key }); return view;
        }
        void ConfigureText(Text view, int size, TextAlignmentOptions align)
        {
            view.font = font; view.fontSize = Mathf.Max(16, size); if (size >= 25) view.font = M5Art.Catalog.title; view.alignment = align; view.raycastTarget = false; view.richText = false;
            view.textWrappingMode = TextWrappingModes.Normal; view.overflowMode = TextOverflowModes.Overflow;
        }
        Text FixedText(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, int fontSize, TextAlignmentOptions align)
        {
            var rect = Rect(parent, name); rect.anchorMin = rect.anchorMax = anchor; rect.pivot = anchor; rect.anchoredPosition = position; rect.sizeDelta = size;
            var view = rect.gameObject.AddComponent<Text>(); ConfigureText(view, fontSize, align); view.color = new Color(.9f, .94f, .94f); return view;
        }
        void Button(Transform parent, string key, Action action, bool enabled = true, bool accent = false, float height = 38)
        {
            var image = Image(parent, key, accent ? new Color(.22f, .42f, .4f) : new Color(.12f, .23f, .29f)); image.raycastTarget = true;
            var element = image.gameObject.AddComponent<LayoutElement>(); element.preferredHeight = element.minHeight = height;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.interactable = enabled;
            var colors = button.colors; colors.highlightedColor = new Color(.65f, .86f, .82f); colors.pressedColor = new Color(.46f, .68f, .62f);
            colors.disabledColor = new Color(.4f, .45f, .5f); button.colors = colors;
            var navigation = button.navigation; navigation.mode = Navigation.Mode.Automatic; button.navigation = navigation;
            if (action != null) button.onClick.AddListener(() => { game.Audio?.Play(GameSound.Ui); action(); });
            var text = Label(image.transform, key, 19, height); Stretch(text.rectTransform); text.color = enabled ? Color.white : new Color(.57f, .65f, .69f);
        }
        void LanguageButtons(Transform parent)
        {
            Label(parent, "language.label", 20, 30, PrototypeVisuals.Gold);
            var row = Rect(parent, "Language choices"); row.gameObject.AddComponent<LayoutElement>().preferredHeight = 42;
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>(); layout.spacing = 12; layout.childControlHeight = layout.childControlWidth = true;
            layout.childForceExpandHeight = layout.childForceExpandWidth = true;
            Button(row, null, () => game.SetLanguage("zh-CN"), true, game.Settings.language == "zh-CN", 42);
            row.GetChild(0).GetComponentInChildren<Text>().text = "简体中文";
            languageChoices.Add((row.GetChild(0).GetComponent<Image>(), "zh-CN"));
            Button(row, null, () => game.SetLanguage("en"), true, game.Settings.language == "en", 42);
            row.GetChild(1).GetComponentInChildren<Text>().text = "English";
            languageChoices.Add((row.GetChild(1).GetComponent<Image>(), "en"));
        }
        void BuildMenu()
        {
            menu = new GameObject("Legacy menu retired"); menu.transform.SetParent(transform, false); menu.SetActive(false);
            continueButton = menu.AddComponent<Button>(); checkpointHint = Label(menu.transform, null);
        }
        void BuildFirst()
        {
            var panel = Panel("Language Welcome", 650, 390, out first);
            Label(panel, "title", 29, 50, PrototypeVisuals.Gold); Label(panel, "language.welcome", 22, 44);
            LanguageButtons(panel); Label(panel, "language.firstHint", 18, 61);
            Button(panel, "language.confirm", game.ConfirmLanguage, true, true, 44);
        }
        void BuildSettings()
        {
            var panel = Panel("Settings", 900, 680, out settings);
            panel.GetComponent<VerticalLayoutGroup>().spacing = 5;
            Label(panel, "settings.title", 27, 40, PrototypeVisuals.Gold); LanguageButtons(panel);
            Button(panel, null, game.ToggleFullscreen, true, false, 34);
            displayMode = panel.GetChild(panel.childCount - 1).GetComponentInChildren<Text>();
            VolumeSlider(panel, "master", game.Settings.masterVolume); VolumeSlider(panel, "music", game.Settings.musicVolume); VolumeSlider(panel, "effects", game.Settings.effectsVolume); VolumeSlider(panel, "shake", game.Settings.shake);
            var options = Rect(panel, "Visual options row"); options.gameObject.AddComponent<LayoutElement>().preferredHeight = 36;
            var group = options.gameObject.AddComponent<HorizontalLayoutGroup>(); group.spacing = 12; group.childForceExpandWidth = true; group.childControlWidth = true; group.childControlHeight = true;
            Button(options, null, game.ToggleFlash, true, false, 36); flashSetting = options.GetChild(0).GetComponentInChildren<Text>();
            Button(options, null, game.ToggleTimer, true, false, 36); timerSetting = options.GetChild(1).GetComponentInChildren<Text>();
            options = Rect(panel, "Rendering options row"); options.gameObject.AddComponent<LayoutElement>().preferredHeight = 36;
            group = options.gameObject.AddComponent<HorizontalLayoutGroup>(); group.spacing = 12; group.childForceExpandWidth = true; group.childControlWidth = true; group.childControlHeight = true;
            Button(options, null, game.ToggleMinimal, true, false, 36); minimalSetting = options.GetChild(0).GetComponentInChildren<Text>();
            Button(options, null, game.ToggleVignette, true, false, 36); vignetteSetting = options.GetChild(1).GetComponentInChildren<Text>();
            options = Rect(panel, "Feedback options row"); options.gameObject.AddComponent<LayoutElement>().preferredHeight = 36;
            group = options.gameObject.AddComponent<HorizontalLayoutGroup>(); group.spacing = 12; group.childForceExpandWidth = true; group.childControlWidth = true; group.childControlHeight = true;
            Button(options, null, game.ToggleDamageNumbers, true, false, 36); numbersSetting = options.GetChild(0).GetComponentInChildren<Text>();
            Button(options, "m5.testSound", () => game.Audio.TestSound(), true, false, 36);
            Label(panel, "settings.note", 16, 40); Button(panel, "button.back", CloseSettings, true, true, 34);
        }
        void VolumeSlider(Transform parent, string channel, float value)
        {
            var row = Rect(parent, "settings." + channel); row.gameObject.AddComponent<LayoutElement>().preferredHeight = 34;
            var caption = Label(row, "settings." + channel, 18, 34); caption.rectTransform.anchorMin = Vector2.zero; caption.rectTransform.anchorMax = new Vector2(.4f, 1);
            caption.rectTransform.offsetMin = caption.rectTransform.offsetMax = Vector2.zero;
            var back = Image(row, "Slider track", new Color(.14f, .27f, .29f)); back.raycastTarget = true;
            back.rectTransform.anchorMin = new Vector2(.43f, .25f); back.rectTransform.anchorMax = new Vector2(.98f, .75f); back.rectTransform.offsetMin = back.rectTransform.offsetMax = Vector2.zero;
            var fill = Image(back.transform, "Slider fill", PrototypeVisuals.Teal); Stretch(fill.rectTransform);
            var handle = Image(back.transform, "Slider handle", PrototypeVisuals.Gold); handle.rectTransform.sizeDelta = new Vector2(15, 24); handle.raycastTarget = true;
            var slider = back.gameObject.AddComponent<Slider>(); slider.fillRect = fill.rectTransform; slider.handleRect = handle.rectTransform; slider.targetGraphic = handle;
            slider.value = value; var navigation = slider.navigation; navigation.mode = Navigation.Mode.Automatic; slider.navigation = navigation;
            slider.onValueChanged.AddListener(amount => game.SetVolume(channel, amount));
        }
        void BuildPause()
        {
            var panel = Panel("Pause", 660, 380, out pause);
            pauseTitle = Label(panel, null, 27, 56, PrototypeVisuals.Gold); Label(panel, "pause.note", 18, 52);
            Button(panel, "button.resume", game.Resume, true, true, 43);
            Button(panel, "menu.settings", OpenSettings); Button(panel, "button.restart", game.RestartMode); Button(panel, "button.menu", game.ReturnToMenu);
        }
        void BuildMap()
        {
            var panel = Panel("Map", 680, 535, out map); Label(panel, "map.title", 27, 48, PrototypeVisuals.Gold);
            var mapImage = Image(panel, "Map drawing", new Color(.1f, .2f, .24f)); mapImage.gameObject.AddComponent<LayoutElement>().preferredHeight = 265;
            var content = Rect(mapImage.transform, "Map geometry"); content.anchorMin = content.anchorMax = new Vector2(.5f, .5f); content.sizeDelta = new Vector2(552, 322);
            // Map scale is 21 pixels per world unit; vertical geometry fits its reserved area.
            content.localScale = new Vector3(.7f, .7f, 1);
            foreach (var rect in game.Room.Walls)
            {
                var wall = Image(content, "Map wall", new Color(.42f, .52f, .53f)); wall.rectTransform.anchoredPosition = rect.center * 23;
                wall.rectTransform.sizeDelta = rect.size * 23;
            }
            var entrance = Image(content, "Map entrance", PrototypeVisuals.Gold); entrance.rectTransform.anchoredPosition = game.Room.Spawn * 23; entrance.rectTransform.sizeDelta = Vector2.one * 15;
            var exit = Image(content, "Map exit", PrototypeVisuals.Teal); exit.rectTransform.anchoredPosition = game.Room.Exit * 23; exit.rectTransform.sizeDelta = new Vector2(12, 35);
            var dot = Image(content, "Map player", PrototypeVisuals.Teal); dot.sprite = PrototypeVisuals.Sprite("orb"); mapPlayer = dot.rectTransform; mapPlayer.sizeDelta = Vector2.one * 14;
            Label(panel, "map.legend", 18, 63); Button(panel, "map.close", () => game.SetPause(PauseReason.Map, false), true, true, 42);
        }
        void BuildResults()
        {
            var panel = Panel("Death", 730, 420, out death); Label(panel, "death.title", 26, 86, PrototypeVisuals.Gold);
            Label(panel, "death.body", 19, 72); deathStats = Label(panel, null, 19, 32);
            Button(panel, "button.restart", game.RestartMode, true, true, 44); Button(panel, "button.menu", game.ReturnToMenu, true, false, 42);
            panel = Panel("Complete", 730, 420, out complete); Label(panel, "clear.title", 28, 56, PrototypeVisuals.Gold);
            Label(panel, "clear.body", 19, 76); clearStats = Label(panel, null, 19, 32); Label(panel, "result.practice", 18, 30, PrototypeVisuals.Teal);
            Button(panel, "button.restart", game.RestartMode, true, true, 44); Button(panel, "button.menu", game.ReturnToMenu, true, false, 42);
        }
        void BuildHud()
        {
            var root = Rect(transform, "HUD"); Stretch(root); hud = root.gameObject;
            var left = Image(root, "Vital panel", M5UI.Ink); left.rectTransform.anchorMin = left.rectTransform.anchorMax = new Vector2(0,1); left.rectTransform.pivot = new Vector2(0,1); left.rectTransform.anchoredPosition = new Vector2(16,-16); left.rectTransform.sizeDelta = new Vector2(280,100);
            health = FixedText(root, "Health", new Vector2(0,1), new Vector2(32,-28), new Vector2(248,28), 20, TextAlignmentOptions.Left);
            var back = Image(root, "Health track", new Color(.16f,.23f,.23f)); back.rectTransform.anchorMin = back.rectTransform.anchorMax = new Vector2(0,1); back.rectTransform.pivot = new Vector2(0,1); back.rectTransform.anchoredPosition = new Vector2(32,-64); back.rectTransform.sizeDelta = new Vector2(248,8);
            healthLag = Image(back.transform,"Health loss",new Color(.82f,.53f,.34f)); Stretch(healthLag.rectTransform);
            healthFill = Image(back.transform, "Health fill", M5UI.Accent); Stretch(healthFill.rectTransform);
            dodge = FixedText(root, "Dodge", new Vector2(0,1), new Vector2(32,-82), new Vector2(248,26), 16, TextAlignmentOptions.Left);
            coins = FixedText(root, "Coins", Vector2.one, new Vector2(-24,-20), new Vector2(280,28), 19, TextAlignmentOptions.Right);
            objective = FixedText(root, "Objective", Vector2.one, new Vector2(-24,-54), new Vector2(284,76), 18, TextAlignmentOptions.TopRight); objective.color = PrototypeVisuals.Gold;
            var gear = Image(root, "Equipment dock", M5UI.Ink); gear.rectTransform.anchorMin = gear.rectTransform.anchorMax = Vector2.zero; gear.rectTransform.pivot = Vector2.zero; gear.rectTransform.anchoredPosition = new Vector2(16,16); gear.rectTransform.sizeDelta = new Vector2(320,130);
            weaponIcon = M5UI.Art(gear.transform,"Weapon icon",M5Art.Get("pistol"),new Vector2(-119,29),new Vector2(60,52));
            activeIcon = M5UI.Art(gear.transform,"Active icon",M5Art.Get("medkit"),new Vector2(-119,-31),new Vector2(52,48));
            weaponLabel = FixedText(root,"Weapon",Vector2.zero,new Vector2(98,90),new Vector2(222,56),18,TextAlignmentOptions.Left);
            activeLabel = FixedText(root,"Active item",Vector2.zero,new Vector2(98,29),new Vector2(222,48),16,TextAlignmentOptions.Left);
            notice = FixedText(root,"Notice",new Vector2(.5f,0),new Vector2(0,22),new Vector2(540,50),18,TextAlignmentOptions.Center); notice.color = PrototypeVisuals.Gold;
            interact = FixedText(root,"Interaction prompt",new Vector2(.5f,.5f),new Vector2(0,-210),new Vector2(530,64),21,TextAlignmentOptions.Center); interact.color = M5UI.Accent;
        }
        public void OpenSettings() { SettingsOpen = true; if (game.Context != null) game.SetPause(PauseReason.Settings, true); Refresh(); }
        public void CloseSettings() { SettingsOpen = false; if (game.Context != null) game.SetPause(PauseReason.Settings, false); Refresh(); }
        public void FinishFirstLaunch() { FirstLaunch = false; Refresh(); }
        public void ResetPanels()
        {
            SettingsOpen = false;
            RefreshMap();
        }
        public void RefreshMap()
        {
            if (map != null)
            {
                var old = map; overlays.Remove(old); bindings.RemoveAll(b => b.View == null || b.View.transform.IsChildOf(old.transform));
                old.SetActive(false); Destroy(old); BuildMap();
                foreach (var binding in bindings) if (binding.View != null) binding.View.text = game.Text.Get(binding.Key);
            }
            Refresh();
        }
        public void RefreshNow() => Refresh();
        void RefreshLanguage()
        {
            foreach (var binding in bindings) if (binding.View != null) binding.View.text = game.Text.Get(binding.Key);
            foreach (var choice in languageChoices) choice.view.color = choice.language == game.Text.Language ? new Color(.22f, .42f, .4f) : new Color(.12f, .23f, .29f);
            Refresh();
        }
        void Update() { if(game.Context==null && SettingsOpen && UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame==true) { EscapeConsumedFrame=Time.frameCount; CloseSettings(); } refreshLeft -= Time.unscaledDeltaTime; if (refreshLeft <= 0) { refreshLeft = .05f; Refresh(); } }
        void Refresh()
        {
            if (game == null || menu == null) return;
            var context = game.Context; bool playing = context != null;
            hud.SetActive(playing && (context.Phase == RunPhase.Combat || context.Phase == RunPhase.RoomClear));
            GameObject visible = null;
            if (FirstLaunch) visible = first;
            else if (SettingsOpen) visible = settings;
            else if (game.NewRunConfirmation) visible = confirm;
            else if (!playing && (game.FrontEnd == null || !game.FrontEnd.IsOpen) && (game.AdventureUI == null || !game.AdventureUI.RecordsOpen)) visible = menu;
            else if (context != null && context.Phase == RunPhase.Dead && game.Adventure == null) visible = death;
            else if (context != null && context.Phase == RunPhase.Complete && context.Mode != GameMode.Tutorial && game.Adventure == null) visible = complete;
            else if (game.Pause.Has(PauseReason.Focus) || game.Pause.Has(PauseReason.Menu)) visible = pause;
            else if (game.Pause.Has(PauseReason.Map)) visible = map;
            foreach (var overlay in overlays) overlay.SetActive(overlay == visible);
            if (visible != null) visible.transform.SetAsLastSibling();
            continueButton.interactable = game.Checkpoints.HasEntry;
            checkpointHint.text = game.Checkpoints.HasEntry ? game.Text.Get("checkpoint.menuHint", ("stage", game.Checkpoints.Current.stage.ToString())) : game.Text.Get("menu.noCheckpoint");
            if (displayMode != null) displayMode.text = game.Text.Get("settings.display", ("mode", game.Text.Get(game.Settings.fullscreen ? "settings.fullscreen" : "settings.windowed")));
            if (flashSetting != null) flashSetting.text = game.Text.Get("settings.flash", ("value", game.Text.Get(game.Settings.reduceFlash ? "flag.on" : "flag.off")));
            if (timerSetting != null) timerSetting.text = game.Text.Get("settings.timer", ("value", game.Text.Get(game.Settings.hideTimer ? "flag.off" : "flag.on")));
            if (pauseTitle != null) pauseTitle.text = game.Text.Get(game.Pause.Has(PauseReason.Focus) ? "pause.focus" : "pause.title");
            if (minimalSetting != null) minimalSetting.text = game.Text.Get("m5.settingsMinimal", ("state",game.Text.Get(game.Settings.minimalEffects?"flag.on":"flag.off")));
            if (vignetteSetting != null) vignetteSetting.text = game.Text.Get("m5.settingsVignette", ("state",game.Text.Get(game.Settings.vignette?"flag.on":"flag.off")));
            if (numbersSetting != null) numbersSetting.text = game.Text.Get("m5.settingsNumbers", ("state",game.Text.Get(game.Settings.damageNumbers?"flag.on":"flag.off")));
            string error = game.Profile.WriteProblem ? "save.failed" : game.CheckpointErrorKey ?? game.SaveErrorKey;
            if (saveError != null) { saveError.text = error == null ? "" : game.Text.Get(error); saveError.gameObject.SetActive(error != null); if (error != null) saveError.transform.SetAsLastSibling(); }
            if (!playing || game.Player == null) return;
            var state = game.Player.Health.State;
            health.text = game.Text.Get("hud.health", ("current", Mathf.CeilToInt(state.Health).ToString()), ("max", Mathf.CeilToInt(state.Maximum).ToString()));
            healthFill.rectTransform.anchorMax = new Vector2(state.Health / state.Maximum, 1);
            float ratio=state.Health/state.Maximum; if(lagPlayer!=game.Player || ratio>lagRatio) { lagPlayer=game.Player; lagRatio=ratio; } else if(game.CanAct) lagRatio=Mathf.MoveTowards(lagRatio,ratio,.045f);
            healthLag.rectTransform.anchorMax=new Vector2(lagRatio,1);
            coins.text = game.Text.Get("hud.coins", ("coins", context.Coins.ToString()));
            weaponIcon.sprite = M5Art.Get(game.Loadout.Weapon); activeIcon.sprite = M5Art.Get(game.Loadout.Active ?? "medkit"); activeIcon.color = game.Loadout.Active == null ? new Color(1,1,1,.25f) : Color.white;
            weaponLabel.text = game.Text.Get("hud.loadout", ("weapon", game.Text.Get(game.Catalog.Find(game.Loadout.Weapon).nameKey)), ("energy", game.Loadout.Energy.ToString("0")));
            activeLabel.text = game.Text.Get("hud.active", ("item", game.Loadout.Active == null ? game.Text.Get("active.none") : game.Text.Get(game.Catalog.Find(game.Loadout.Active).nameKey)),
                ("charges", game.Loadout.Charges.ToString()), ("cooldown", game.Loadout.ActiveCooldown.ToString("0.0")));
            dodge.text = game.Player.DodgeCooldown <= 0 ? game.Text.Get("hud.dodge.ready") : game.Text.Get("hud.dodge.wait", ("seconds", game.Player.DodgeCooldown.ToString("0.0", CultureInfo.InvariantCulture)));
            objective.text = context.Phase == RunPhase.RoomClear ? game.Text.Get("hud.exit") : game.Text.Get("hud.enemies", ("count", game.LivingEnemies.ToString()));
            if (context.Mode == GameMode.Tutorial) objective.text = game.Text.Get("tutorial.progress", ("step", ((int)game.Tutorial.Step + 1).ToString()));
            else if (context.Mode == GameMode.Training) objective.text = game.Text.Get("training.objective", ("count", game.LivingEnemies.ToString()));
            else if (game.Adventure != null) objective.text = game.Text.Get(game.Adventure.TaskKey, ("count", game.Adventure.Progress.Beacons.ToString()), ("enemies", game.LivingEnemies.ToString()));
            notice.text = game.Text.Get(game.NotificationKey ?? "hud.pickups");
            interact.text = game.CanAct && context.Phase == RunPhase.RoomClear && Vector2.Distance(game.Player.Body.position, game.Room.Exit) < 1.8f ? game.Text.Get("hud.interact") : "";
            if (game.CanAct) {
                string a = game.PickupInteraction == null ? "" : game.Text.Get("m5.pickup", ("item",game.Text.Get(game.PickupInteraction.NameKey)));
                string b = game.Interaction == null ? "" : "[F] " + game.Text.Get(game.Interaction.NameKey);
                if (a.Length > 0 || b.Length > 0) interact.text = a + (a.Length > 0 && b.Length > 0 ? "\n" : "") + b;
            }
            if (game.CanAct && game.PickupInteraction == null && game.Interaction == null && context.Mode != GameMode.Adventure && game.Room.DoorOpen && Vector2.Distance(game.Player.Body.position, game.Room.Exit) < 1.8f) interact.text = game.Text.Get("hud.interact");
            interact.rectTransform.anchoredPosition = new Vector2(0, context.Mode == GameMode.Training ? -145 : -210);
            mapPlayer.anchoredPosition = game.Player.Body.position * 23;
            string result = game.Text.Get("result.stats", ("kills", context.Kills.ToString()), ("coins", context.Coins.ToString()));
            deathStats.text = result; clearStats.text = result;
        }
        void OnDestroy() { if (game != null && game.Text != null) game.Text.Changed -= RefreshLanguage;  }
    }
}
