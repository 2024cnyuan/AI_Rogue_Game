from pathlib import Path
root=Path(__file__).resolve().parents[1]/'Assets/_Game/Scripts/UI'
p=root/'GameInterface.cs'; s=p.read_text(encoding='utf-8-sig')
s=s.replace('Image healthFill;', 'Image healthFill, weaponIcon, activeIcon;\n        Text minimalSetting, vignetteSetting, numbersSetting;')
s=s.replace('view.fontSize = size;', 'view.fontSize = Mathf.Max(16, size); if (size >= 25) view.font = M5Art.Catalog.title;')
s=s.replace('navigation.mode = Navigation.Mode.None','navigation.mode = Navigation.Mode.Automatic')
s=s.replace('Panel("Settings", 820, 660', 'Panel("Settings", 900, 680')
s=s.replace('Label(panel, "settings.note", 16, 44);', '''Button(panel, null, game.ToggleMinimal, true, false, 32); minimalSetting = panel.GetChild(panel.childCount - 1).GetComponentInChildren<Text>();
            Button(panel, null, game.ToggleVignette, true, false, 32); vignetteSetting = panel.GetChild(panel.childCount - 1).GetComponentInChildren<Text>();
            Button(panel, null, game.ToggleDamageNumbers, true, false, 32); numbersSetting = panel.GetChild(panel.childCount - 1).GetComponentInChildren<Text>();
            Button(panel, "m5.testSound", () => game.Audio.TestSound(), true, false, 36);
            Label(panel, "settings.note", 16, 40);''')
start=s.index('        void BuildHud()'); end=s.index('        public void OpenSettings()',start)
s=s[:start]+'''        void BuildHud()
        {
            var root = Rect(transform, "HUD"); Stretch(root); hud = root.gameObject;
            var left = Image(root, "Vital panel", M5UI.Ink); left.rectTransform.anchorMin = left.rectTransform.anchorMax = new Vector2(0,1); left.rectTransform.pivot = new Vector2(0,1); left.rectTransform.anchoredPosition = new Vector2(16,-16); left.rectTransform.sizeDelta = new Vector2(280,100);
            health = FixedText(root, "Health", new Vector2(0,1), new Vector2(32,-28), new Vector2(248,28), 20, TextAlignmentOptions.Left);
            var back = Image(root, "Health track", new Color(.16f,.23f,.23f)); back.rectTransform.anchorMin = back.rectTransform.anchorMax = new Vector2(0,1); back.rectTransform.pivot = new Vector2(0,1); back.rectTransform.anchoredPosition = new Vector2(32,-64); back.rectTransform.sizeDelta = new Vector2(248,8);
            healthFill = Image(back.transform, "Health fill", M5UI.Accent); Stretch(healthFill.rectTransform);
            dodge = FixedText(root, "Dodge", new Vector2(0,1), new Vector2(32,-82), new Vector2(248,26), 16, TextAlignmentOptions.Left);
            coins = FixedText(root, "Coins", Vector2.one, new Vector2(-24,-20), new Vector2(280,28), 19, TextAlignmentOptions.Right);
            objective = FixedText(root, "Objective", Vector2.one, new Vector2(-24,-54), new Vector2(284,76), 18, TextAlignmentOptions.TopRight); objective.color = PrototypeVisuals.Gold;
            var gear = Image(root, "Equipment dock", M5UI.Ink); gear.rectTransform.anchorMin = gear.rectTransform.anchorMax = Vector2.zero; gear.rectTransform.pivot = Vector2.zero; gear.rectTransform.anchoredPosition = new Vector2(16,16); gear.rectTransform.sizeDelta = new Vector2(320,130);
            weaponIcon = M5UI.Art(gear.transform,"Weapon icon",M5Art.Get("pistol"),new Vector2(-119,29),new Vector2(60,52));
            activeIcon = M5UI.Art(gear.transform,"Active icon",M5Art.Get("medkit"),new Vector2(-119,-31),new Vector2(52,48));
            weaponLabel = FixedText(root,"Weapon",Vector2.zero,new Vector2(98,90),new Vector2(222,48),18,TextAlignmentOptions.Left);
            activeLabel = FixedText(root,"Active item",Vector2.zero,new Vector2(98,29),new Vector2(222,48),16,TextAlignmentOptions.Left);
            notice = FixedText(root,"Notice",new Vector2(.5f,0),new Vector2(0,22),new Vector2(540,50),18,TextAlignmentOptions.Center); notice.color = PrototypeVisuals.Gold;
            interact = FixedText(root,"Interaction prompt",new Vector2(.5f,.5f),new Vector2(0,-210),new Vector2(530,64),21,TextAlignmentOptions.Center); interact.color = M5UI.Accent;
        }
''' + s[end:]
s=s.replace('string error = game.CheckpointErrorKey ?? game.SaveErrorKey;', '''if (minimalSetting != null) minimalSetting.text = game.Text.Get("m5.settingsMinimal", ("state",game.Text.Get(game.Settings.minimalEffects?"flag.on":"flag.off")));
            if (vignetteSetting != null) vignetteSetting.text = game.Text.Get("m5.settingsVignette", ("state",game.Text.Get(game.Settings.vignette?"flag.on":"flag.off")));
            if (numbersSetting != null) numbersSetting.text = game.Text.Get("m5.settingsNumbers", ("state",game.Text.Get(game.Settings.damageNumbers?"flag.on":"flag.off")));
            string error = game.Profile.WriteProblem ? "save.failed" : game.CheckpointErrorKey ?? game.SaveErrorKey;''')
