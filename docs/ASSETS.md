# M1 / M2a 资源登记

| 资源 | 来源/用途 | 状态与替换路径 |
| --- | --- | --- |
| 地块、石墙、苔藓、灯柱、方向标记、门 | 本项目 `PrototypeRoom.cs` 程序生成，无第三方图片 | 功能占位。后续替换到 `Assets/_Game/Art/World`，保留真实碰撞范围和可行走路线。 |
| 探险者、菱形追击者、斜角射手、金币、治疗十字 | 本项目 `PrototypeVisuals.cs` 生成 16×16 像素轮廓，Point 过滤 | 功能占位。后续替换到 `Assets/_Game/Art/Characters` 与 `Art/Items`，补完整动画。 |
| 枪口闪光、命中变白、闪避亮色、敌人预警 | SpriteRenderer 与程序参数 | 基础反馈占位，没有把它们列为正式美术完成。 |
| Sprite 材质 | Unity 项目已安装 URP 的 Sprite Unlit shader | `Assets/_Game/Resources/PrototypeSprite.mat`，显式保留 shader，避免运行时资源在构建中缺失。 |
| 中文/英文字体 | Windows 本机 Microsoft YaHei / SimHei / Arial，由 Unity 动态请求 | **不复制、不打包、不分发系统字体文件**。当前只面向有这些字体的 Windows 机器。测试检查实际用到的非 ASCII 字符。正式发布前需替换/补入明确允许分发的中英字体并验证回退。 |
| 音乐、音效 | M1 无新增音频 | M2b 提供基础音效，后续做混音与正式风格。 |
| 教学目标、靶子、训练工作站、功能区底色、机关提示条 | 复用 `PrototypeVisuals.cs` 与程序绘制 | M2a 功能占位。靶子、场景和 UI 在正式美术阶段统一替换。 |
| 物品图标与参数 | `ItemCatalog.asset` 集中定义，复用生成的 orb/cross 等轮廓 | 9 件物品效果已实现；其他定义明确 `implemented=false`，不计入已完成内容。正式独立图标后续替换。 |

未购买资源或插件，未生成 AI 图片，未更换原模板资源。上述登记是占位资源清单，不代表 M5 美术验收通过。
