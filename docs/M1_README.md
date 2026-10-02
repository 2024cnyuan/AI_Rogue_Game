# M1 启动与验收

## 启动

1. 在 Unity Hub 打开 `D:\UnityProjects\AI_Rogue_Game\AI_Rogue_Game`，不是外层放置 prompt.md 的目录。
2. 使用 **Unity 6000.6.4f1**，等待脚本与资源导入。
3. 打开 **Assets/_Game/Scenes/Boot.unity**，点击 Play。首次选择简体中文或 English，确认后进入主菜单。
4. 正常情况下无需初始化。若入口或配置缺失，执行 **Starfall > Setup M1 Prototype**。该工具创建缺失资源，不覆盖已有 Boot 场景或调参配置；会将 Boot 放到构建场景列表第一项。原 SampleScene 保留。
5. Windows 构建菜单为 **Starfall > Build M1 Windows**，输出 **Builds/M1/StarcoreLabyrinth.exe**。运行时要保留同目录的 DLL、MonoBleedingEdge、D3D12 和 StarcoreLabyrinth_Data 等文件。构建与日志不加入 Git。

## 按顺序验收

1. **菜单与语言**：切换两种语言，检查未完成入口均显示“开发中”，继续冒险禁用并说明无检查点。再次 Play 后应记住语言，不再强制选择。
2. **移动与枪械**：进入样板，用 WASD 和斜向移动，鼠标瞄准、按住左键射击。绕过两组错位掩体；贴墙时不能从墙另一侧生成弹丸，敌我弹丸颜色与形状不同。按住射击点击暂停菜单后，恢复需松开再按左键，避免 UI 点击开火。
3. **闪避与暂停**：Space 朝移动方向闪避，静止时沿瞄准方向闪避；不能穿墙，闪避中不能开火。Tab 地图、Esc 暂停、暂停设置和切出窗口应冻结敌人、弹丸、闪避与冷却。设置返回后仍应停留在原暂停层。仅恢复焦点不应解除已手动打开的地图/暂停。
4. **拾取与死亡**：靠近金币自动吸附，治疗恢复 25；满生命时治疗留在地面。让敌人击败玩家：立即停止战斗，只显示失败界面。重新开始应恢复 100 生命、0 金币、五名守卫、关闭出口，清掉旧弹丸与掉落。
5. **正常清场**：用手枪击败五名守卫。每次清场只奖励 10 金币，出口变为青色，前往右侧门廊按 E 显示样板完成界面；不会写正式纪录。重开和返回菜单均不带入上一轮资源。
6. **双语和比例**：战斗中通过 Esc > 设置切换语言，检查 HUD、任务、地图、提示立即更新，生命/金币/暂停状态保持。两种语言分别检查 1280×720、1920×1080、2560×1440、1280×960；死亡或完成后返回菜单也应保留语言。

## 模块与配置

- `Assets/_Game/Scenes/Boot.unity`：唯一新增入口，持有 StarfallGame 和调参资产引用，房间和界面运行时创建。
- `Assets/_Game/Resources/PrototypeConfig.asset`：角色、手枪与敌人基线数值。未经人工试玩确认，不能视为已平衡。
- `Assets/_Game/Scripts/Core`：流程、模式、嵌套暂停、基础规则与配置。
- `Player / Combat / Enemies / Items / World / UI / Save`：输入与物理、伤害与弹丸池、敌人预警/寻路、自动拾取、固定房间、UGUI 与本地设置。
- `Resources/Localization/{zh-CN,en}.json`：全部当前玩家可见文案。名称与具名参数使用同一键，切换不重启关卡。
- `Tests/EditMode / PlayMode`：规则、真实 Input System 事件、射击命中、清理与截图检查。

M1 新脚本使用 `Starfall` 命名空间，不修改原来的空 `Assets/PlayerController.cs`，不会产生同名类。

## 设置与复测

设置路径是 `Application.persistentDataPath/starfall-settings.json`，不是工程 Assets。使用版本号、同目录临时文件与备份替换；损坏文件会尝试读取备份并保留损坏副本。存档错误在界面显示，不阻止继续试玩。M1 只保存语言选择和显示模式，不保存战斗进度或个人纪录。

通过 **Window > General > Test Runner** 分别运行 Starfall 的 EditMode、PlayMode 测试。测试保存到独立临时目录，并恢复测试用的 Input System 无焦点配置，不改个人设置。PlayMode 会在 `Logs/M1-Screens` 生成真正运行渲染的截图，检查语言参数、字体字符与布局。

资源检查菜单为 **Starfall > Validate M1 Assets**；会打开 Boot 场景，因此先保存当前场景改动。

## 尚未交付

教学、训练营、特殊武器、主被动道具、正式第一关、Boss、成长、统一计时、个人纪录、检查点与后续五关属于后续阶段。当前为原创程序像素形状占位，美术、音效、震屏/闪光设置与平衡未完成。没有实测性能数字。详见资源登记和验证记录。
