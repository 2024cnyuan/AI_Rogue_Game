# M5 资源与维护规范

实际风格与导入配置见 [style-guide.md](style-guide.md)，稳定 ID 和资源文件见 [asset-manifest.md](asset-manifest.md)。

## 资源位置

| 内容 | 原始文件 | Unity 运行时 |
| --- | --- | --- |
| 主角四向四帧行走 | `ArtSource/M5/Characters/explorer_walk_atlas.png` | `Assets/_Game/Art/Characters/Player/` |
| 普通敌人、六首领 | `ArtSource/M5/Characters/enemies-clean.png` | `Assets/_Game/Art/Characters/Enemies/enemies.png` |
| 专用炮台、无人机、孢体 | `ArtSource/M5/Characters/enemies-extra.png` | `Assets/_Game/Art/Characters/Enemies/enemies-extra.png` |
| 25 件装备、补给、设施 | `ArtSource/M5/Items/items.png` | `Assets/_Game/Art/Items/items.png` |
| 六主题地面 | `ArtSource/M5/World/floors.png` | `Assets/_Game/Art/World/floors.png` |
| 六主题墙/门/地标 | `ArtSource/M5/World/props.png` | `Assets/_Game/Art/World/props.png` |
| 六张独立关卡插画 | `ArtSource/M5/UI/stagecards.png` | `Assets/_Game/UI/StageCards/` |
| 星港主菜单插画 | `ArtSource/M5/UI/menu.png` | `Assets/_Game/UI/Backgrounds/` |
| 字体源及 OFL | — | `Assets/_Game/UI/Fonts/` |
| 音频源、数值分析 | `ArtSource/M5/Audio/`、`Tools/M5Audio.py` | `Assets/_Game/Audio/` |
| 灯光、默认/精简后期 | — | `Assets/_Game/Data/Rendering/VolumeProfiles/` |
| 76 个视觉 Prefab | — | `Assets/_Game/Prefabs/Visuals/` |

所有图片由内置 imagegen 生成。原始提示词见 `ArtSource/M5/Briefs/generated-assets.md`，修订提示词见 `edits.md`。原始输出保留，运行时 PNG 复制入项目；按 Unity Sprite Data Provider 切片，透明格按 alpha 边界收紧，保持纵横比。初版 world 图保留为源稿归档；正式地面使用新版 floors，按 128 PPU、约 4 世界单位纹理模块平铺。透明建筑使用 props 图集并按实际三行高度切片，避免把邻行内容切入墙体。墙按约 2 世界单位分段，碰撞仍由原逻辑配置维护。

字体来自 Adobe 官方 [Source Han Sans](https://github.com/adobe-fonts/source-han-sans/tree/release/OTF/SimplifiedChinese) 和 [Source Han Serif](https://github.com/adobe-fonts/source-han-serif/tree/release/OTF/SimplifiedChinese)，分别用于正文/强调和标题。原始 OTF、OFL 文本、预热 SDF 和多图集均随项目分发。TMP 着色器及中文断行资源来自本项目安装的 Unity UGUI 包，其原始 Unity 包许可随依赖保留。

音频为本项目原创的确定性合成与混音，无第三方采样。8 类事件、7 类武器及立体声环境乐分开存储；24 个 SFX 播放源受限复用，Music/SFX/UI 走独立总线，UI 测试声音可在暂停界面播放。素材的峰值上限为 0.82；此数值不等同于最终混合输出或硬件听感验收。

`AudioOutputLimiter` 在监听器总输出对 0.65 以上的信号连续软限幅，幅度小于 0.95；回调不分配内存。实时音量设置同时更新已有音源。信号探针可证明软件播放链路，不能代替真实扬声器/耳机验收。

## 25 个 ID 的永久来源

| 来源 | 固定解锁 | 待选池 |
| --- | --- | --- |
| 初始 | pistol、medkit | — |
| 1 庭院 | shotgun | rapid、magnet、vitality |
| 2 工坊 | smg | shield、blast、workshop_smg |
| 3 水库 | crossbow | slow、pierce、agile |
| 4 温室 | launcher | grenade、controlled、flawless |
| 5 中枢 | shock | bounce、critical、lowhealth |
| 6 圣殿 | arc | decoy、recharge |

图标、世界物、整备槽、拾取卡均以稳定 item ID 绑定。不要靠数组顺序推断装备所有权；新增装备需同时覆盖图像、双语说明、实际实现和奖励来源。

正式 Sprite 对应 75 个通用视觉 Prefab，另有 `Explorer.prefab`。世界/物品绘制从目录引用实例化 Prefab；主角从 Explorer 实例创建后按四向帧更新。碰撞和玩法组件保持独立，避免误把画布大小当作碰撞尺寸。编辑器可编辑表现层及子对象；`Setup M5` 对已存在的视觉 Prefab 保留其内容。统一资源替换使用 PresentationCatalog 和导入切片，不通过对象名称猜测物品 ID。

## 视觉与排版

角色使用青绿披风、暖金细节；世界六主题分别为苔绿、赤铜、冰蓝、孢紫、电蓝和象牙金。普通装饰低于弹道、伤害预警与角色；世界有接触阴影、有限局部光和静态掩体阴影。默认轻 Bloom/暗角，精简模式保留必要危险预警。HUD 和菜单使用独立 Overlay Canvas，不进入世界后期。

UI 使用深青底、象牙正文、青绿操作焦点和暖金奖励。正文最小 16；主要标题用宋体；长名称允许换行。六主题选关在宽屏为 3×2，4:3 为 2×3；详情和开始按钮固定。整备、设置、商店、比较、训练、结算共用图标和字族。世界输入在模态期间暂停并要求松键后重新触发。

新增资源的 SHA-256、图像尺寸、透明度和音频指标记录于 `asset-manifest.json`，便于后续替换和验收。
