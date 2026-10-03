from pathlib import Path
import json
root=Path(__file__).resolve().parents[1]
extra={
 'm5.trainingMenu':('训练营','Training'), 'm5.tutorialMenu':('新手教学','Tutorial'),
 'tutorial.energyDemo':('能量已用尽：按 1 换回无限弹药手枪。','Energy depleted: press 1 to return to the unlimited pistol.'),
 'hud.controls':('WASD 移动 · 鼠标瞄准 · 左键攻击 · Space 闪避 · 1/2 武器 · Q 主动 · E 拾取 · F 交互 · Tab 地图','WASD move · Mouse aim · LMB fire · Space dodge · 1/2 weapons · Q active · E pickup · F interact · Tab map'),
 'hud.interact':('F · 使用出口','F · Use exit')
}
for lang,col in [('zh-CN',0),('en',1)]:
 p=root/'Assets/_Game/Resources/Localization'/f'{lang}.json'; d=json.loads(p.read_text()); table={x['key']:x for x in d['entries']}
 for key,v in extra.items():
  if key in table:table[key]['value']=v[col]
  else:d['entries'].append({'key':key,'value':v[col]})
 for entry in d['entries']:
  if entry['key'].startswith(('adventure.','station.','route.','shop.','map.','checkpoint.entry','prepare.')):
   entry['value']=entry['value'].replace('[E]','[F]').replace(' E ',' F ').replace('E：','F：').replace('E —','F —').replace('按 E','按 F').replace('E 激活','F 激活')
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2),encoding='utf-8')
print('M5 control copy completed')
