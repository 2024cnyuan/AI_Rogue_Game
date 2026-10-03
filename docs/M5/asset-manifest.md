# M5 已接入资源索引

机器清单：[asset-manifest.json](asset-manifest.json)。维护规则：[asset-guide.md](asset-guide.md)。来源与提示词：`ArtSource/M5/Briefs/`。实际 `.meta` 均由 Unity 导入生成。

| 稳定 ID/资源 | 运行路径与用途 | 导入与来源 |
| --- | --- | --- |
| explorer.0–15 | `Art/Characters/Player/explorer_walk_atlas.png`；四向四帧，玩家和菜单展示 | imagegen 原创，4×4 原生切片，128 PPU，Bilinear，脚底 pivot (0.5,0.18) |
| 全部 25 个 item ID | `Art/Items/items.png`；持握武器、世界拾取、HUD、整备、奖励、比较 | imagegen 原创，8×4、alpha 收紧、保持比例，无 Mipmap |
| coins/health/energy、beacon/chest/workstation/gate | 同一 items 图集；补给与交互设施 | 独立图像和 ID，不再复用圆点作为物品身份 |
| enemy.basic/archer/shield/bomber/flanker/frost/spore/blocker/assault/sniper、boss.1–6 | `Art/Characters/Enemies/enemies.png`；普通战斗/训练和六首领 | imagegen 原创修整，4×4、alpha 收紧，脚底 pivot (0.5,0.2) |
| enemy.turret/drone/sporelet | `Art/Characters/Enemies/enemies-extra.png` | imagegen 专用补图，3×1，替代临时别名 |
| floor.1–6 | `Art/World/floors.png`；庭院/铜/冰/孢/电/象牙六主题 | 3×2、FullRect、128 PPU、约 4 单位平铺；不整体拉伸地面 |
| wall.1–6/arch.1–6/landmark.1–6 | `Art/World/props.png`；分段墙、出入口、地标 | 6 列，按实际行高度切片，alpha 收紧；保留碰撞逻辑独立 |
| gardens/workshop/reservoir/greenhouse/hub/sanctum | `UI/StageCards/stagecards.png`；六张选关插画 | 3×2、UI 使用原生裁切保持比例 |
| menu | `UI/Backgrounds/menu.png` | imagegen 星港插画，页面共享背景 |
| StarfallBodySC/EmphasisSC/TitleSC | `UI/Fonts/Generated/*.asset` 与各 SourceHan 源字体 | Adobe 官方 OTF、OFL；42 点 SDFAA、2048 多图集、动态字库，无系统字体依赖 |
| TMP shader/断行规则 | `UI/TMPShaders/`、`UI/TMPResources/` | 项目安装的 Unity UGUI 包，保留原 GUID 与 UGUI 许可 |
| M5_SpriteLit/Unlit | `Art/Materials/` | 当前 URP 2D 原生着色器 |
| M5_Default/Minimal | `Data/Rendering/VolumeProfiles/` | 世界相机显式启用 HDR/后期；UI Overlay 不参与世界后期 |
| 8 类事件＋7 武器＋环境 | `Audio/SFX/`、`SFX/Weapons/`、`Ambience/` | 原创确定性合成 WAV，源稿/指标见 `ArtSource/M5/Audio/`，源码 `Tools/M5Audio.py` |
| StarfallMixer | `Audio/Mixers/StarfallMixer.mixer` | Music/SFX/UI 三总线，监听器软限幅 |
| 75 个稳定 ID Prefab＋Explorer | `Prefabs/Visuals/`；世界物、持握武器、主角表现层 | Unity 原生生成、目录显式引用并实际实例化，后续设置保留已存在内容 |

上表路径相对 `Assets/_Game/`。`Resources/PresentationCatalog.asset` 显式引用所有运行资源，普通构建无需联网。未使用的初版世界图只保留制作历史；不会通过文件夹存在来宣称已经加载。

状态：以上项目已接入真实 Boot 运行流程并通过引用检查；最终自动测试结果和用户审美、听感、节奏验收分开记录于外层 `appendix/M5/acceptance.md`。
