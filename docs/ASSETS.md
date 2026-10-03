# M1 / M2a / M2b / M3 / M4 资源登记

## M5 正式资源

M5 已接入精细手绘 2D：主角四向行走、敌人/六 Boss、25 装备、补给与设施、六主题世界、六关插画及菜单背景。实际加载索引为 `Resources/PresentationCatalog.asset`，通过 Unity 原生切片引用正式 PNG。源稿/提示词保留在 `ArtSource/M5/`。字体源、SDF 与 OFL 位于 `UI/Fonts/`；16 段原创 WAV、三总线混音和后期 Profile 随普通构建打包。

详见 [M5 资源维护规范](M5/asset-guide.md)、[逐文件 SHA-256 清单](M5/asset-manifest.json)。旧程序形状目前只用于弹道、危险范围、血条、接触阴影等必要标示；以下清单是 M1–M4 历史登记。新增资源检查不等于用户审美或设备听感验收。

| 资源 | 来源/用途 | 状态与替换路径 |
| --- | --- | --- |
| 地块、石墙、苔藓、灯柱、方向标记、门 | 本项目 `PrototypeRoom.cs` 程序生成，无第三方图片 | 功能占位。后续替换到 `Assets/_Game/Art/World`，保留真实碰撞范围和可行走路线。 |
| 探险者、菱形追击者、斜角射手、金币、治疗十字 | 本项目 `PrototypeVisuals.cs` 生成 16×16 像素轮廓，Point 过滤 | 功能占位。后续替换到 `Assets/_Game/Art/Characters` 与 `Art/Items`，补完整动画。 |
| 枪口闪光、命中变白、闪避亮色、敌人预警 | SpriteRenderer 与程序参数 | 基础反馈占位，没有把它们列为正式美术完成。 |
| Sprite 材质 | Unity 项目已安装 URP 的 Sprite Unlit shader | `Assets/_Game/Resources/PrototypeSprite.mat`，显式保留 shader，避免运行时资源在构建中缺失。 |
| 中文/英文字体 | Windows 本机 Microsoft YaHei / SimHei / Arial，由 Unity 动态请求 | **不复制、不打包、不分发系统字体文件**。当前只面向有这些字体的 Windows 机器。测试检查实际用到的非 ASCII 字符。正式发布前需替换/补入明确允许分发的中英字体并验证回退。 |
| 基础音效与环境底音 | M2b `GameAudio.cs` 通过正弦波/包络程序生成，未使用外部音乐或采样 | 区分射击、命中、受伤、闪避、拾取、UI、清场和 Boss；8 个战斗声槽、独立 UI 声槽及循环底音。音色、音量平衡和正式混音待 M5；未声称人工听感通过。 |
| 教学目标、靶子、训练工作站、功能区底色、机关提示条 | 复用 `PrototypeVisuals.cs` 与程序绘制 | M2a 功能占位。靶子、场景和 UI 在正式美术阶段统一替换。 |
| 物品图标与参数 | `ItemCatalog.asset` 集中定义，复用生成的 orb/cross/arrow 等轮廓 | M4 已实现六类武器、六主动、十二被动，另保留工坊稀有冲锋枪变体；训练分页显示图标、稀有度、说明与层数。正式独立图标待 M5。 |
| 第一关四类战斗房、继电室、石卫队长与预警 | `FirstLevelPlan.cs` / `PrototypeRoom.cs` / `StoneCaptain.cs` 原创程序布局和轮廓 | M2b 玩法资源占位；掩体和预警有真实碰撞/伤害规则，后续替换视觉时保留安全通路和预警时序。 |
| 工坊/水库布局、传送带、冰面、炉体/水闸危险、供能机器人 | `FirstLevelPlan.cs` / `ThemeEnvironment.cs` / `AdventureDirector.cs` 原创程序布局与轮廓 | M3 功能占位，两主题使用不同拓扑、掩体及表面规则；后续替换到 `Art/World`、`Art/Characters`，保留任务可达性和限时/暂停规则。 |
| 炮台、护盾兵、爆破机、冰弹射手、侧翼游击者与两名 Boss | `PrototypeEnemy.cs` / `ThemeBoss.cs` 程序轮廓、预警圆圈和射线 | M3 功能占位；Boss 三动作有真实预警、执行和恢复，霜镜光束沿墙反射。美术与完整动画待 M5，难度待人工调参。 |
| 温室/中枢/圣所布局、可破坏菌丛、毒雾安全岛与电网 | `FirstLevelPlan.cs` / `PrototypeRoom.cs` / `ThemeEnvironment.cs` / `DestructibleCover.cs` 原创程序图形 | M4 功能占位；真实碰撞、导航更新、危险范围、倒计时和永久安全通路。正式植物、轨道与圣所美术待 M5。 |
| 分裂孢体、封锁者、突击者、狙击手、无人机与三名新 Boss | `PrototypeEnemy.cs` / `LateCampaignBoss.cs` 程序轮廓与预警 | M4 功能占位；真实分裂、区域攻击、冲刺、支援治疗、藤蔓碰撞及三阶段切换。有限召唤不授予击杀奖励，死亡清场。正式动画待 M5。 |
| 减速区域、爆炸圈、诱饵、电弧命中与蓄力亮色 | `CombatEffects.cs` / `ProjectilePool.cs` / `ExplorerController.cs` 原创程序反馈 | 固定容量可复用效果；换房/重置回收，不含第三方或 AI 图片。爆炸与连锁表现仍为功能占位，电弧正式连线特效和音色差异待 M5。 |

未购买资源或插件，未生成 AI 图片，未更换原模板资源。上述登记是占位资源清单，不代表 M5 美术验收通过。
