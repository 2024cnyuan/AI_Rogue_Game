from pathlib import Path
import json
root=Path(__file__).resolve().parents[1]
p=root/'Assets/_Game/Scripts/UI/FrontEndInterface.cs'; s=p.read_text()
s=s.replace('T("title"), new Vector2(x, 225), new Vector2(420, 70), 46','T("title").Replace(" ","\\n"), new Vector2(x, 230), new Vector2(420, 112), 38')
s=s.replace('new Vector2(x, 168)', 'new Vector2(x, 147)')
s=s.replace('M5UI.Art(card.transform, "Stage artwork", M5Art.Catalog.stageCards[i], new Vector2(0, 29), new Vector2(cardWidth - 8, cardHeight - 74))', 'M5UI.CoverArt(card.transform, "Stage artwork", M5Art.Catalog.stageCards[i], new Vector2(0, 39), new Vector2(cardWidth - 12, cardHeight - 90))')
s=s.replace('stage + " · " + T("stage." + StageRewards.StageIds[i])','T("stage." + StageRewards.StageIds[i])')
s=s.replace('new Vector2(0, -cardHeight / 2 + 46), new Vector2(cardWidth - 28, 46), 21', 'new Vector2(0, -cardHeight / 2 + 51), new Vector2(cardWidth - 28, 58), 19')
s=s.replace('new Vector2(0, -cardHeight / 2 + 16)', 'new Vector2(0, -cardHeight / 2 + 12)')
s=s.replace('new Vector2(0, screen.y / 2 - 154), new Vector2(w - 40, 65), 32','new Vector2(0, screen.y / 2 - 157), new Vector2(w - 40, 88), 28')
s=s.replace('id == null ? T("m5.empty") : T("item." + id)', 'id == null ? T("m5.empty") : T("m5.item." + id)')
s=s.replace('new Vector2(90, 42), 16','new Vector2(90, 48), 16')
s=s.replace('T("kind." + k.ToString().ToLowerInvariant())','T("m5.kind." + k.ToString().ToLowerInvariant())')
s=s.replace('new Vector2(144 + n % 4 * 110, 162 - n / 4 * 100)', 'new Vector2(144 + n % 4 * 110, 162 - n / 4 * 120)')
s=s.replace('new Vector2(98, 92)', 'new Vector2(98, 112)')
s=s.replace('new Vector2(0, 10), new Vector2(62, 56)', 'new Vector2(0, 23), new Vector2(62, 48)')
s=s.replace('(owned ? "" : T("m5.locked") + " ") + T(item.nameKey), new Vector2(0, -30), new Vector2(90, 34)', 'T("m5.item."+id), new Vector2(0, -29), new Vector2(90, 52)')
s=s.replace('new Vector2(310, -190), new Vector2(442, 112)', 'new Vector2(310, -218), new Vector2(442, 105)')
s=s.replace('                M5UI.Text(b.transform, "Gear name",', '                if(!owned) M5UI.Text(b.transform,"Locked badge",T("m5.locked"),new Vector2(0,43),new Vector2(90,22),16,false,TextAlignmentOptions.Center);\n                M5UI.Text(b.transform, "Gear name",')
p.write_text(s,encoding='utf-8')
p=root/'Assets/_Game/Scripts/UI/GameInterface.cs'; s=p.read_text().replace('new Vector2(222,48),18','new Vector2(222,56),18').replace('new Vector2(222, 48),18','new Vector2(222, 56),18'); p.write_text(s,encoding='utf-8')
short={
'pistol':('手枪','Pistol'),'shotgun':('霰弹枪','Shotgun'),'smg':('冲锋枪','SMG'),'crossbow':('穿透弩','Crossbow'),'launcher':('发射器','Launcher'),'arc':('电弧枪','Arc gun'),'workshop_smg':('精制冲锋枪','Workshop SMG'),
'medkit':('急救包','Medkit'),'shield':('护盾','Shield'),'slow':('冰缓力场','Slow field'),'shock':('震荡器','Shock'),'decoy':('诱饵','Decoy'),'grenade':('手雷','Grenade'),
'rapid':('加速机芯','Rapid core'),'magnet':('磁环','Magnet'),'vitality':('生命核心','Vitality'),'agile':('助推靴','Agile boots'),'pierce':('穿透弹芯','Pierce'),'bounce':('反弹棱镜','Ricochet'),'critical':('精密瞄具','Critical'),'recharge':('回能电容','Recharge'),'lowhealth':('应急护甲','Last stand'),'blast':('扩散弹舱','Blast'),'controlled':('控制追踪','Control'),'flawless':('修复徽章','Flawless')}
for lang,col in [('zh-CN',0),('en',1)]:
 p=root/'Assets/_Game/Resources/Localization'/f'{lang}.json'; d=json.loads(p.read_text()); t={v['key']:v for v in d['entries']}
 values={**{'m5.item.'+key:v[col] for key,v in short.items()},'m5.equip':['装备','Equip'][col],'m5.kind.weapon':['武器','Weapons'][col],'m5.kind.active':['主动','Active'][col],'m5.kind.passive':['被动','Passives'][col]}
 for key,v in values.items():
  if key in t:t[key]['value']=v
  else:d['entries'].append({'key':key,'value':v})
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2),encoding='utf-8')
print('M5 readable card labels and spacing applied')
