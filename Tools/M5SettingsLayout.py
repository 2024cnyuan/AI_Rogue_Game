from pathlib import Path
p=Path(__file__).resolve().parents[1]/'Assets/_Game/Scripts/UI/GameInterface.cs'
s=p.read_text(encoding='utf-8-sig'); a=s.index('            Button(panel, null, game.ToggleFlash'); b=s.index('            Label(panel, "settings.note"',a)
s=s[:a]+'''            var options = Rect(panel, "Visual options row"); options.gameObject.AddComponent<LayoutElement>().preferredHeight = 36;
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
''' +s[b:]; p.write_text(s,encoding='utf-8')
# Calls for present-day independent record comparison are explicit.
for name in ['AdventureInterface.cs','FrontEndInterface.cs']:
 p=p.parent/name; s=p.read_text(); s=s.replace('game.LevelConfig.balanceVersion);','game.LevelConfig.balanceVersion,"stage_select");'); p.write_text(s,encoding='utf-8')
print('Compact settings and record grouping updated')
