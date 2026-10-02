# M1 验证记录

日期：2026-10-02。Unity 6000.6.4f1，项目原 Input System 1.20.0、URP 17.6.0、UGUI 2.6.0、Test Framework 1.8.0；未升级依赖。

## 已执行

- Unity 批处理编译和 Editor API 生成入口：成功。`Logs/M1-setup-retry.log` 包含 `STARFALL_M1_ASSETS_OK` 和 `STARFALL_M1_SETUP_OK`。检查双语键/参数、入口 Bootstrap/配置引用及丢失脚本。
- EditMode：10/10 通过，结果 `Logs/M1-editmode.xml`。覆盖伤害无敌与保护窗口、满血治疗、嵌套暂停、未登记不清场、奖励去重、死亡优先、模式资源隔离、双语参数、设置安全保存/损坏恢复、非法语言回退。
- PlayMode：8/8 通过，结果 `Logs/M1-playmode.xml`。测试从 Boot 入口加载，用真实 Input System 键鼠事件驱动角色、闪避与手枪。覆盖斜向速度、瞄准、闪避保护、嵌套暂停冻结、墙与枪口阻挡、真实弹丸命中、满血补给保留、弹丸池重用、清场/死亡/重开清理、出口 E 交互、切换语言不重建状态与多分辨率布局。死亡和清场分支使用显式伤害注入以验证规则，不能据此声称正常完整试玩或平衡通过。
- 真实运行截图：`Logs/M1-Screens`。中英文菜单、HUD、地图、设置、死亡和样板完成。最终检查包含 1280×720、1920×1080、2560×1440 和 1280×960 的菜单、房间与设置，并校验当前显示的字符、文本高度和未格式化参数。
- 测试设置写入独立临时目录，不使用用户个人存档，不产生正式纪录。无焦点批处理仅在测试中临时启用 IgnoreFocus / AllDeviceInputAlwaysGoesToGameView，并在结束恢复；产品仍在失焦时暂停。

首次沙箱内 Unity 授权客户端 IPC 超时；使用批准的沙箱外 Unity 运行后编译与测试正常。第一次输入测试失败，因为编辑器默认将无焦点 Game View 的键鼠事件路由给编辑器；修正测试环境后才记录通过。

## 未验证 / 不做完成声明

- 人工键鼠手感、完整正常装备清场、不同水平玩家的难度和数值平衡。
- 手动 Alt-Tab 及窗口/全屏切换；自动测试验证暂停原因的状态和冻结，不代替真实窗口交互。
- 普通 Windows 程序的完整人工游玩、目标机器字体回退与 UI 点击感受。
- 60 FPS、95% 帧耗时、GC、压力战斗、20 分钟内存稳定性：未测量，没有性能达标声明。
- 新手关、训练营、正式六关、纪录/检查点与其他后续阶段功能：未实现，未验收。

原有 Welcome 图片、教学框架设置、ProjectSettings.asset、.vsconfig 与空 PlayerController 改动均保留，不纳入本阶段实现声明。没有 git add、commit、push、pull、分支切换或远程 PR。

## 最终复核

- 普通 Windows x64 构建成功，**非 Development Build**。`Logs/M1-build.log` 包含 `STARFALL_M1_BUILD_OK: 105326453 bytes, 0 errors`，输出为 `Builds/M1/StarcoreLabyrinth.exe`，包含配套数据与运行库。
- 后台启动该普通构建 15 秒，进程保持运行，日志完成引擎/Mono/物理初始化，未见托管异常、丢失资源或脚本错误；然后关闭仅此次检查启动的进程。日志 `Logs/M1-player.log`。这是启动冒烟，**没有人工操作或独立程序界面截图，不代表完整程序游玩验证**。
- 启动日志记录 DX12 / NVIDIA GeForce RTX 4070 Laptop GPU / 7948 MB VRAM / 驱动 32.0.15.6607；还记录 D3D12 查询 info queue 接口失败提示。没有据此计算性能或声称图形适配完全通过。
- 查看真实截图的中英文房间、菜单、地图、设置、死亡与完成卡片；当前使用到的中文字符校验通过。截图来自 Editor PlayMode 实际渲染，完成截图使用测试伤害注入，不冒充正常手打通关。
- 资源 `.meta` 与 GUID 均由 Unity 生成。Unity 在构建时写入的 URP shader 预过滤参数、全局运行设置和 UnityConnect 开关已还原，未留下渲染配置或云设置改动；场景模板自动配置文件已清理。仅保留本次入口 Build Settings 与针对构建临时资源的 .gitignore 增量。
- `git diff --check` 通过。Git 无提交、推送、拉取或分支切换。用户验收尚未通过。
