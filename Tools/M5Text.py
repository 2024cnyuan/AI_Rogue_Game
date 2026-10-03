from pathlib import Path
import json
root=Path(__file__).resolve().parents[1]
pairs={
'm5.menuSubtitle':('星港远征 · 六处遗迹，任你挑战','A starport expedition · Six ruins await'),
'm5.play':('选择关卡','Choose stage'), 'm5.preparation':('出战整备','Preparation'),
'm5.collection':('永久装备','Equipment collection'), 'm5.selectStage':('远征地图','Expedition map'),
'm5.recommended':('推荐初次挑战','Recommended first'), 'm5.open':('可直接挑战','Ready to explore'),
'm5.cleared':('已通关','Cleared'), 'm5.pending':('待领奖','Reward pending'),
'm5.unlockReward':('通关永久解锁','Permanent clear reward'), 'm5.startStage':('开始本关','Start this stage'),
'm5.claimPending':('领取通关奖励','Claim clear reward'), 'm5.allStagesOpen':('六关均可直接挑战；通关可永久解锁装备。','All six stages are open. Clears permanently unlock equipment.'),
'm5.empty':('空槽','Empty'), 'm5.preparationRules':('手枪固定携带\n特殊武器 × 1 · 主动道具 × 1\n被动装备最多 6 种，开局各 1 层\n金币与补给每次挑战重置','Pistol always equipped\n1 special weapon · 1 active item\nUp to 6 passives, 1 layer each\nCoins and supplies reset per attempt'),
'm5.locked':('未解锁','Locked'), 'm5.permanent':('已永久解锁','Permanently unlocked'), 'm5.source':('通关来源：关卡','Unlock source: stage'),
'm5.equip':('装入出战栏','Equip for departure'), 'm5.remove':('移出出战栏','Unequip'),
'm5.pendingHint':('奖励已经保存。选择一件未解锁的装备，今后可在任意关卡使用。','Your reward is saved. Choose equipment to use in any future stage.'),
'm5.allCollected':('本关装备已全部收集','All stage equipment collected'),
'm5.pickup':('E · 拾取 {item}','E · Pick up {item}'), 'm5.obtained':('已获得 · {item}','Obtained · {item}'),
'm5.resource.coins':('金币 +{count}','Coins +{count}'), 'm5.resource.health':('生命 +{count}','Health +{count}'), 'm5.resource.energy':('能量 +{count}','Energy +{count}'),
'm5.unlockSaveFailed':('永久奖励保存失败，请重试保存后离开。','Reward save failed. Retry saving before leaving.'),
'm5.weaponChest':('F · 打开武器箱','F · Open weapon chest'),
'm5.stageFeature.1':('花园庭院 · 适合初次远征\n越过安全门廊开始战斗，点亮两座信标后挑战守卫。','Garden courtyard · A gentle first expedition\nCross the foyer, light two beacons and face the guardian.'),
'm5.stageFeature.2':('机械工坊 · 传送带与爆裂装置\n注意地面运动，利用掩体与机械节拍。','Mechanical workshop · Conveyors and explosives\nUse cover and watch the machinery rhythm.'),
'm5.stageFeature.3':('冰封水库 · 冰面与护送挑战\n保持移动，为护送装置留出安全空间。','Frozen reservoir · Ice and escort challenge\nStay mobile and protect the escort route.'),
'm5.stageFeature.4':('孢子温室 · 毒池与连续波次\n避开腐蚀区域，利用间歇补给。','Spore greenhouse · Toxic pools and enemy waves\nAvoid corrosion and use supply breaks.'),
'm5.stageFeature.5':('能源枢纽 · 电网与激光\n观察预警颜色，在电网间歇穿行。','Energy hub · Grids and lasers\nRead the warnings and move between pulses.'),
'm5.stageFeature.6':('星核圣殿 · 最终守卫\n带好装备，留意首领各阶段的攻击预兆。','Starcore sanctum · Final guardian\nPrepare your gear and read each attack phase.'),
'm5.returnStages':('返回关卡选择','Return to stages'), 'm5.retryStage':('重试本关','Retry this stage'),
'm5.rewardSaved':('通关解锁已保存：{item}','Clear unlock saved: {item}'), 'm5.rewardChosen':('已永久解锁：{item}','Permanently unlocked: {item}'),
'm5.chooseReward':('选择一件永久装备','Choose permanent equipment'), 'm5.retryUnlock':('重试保存奖励','Retry reward save'),
'm5.time':('本关用时 {time}','Stage time {time}'),
'm5.settingsMinimal':('精简画面：{state}','Minimal effects: {state}'),
'm5.settingsVignette':('轻微暗角：{state}','Subtle vignette: {state}'),
'm5.settingsNumbers':('伤害数字：{state}','Damage numbers: {state}'),
'm5.testSound':('测试声音','Test sound'),
'm5.trainingStart':('开始选定训练','Start selected practice'), 'm5.trainingSelected':('已选择：{item} · 点击开始才会生成','Selected: {item} · Press Start to spawn'),
'm5.trainingIdle':('待开始','Ready to start'), 'm5.trainingRunning':('进行中；先停止再换配置','Running; stop before reconfiguring'),
'm5.compare':('替换装备','Replace equipment'), 'm5.current':('当前装备','Current equipment'), 'm5.new':('地面装备','Ground equipment'),
'm5.confirmReplace':('确认替换','Confirm replacement'), 'm5.cancelReplace':('保留原装备','Keep current equipment'),
}
for lang, col in [('zh-CN',0),('en',1)]:
    path=root/'Assets/_Game/Resources/Localization'/f'{lang}.json'
    data=json.loads(path.read_text(encoding='utf-8-sig')); table={e['key']:e for e in data['entries']}
    for key,values in pairs.items():
        if key in table: table[key]['value']=values[col]
        else: data['entries'].append({'key':key,'value':values[col]})
    # Explicit interaction domains and the new independent stage journey.
    updates={'hud.controls':('WASD 移动 · 鼠标瞄准 · 左键攻击 · Space 闪避 · Q 武器 · R 主动 · E 拾取 · F 交互 · Tab 地图','WASD move · Mouse aim · LMB fire · Space dodge · Q weapon · R active · E pickup · F interact · Tab map'),
             'hud.exit':('F · 使用出口','F · Use exit'),
             'records.scope':('单关成绩独立记录；旧版连续冒险记录保留为存档。','Stage records are independent. Earlier campaign records remain archived.')}
    for key,values in updates.items():
        if key in table: table[key]['value']=values[col]
    for e in data['entries']:
        if not e['key'].startswith('item.') and ('station' in e['key'] or e['key'].startswith('tutorial.map')):
            e['value']=e['value'].replace('E 键','F 键').replace('按 E','按 F').replace('E ·','F ·').replace('Press E','Press F')
    path.write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
print('M5 bilingual strings updated')