s=s.replace('            weaponLabel.text =', '            weaponIcon.sprite = M5Art.Get(game.Loadout.Weapon); activeIcon.sprite = M5Art.Get(game.Loadout.Active ?? "medkit"); activeIcon.color = game.Loadout.Active == null ? new Color(1,1,1,.25f) : Color.white;\n            weaponLabel.text =')
s=s.replace('"[E] " + game.Text.Get("m5.pickup") + " " + game.Text.Get(game.PickupInteraction.NameKey)','game.Text.Get("m5.pickup", ("item",game.Text.Get(game.PickupInteraction.NameKey)))')
s=s.replace('context.Mode == GameMode.Training ? -155 : -235','context.Mode == GameMode.Training ? -145 : -210')
p.write_text(s,encoding='utf-8')
# Shared readable typography and keyboard navigation.
for name in ['PracticeInterface.cs','AdventureInterface.cs']:
    p=root/name; s=p.read_text(encoding='utf-8-sig').replace('text.fontSize = size;', 'text.fontSize = Mathf.Max(16,size); if(size >= 24) text.font = M5Art.Catalog.title;').replace('Navigation.Mode.None','Navigation.Mode.Automatic'); p.write_text(s,encoding='utf-8')
p=root/'AdventureInterface.cs'; s=p.read_text()
s=s.replace('new Vector2(0, -79)', 'new Vector2(0, -16)').replace('new Vector2(1060, 105)', 'new Vector2(640, 116)')
s=s.replace('roomTitle = Label(strip.transform, null, 26, 20); timer = Label(strip.transform, null, 24, 16); bossTitle = Label(strip.transform, null, 36, 15);', 'roomTitle = Label(strip.transform, null, 26, 20); timer = Label(strip.transform, null, 24, 16); bossTitle = Label(strip.transform, null, 52, 16);')
s=s.replace('Button(row, "adventure.retryRun", game.RequestAdventureStart, 46)', 'Button(row, "m5.retryStage", game.RestartMode, 46)').replace('"adventure.nextStage", () => game.Adventure.AdvanceStage()', '"m5.returnStages", () => game.Adventure.AdvanceStage()')
s=s.replace('Button(row, "button.menu", game.ReturnToMenu, 46)', 'Button(row, "m5.retryUnlock", () => game.Adventure.RetryUnlockSave(), 46)')
s=s.replace('            if (result.Success && adventure.Stage == 6) {', '            /* Independent stage result: no cumulative campaign time. */\n            if (false) {')
s=s.replace('rewardStatus.text = result.Success ? game.Text.Get(adventure.RewardSelected ? "reward.selected" : "reward.choose", ("item", adventure.SelectedReward == null ? "" : RewardName(adventure.SelectedReward))) : "";', 'rewardStatus.text = !result.Success ? "" : adventure.ResultErrorKey != null ? game.Text.Get(adventure.ResultErrorKey) : !result.Eligible ? game.Text.Get("result.practice") : game.Text.Get(adventure.RewardSelected ? "m5.rewardChosen" : "m5.rewardSaved", ("item", adventure.SelectedReward == null ? RewardName(StageRewards.Signature[adventure.Stage-1]) : RewardName(adventure.SelectedReward)));')
s=s.replace('rewardButtons[i].interactable = result.Success && !adventure.RewardSelected;', 'rewardButtons[i].gameObject.SetActive(i < adventure.Rewards.Count); rewardButtons[i].interactable = result.Success && !adventure.RewardSelected && adventure.ResultErrorKey == null;')
s=s.replace('nextStage.gameObject.SetActive(result.Success && adventure.Stage < 6); nextStage.interactable = adventure.RewardSelected;', 'nextStage.gameObject.SetActive(true); nextStage.interactable = adventure.ResultErrorKey == null;')
begin=s.index('            nextHint.text = game.Text.Get(adventure.ResultErrorKey'); end=s.index('\n',begin)
s=s[:begin]+'            nextHint.text = game.Text.Get(adventure.ResultErrorKey ?? (result.Success && !adventure.RewardSelected ? "m5.chooseReward" : "m5.allStagesOpen"));'+s[end:]
# Reward cards reserve top for real icons, body for descriptions.
s=s.replace('choice.GetComponentInChildren<Text>().fontSize = 16; rewardButtons.Add(choice);', 'var caption = choice.GetComponentInChildren<Text>(); caption.fontSize = 16; caption.rectTransform.anchorMax = new Vector2(1,.62f); M5UI.Art(choice.transform,"Reward icon",null,new Vector2(0,35),new Vector2(50,42)); rewardButtons.Add(choice);')
s=s.replace('                var item = adventure.Rewards.Count > i ?', '                var icon = rewardButtons[i].transform.Find("Reward icon").GetComponent<Image>(); icon.sprite = adventure.Rewards.Count > i ? M5Art.Get(adventure.Rewards[i]) : null;\n                var item = adventure.Rewards.Count > i ?')
p.write_text(s,encoding='utf-8')
print('M5 HUD, settings and results updated')
