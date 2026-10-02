using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Starfall
{
    public sealed class AdventureInterface : MonoBehaviour
    {
        StarfallGame game;
        GameObject records, resultPanel, banner, route;
        Text timer, bossTitle, roomTitle, title, body, times, saved, metadata, rewardStatus, rules, recordRows, routeText;
        Image bossFill;
        readonly List<(Text text, string key)> labels = new List<(Text, string)>();
        readonly List<Button> rewardButtons = new List<Button>();
        Button retry;
        Button retryPending;
        Outline resultOutline;
        ClearResult animatedResult;
        float revealStart;
        bool recordsOpen;
        float refresh;
        public bool RecordsOpen => recordsOpen;
        public void Initialize(StarfallGame owner)
        {
            game = owner;
            var strip = Box(game.Interface.transform, "Adventure title", new Color(.04f, .09f, .08f, .96f)); banner = strip.gameObject;
            strip.rectTransform.anchorMin = strip.rectTransform.anchorMax = new Vector2(.5f, 1); strip.rectTransform.pivot = new Vector2(.5f, 1);
            strip.rectTransform.anchoredPosition = new Vector2(0, -79); strip.rectTransform.sizeDelta = new Vector2(1060, 85);
            var layout = strip.gameObject.AddComponent<VerticalLayoutGroup>(); layout.padding = new RectOffset(8, 8, 4, 4); layout.childControlHeight = layout.childControlWidth = true; layout.childForceExpandHeight = false;
            roomTitle = Label(strip.transform, null, 26, 20); timer = Label(strip.transform, null, 24, 16); bossTitle = Label(strip.transform, null, 23, 16);
            var track = Box(strip.transform, "Captain health", new Color(.12f, .2f, .18f)); track.gameObject.AddComponent<LayoutElement>().preferredHeight = 5;
            bossFill = Box(track.transform, "Health fill", PrototypeVisuals.Enemy); Stretch(bossFill.rectTransform);
            var mapRoute = Box(game.Interface.transform, "Level route", new Color(.05f, .12f, .1f, .95f)); route = mapRoute.gameObject;
            mapRoute.rectTransform.anchorMin = mapRoute.rectTransform.anchorMax = new Vector2(.5f, 1); mapRoute.rectTransform.pivot = new Vector2(.5f, 1);
            mapRoute.rectTransform.anchoredPosition = new Vector2(0, -85); mapRoute.rectTransform.sizeDelta = new Vector2(1100, 100);
            routeText = Label(mapRoute.transform, null, 100, 16); Stretch(routeText.rectTransform);
            var panel = Panel("Personal bests", 1100, 660, out records); Label(panel, "records.title", 38, 25);
            rules = Label(panel, "records.rules", 103, 16);
            var viewport = Box(panel, "Records scroll", new Color(.05f, .11f, .14f), true); viewport.gameObject.AddComponent<LayoutElement>().preferredHeight = 330;
            viewport.gameObject.AddComponent<RectMask2D>(); var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            recordRows = Label(viewport.transform, null, 330, 17); recordRows.alignment = TextAnchor.UpperCenter;
            Destroy(recordRows.GetComponent<LayoutElement>());
            recordRows.rectTransform.anchorMin = new Vector2(0, 1); recordRows.rectTransform.anchorMax = Vector2.one; recordRows.rectTransform.pivot = new Vector2(.5f, 1);
            recordRows.rectTransform.sizeDelta = new Vector2(-20, 330);
            scroll.viewport = viewport.rectTransform; scroll.content = recordRows.rectTransform;
            Label(panel, "records.scope", 45, 15); retryPending = Button(panel, "records.pending", game.RetryRecordSaves, 32); Button(panel, "button.back", CloseRecords, 38);
            panel = Panel("Adventure result", 1000, 660, out resultPanel);
            resultOutline = panel.gameObject.AddComponent<Outline>(); resultOutline.effectDistance = new Vector2(1, -1);
            title = Label(panel, null, 42, 26); body = Label(panel, null, 58, 19); times = Label(panel, null, 58, 19);
            saved = Label(panel, null, 32, 16); metadata = Label(panel, null, 44, 16); rewardStatus = Label(panel, null, 31, 17);
            var row = Row(panel, 112); for (int i = 0; i < 3; i++) { int at = i; var choice = Button(row, null, () => { if (game.Adventure.Rewards.Count > at) game.Adventure.SelectReward(game.Adventure.Rewards[at]); }, 112); choice.GetComponentInChildren<Text>().fontSize = 16; rewardButtons.Add(choice); }
            retry = Button(panel, "records.retrySave", game.RetryRecordSaves, 33);
            Label(panel, "adventure.nextStagePending", 32, 16);
            row = Row(panel, 38); Button(row, "adventure.retryRun", game.StartAdventure, 38); Button(row, "records.title", OpenRecords, 38); Button(row, "button.menu", game.ReturnToMenu, 38);
            game.Text.Changed += RefreshNow; RefreshNow();
        }
        RectTransform Rect(Transform parent, string name)
        {
            var rect = new GameObject(name ?? "Adventure label", typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); return rect;
        }
        static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        Image Box(Transform parent, string name, Color color, bool hit = false)
        {
            var rect = Rect(parent, name); var box = rect.gameObject.AddComponent<Image>(); box.color = color; box.raycastTarget = hit; return box;
        }
        Text Label(Transform parent, string key, float height, int size)
        {
            var rect = Rect(parent, key); var text = rect.gameObject.AddComponent<Text>(); text.font = game.Interface.SharedFont; text.fontSize = size; text.color = new Color(.9f, .95f, .91f);
            text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false; text.supportRichText = false; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow;
            var element = rect.gameObject.AddComponent<LayoutElement>(); element.preferredHeight = element.minHeight = height;
            if (key != null) labels.Add((text, key)); return text;
        }
        Transform Panel(string name, float width, float height, out GameObject root)
        {
            var overlay = Box(game.Interface.transform, name, new Color(.015f, .03f, .04f, .9f), true); Stretch(overlay.rectTransform); root = overlay.gameObject;
            var box = Box(overlay.transform, name + " body", new Color(.07f, .14f, .17f), true); box.rectTransform.sizeDelta = new Vector2(width, height);
            var group = box.gameObject.AddComponent<VerticalLayoutGroup>(); group.padding = new RectOffset(18, 18, 14, 14); group.spacing = 5;
            group.childControlHeight = group.childControlWidth = true; group.childForceExpandHeight = false; return box.transform;
        }
        Transform Row(Transform parent, float height)
        {
            var row = Rect(parent, "Choices"); row.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            var group = row.gameObject.AddComponent<HorizontalLayoutGroup>(); group.spacing = 8; group.childControlHeight = group.childControlWidth = true; group.childForceExpandHeight = false; group.childForceExpandWidth = true; return row;
        }
        Button Button(Transform parent, string key, Action action, float height)
        {
            var box = Box(parent, key, new Color(.16f, .32f, .35f), true); var element = box.gameObject.AddComponent<LayoutElement>(); element.preferredHeight = element.minHeight = height;
            var button = box.gameObject.AddComponent<Button>(); button.targetGraphic = box; var nav = button.navigation; nav.mode = Navigation.Mode.None; button.navigation = nav;
            button.onClick.AddListener(() => { game.Audio?.Play(GameSound.Ui); action(); RefreshNow(); }); var label = Label(box.transform, key, height, 18); Stretch(label.rectTransform); return button;
        }
        public void OpenRecords() { recordsOpen = true; RefreshNow(); records.transform.SetAsLastSibling(); }
        public void CloseRecords() { recordsOpen = false; RefreshNow(); }
        void Update() { refresh -= Time.unscaledDeltaTime; if (refresh <= 0) { refresh = .1f; RefreshNow(); } }
        public void RefreshNow()
        {
            if (game == null || records == null) return;
            foreach (var binding in labels) binding.text.text = game.Text.Get(binding.key, ("timing", game.LevelConfig.timingVersion), ("balance", game.LevelConfig.balanceVersion));
            var adventure = game.Adventure; bool showingResult = adventure?.Result != null;
            records.SetActive(recordsOpen && !game.Interface.SettingsOpen);
            retryPending.gameObject.SetActive(game.PendingRecords.Count > 0);
            resultPanel.SetActive(showingResult && !recordsOpen && !game.Interface.SettingsOpen);
            banner.SetActive(adventure != null && game.CanAct);
            route.SetActive(adventure != null && game.Pause.Has(PauseReason.Map) && !game.Interface.SettingsOpen);
            if (route.activeSelf)
            {
                route.transform.SetAsLastSibling(); var text = new System.Text.StringBuilder();
                foreach (var room in adventure.Plan.Rooms) text.Append(game.Text.Get("room." + room.Id)).Append(room.Id == adventure.Current.Id ? " ◆" : adventure.Progress.IsClear(room.Id) ? " ✓" : " ·").Append("   ");
                routeText.text = game.Text.Get("adventure.route", ("rooms", text.ToString()), ("count", adventure.Progress.Beacons.ToString()));
            }
            if (recordsOpen)
            {
                var text = new System.Text.StringBuilder(); string[] stages = { "gardens", "workshop", "reservoir", "greenhouse", "hub", "sanctum" };
                foreach (string stage in stages)
                {
                    var best = CurrentBest(stage); text.Append(game.Text.Get("stage." + stage)).Append(" — ");
                    if (best == null) text.Append(game.Text.Get("records.none"));
                    else text.Append(LevelTimer.Format(best.milliseconds)).Append("\n").Append(RecordMetadata(best));
                    text.Append("\n\n");
                }
                foreach (var best in game.Records.Book.bests) if (best.timingVersion != game.LevelConfig.timingVersion || best.balanceVersion != game.LevelConfig.balanceVersion)
                    text.Append(game.Text.Get("stage." + best.stageId)).Append(" — ").Append(game.Text.Get("records.archived", ("time", LevelTimer.Format(best.milliseconds)), ("rules", best.timingVersion + "/" + best.balanceVersion))).Append("\n").Append(RecordMetadata(best)).Append("\n");
                recordRows.text = text.ToString().Trim();
                recordRows.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(330, recordRows.preferredHeight + 12));
            }
            if (adventure == null) return;
            roomTitle.text = game.Text.Get("adventure.roomTitle", ("room", game.Text.Get("room." + adventure.Current.Id)), ("count", adventure.Progress.Beacons.ToString()));
            timer.text = game.Settings.hideTimer ? "" : game.Text.Get("adventure.timer", ("time", LevelTimer.Format(adventure.Timer.Milliseconds)), ("best", CurrentBest("gardens")?.milliseconds is long bestTime ? LevelTimer.Format(bestTime) : "—"));
            bool boss = adventure.Boss != null && adventure.Boss.Health.State.Alive;
            bossFill.transform.parent.gameObject.SetActive(boss);
            bossTitle.text = boss ? game.Text.Get("boss.caption", ("move", game.Text.Get("boss.move." + adventure.Boss.Move.ToString().ToLowerInvariant()))) : game.Text.Get(adventure.Current.Kind == LevelRoomKind.Safe ? "adventure.safeHint" : adventure.Current.Kind == LevelRoomKind.Challenge ? "adventure.challengeTime" : adventure.Current.Kind == LevelRoomKind.Mechanism ? "adventure.mechanismHint" : "adventure.routeHint",
                ("seconds", Mathf.Max(0, game.LevelConfig.challengeSeconds - (float)adventure.ChallengeElapsed).ToString("0")));
            if (boss) bossFill.rectTransform.anchorMax = new Vector2(adventure.Boss.Health.State.Health / adventure.Boss.Health.State.Maximum, 1);
            if (!showingResult) return;
            var result = adventure.Result; string key = "feedback." + result.Feedback.ToString().ToLowerInvariant();
            if (animatedResult != result) { animatedResult = result; revealStart = Time.unscaledTime; }
            float reveal = game.Settings.reduceFlash ? 1 : Mathf.Clamp01((Time.unscaledTime - revealStart) / .25f);
            resultOutline.effectColor = new Color(.65f, .7f, .42f, .3f + reveal * .4f);
            title.color = Color.Lerp(PrototypeVisuals.Gold, new Color(.9f, .95f, .91f), reveal);
            title.text = game.Text.Get(key + ".title"); body.text = game.Text.Get(key + ".body", ("time", LevelTimer.Format(result.Attempt.milliseconds)), ("delta", LevelTimer.Format(result.Delta)));
            times.text = result.Success ? game.Text.Get("result.times", ("time", LevelTimer.Format(result.Attempt.milliseconds)), ("best", result.Previous.HasValue ? LevelTimer.Format(result.Previous.Value) : "—"),
                ("comparison", result.Previous.HasValue && result.Eligible ? game.Text.Get(result.Attempt.milliseconds < result.Previous.Value ? "result.faster" : result.Attempt.milliseconds == result.Previous.Value ? "result.equal" : "result.slower", ("delta", LevelTimer.Format(result.Delta))) : "—")) : game.Text.Get("result.failureTime", ("time", LevelTimer.Format(result.Attempt.milliseconds)));
            saved.text = game.Text.Get(!result.Success ? "records.failureExcluded" : !result.Eligible ? "result.practice" : result.Saved ? "records.saved" : "records.saveFailed");
            metadata.text = RecordMetadata(result.Attempt);
            rewardStatus.text = result.Success ? game.Text.Get(adventure.RewardSelected ? "reward.selected" : "reward.choose", ("item", adventure.SelectedReward == null ? "" : RewardName(adventure.SelectedReward))) : "";
            foreach (var button in rewardButtons) button.transform.parent.gameObject.SetActive(result.Success);
            for (int i = 0; i < rewardButtons.Count; i++)
            {
                rewardButtons[i].interactable = result.Success && !adventure.RewardSelected;
                var item = adventure.Rewards.Count > i ? game.Catalog.Find(adventure.Rewards[i]) : null;
                rewardButtons[i].GetComponentInChildren<Text>().text = adventure.Rewards.Count > i ? RewardName(adventure.Rewards[i]) + (item == null ? "" : "\n" + game.Catalog.Describe(game.Text, item)) : "";
            }
            retry.gameObject.SetActive(result.Success && result.Eligible && !result.Saved);
            resultPanel.transform.SetAsLastSibling();
        }
        string RewardName(string id) => game.Catalog.Find(id) != null ? game.Text.Get(game.Catalog.Find(id).nameKey) : game.Text.Get("reward." + id);
        BestRecord CurrentBest(string stage) => game.Records.Find(stage, game.LevelConfig.timingVersion, game.LevelConfig.balanceVersion);
        string RecordMetadata(BestRecord best)
        {
            var names = new List<string>();
            foreach (var entry in best.equipment)
            {
                string[] parts = entry.Split(':'); var item = game.Catalog.Find(parts[0]);
                if (item != null) names.Add(parts.Length > 1 ? game.Text.Get("training.layers", ("name", game.Text.Get(item.nameKey)), ("layers", parts[1])) : game.Text.Get(item.nameKey));
            }
            return game.Text.Get("records.metadata", ("seed", best.seed.ToString()), ("date", best.date), ("gear", string.Join(" / ", names)), ("rules", best.difficulty + "/" + best.timingVersion + "/" + best.balanceVersion));
        }
        void OnDestroy() { if (game?.Text != null) game.Text.Changed -= RefreshNow; }
    }
}
