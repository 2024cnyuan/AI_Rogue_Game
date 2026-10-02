# M1 / M2a / M2b / M3 资源登记

| 资源 | 来源/用途 | 状态与替换路径 |
| --- | --- | --- |
| 地块、石墙、苔藓、灯柱、方向标记、门 | 本项目 `PrototypeRoom.cs` 程序生成，无第三方图片 | 功能占位。后续替换到 `Assets/_Game/Art/World`，保留真实碰撞范围和可行走路线。 |
| 探险者、菱形追击者、斜角射手、金币、治疗十字 | 本项目 `PrototypeVisuals.cs` 生成 16×16 像素轮廓，Point 过滤 | 功能占位。后续替换到 `Assets/_Game/Art/Characters` 与 `Art/Items`，补完整动画。 |
| 枪口闪光、命中变白、闪避亮色、敌人预警 | SpriteRenderer 与程序参数 | 基础反馈占位，没有把它们列为正式美术完成。 |
| Sprite 材质 | Unity 项目已安装 URP 的 Sprite Unlit shader | `Assets/_Game/Resources/PrototypeSprite.mat`，显式保留 shader，避免运行时资源在构建中缺失。 |
| 中文/英文字体 | Windows 本机 Microsoft YaHei / SimHei / Arial，由 Unity 动态请求 | **不复制、不打包、不分发系统字体文件**。当前只面向有这些字体的 Windows 机器。测试检查实际用到的非 ASCII 字符。正式发布前需替换/补入明确允许分发的中英字体并验证回退。 |
| 基础音效与环境底音 | M2b `GameAudio.cs` 通过正弦波/包络程序生成，未使用外部音乐或采样 | 区分射击、命中、受伤、闪避、拾取、UI、清场和 Boss；8 个战斗声槽、独立 UI 声槽及循环底音。音色、音量平衡和正式混音待 M5；未声称人工听感通过。 |
| 教学目标、靶子、训练工作站、功能区底色、机关提示条 | 复用 `PrototypeVisuals.cs` 与程序绘制 | M2a 功能占位。靶子、场景和 UI 在正式美术阶段统一替换。 |
| 物品图标与参数 | `ItemCatalog.asset` 集中定义，复用生成的 orb/cross 等轮廓 | 9 件基础物品与工坊稀有冲锋枪变体已实现；其他定义明确 `implemented=false`，不计入已完成内容。正式独立图标后续替换。 |
| 第一关四类战斗房、继电室、石卫队长与预警 | `FirstLevelPlan.cs` / `PrototypeRoom.cs` / `StoneCaptain.cs` 原创程序布局和轮廓 | M2b 玩法资源占位；掩体和预警有真实碰撞/伤害规则，后续替换视觉时保留安全通路和预警时序。 |
| 工坊/水库布局、传送带、冰面、炉体/水闸危险、供能机器人 | `FirstLevelPlan.cs` / `ThemeEnvironment.cs` / `AdventureDirector.cs` 原创程序布局与轮廓 | M3 功能占位，两主题使用不同拓扑、掩体及表面规则；后续替换到 `Art/World`、`Art/Characters`，保留任务可达性和限时/暂停规则。 |
| 炮台、护盾兵、爆破机、冰弹射手、侧翼游击者与两名 Boss | `PrototypeEnemy.cs` / `ThemeBoss.cs` 程序轮廓、预警圆圈和射线 | M3 功能占位；Boss 三动作有真实预警、执行和恢复，霜镜光束沿墙反射。美术与完整动画待 M5，难度待人工调参。 |

未购买资源或插件，未生成 AI 图片，未更换原模板资源。上述登记是占位资源清单，不代表 M5 美术验收通过。
