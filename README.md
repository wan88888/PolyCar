# PolyCar 项目说明

PolyCar 是基于 Unity 的低多边形漂移驾驶游戏项目。玩家驾驶车辆穿过有序检查点，在十条逐步提高难度的路线中完成计时挑战、积累漂移分数、收集金币并解锁后续路线与车辆。

当前包含四辆可选车辆、十关独立环境、车库与个人纪录页面、比赛 HUD、暂停与结算流程，以及可重复运行的物理驾驶和环境检查工具。项目仍在迭代；自动验证、截图审查与人工驾驶体验的范围见下文。

## 环境与快速运行

| 项目 | 当前配置 |
|---|---|
| Unity Editor | **6000.2.7f2** |
| 渲染管线 | Universal Render Pipeline（URP）17.2.0 |
| 输入系统 | Unity Input System 1.14.2 |
| 测试框架 | Unity Test Framework 1.6.0 |
| 当前交付运行包 | macOS 开发版 |

版本依据：[ProjectVersion.txt](ProjectSettings/ProjectVersion.txt)、[manifest.json](Packages/manifest.json)。

1. 在 Unity Hub 中添加本仓库目录，并用 **6000.2.7f2** 打开。
2. 等待依赖解析与资源导入完成。
3. 打开 [Assets/Scenes/DriftPrototype.unity](Assets/Scenes/DriftPrototype.unity)。这是游戏入口；仓库中的 `SampleScene.unity` 是模板场景。
4. 点击 Unity 的 **Play**，在首页开始游戏，或进入选关和车库。
5. 首次游玩从第 1 关开始，完成当前关即可解锁下一关。

直接游玩已有场景无需执行重建命令。重建会覆盖生成内容，具体见“场景制作与重建”。

## 操作方式

| 操作 | 键盘 | 手柄驾驶输入 |
|---|---|---|
| 加速 | W / ↑ | 右扳机 |
| 倒车 | S / ↓ | 当前未配置独立倒车输入 |
| 转向 | A、D / ←、→ | 左摇杆横向 |
| 手刹漂移 | 空格 | 下方面键（常见为 A / ×） |
| 脚刹 | 左 Shift / 右 Shift | 左扳机 |
| 比赛中救援 | R | 未配置专用手柄键 |
| 暂停 / 返回 | Esc | 未配置专用手柄键；界面提供暂停按钮 |
| 结算后重玩 | R | 界面提供结算按钮 |
| 结算后进入下一关 | N（下一关可用时） | 界面提供结算按钮 |

菜单可使用鼠标；已聚焦的按钮可按 Enter 提交。手柄驾驶输入已接入，完整菜单导航仍需专项验证。

漂移时在入弯处轻点手刹，保持油门并调整转向；反打可以维持侧滑。手刹与脚刹是两套输入，手刹不会等同于全力脚刹。实际映射见 [CarController.cs](Assets/Scripts/CarController.cs) 和 [MainMenuUI.cs](Assets/Scripts/MainMenuUI.cs)。

## 比赛、奖励与进度

- **完成条件**：等待三秒倒计时结束，按顺序、沿正向通过所有检查点。当前路线均为单圈；第 1 关有 12 个检查点。
- **金币**：沿途金币为可选收集项，收齐金币不会代替检查点通关。比赛内显示本局收集进度；钱包用于车库解锁车辆。
- **漂移计分**：车辆需要有实际侧滑，速度至少 18 km/h、横向速度至少 0.5 m/s。有效干净漂移段需要持续至少 1.1 秒并获得至少 120 分，才能获得对应漂移奖励。三秒没有计分会重置连击，碰撞会使当前漂移段失效。
- **奖牌**：完成路线获得铜牌；达到该关漂移目标获得银牌；同时达到时间与漂移目标且没有救援获得金牌。各关目标不同，集中配置于 [CourseDifficultyBuilder.cs](Assets/Editor/CourseDifficultyBuilder.cs)。
- **奖励结算**：通关后将本局收集金币、漂移奖励和 10 枚通关奖励合并，再扣除本局碰撞等罚金，最低为 0。重新开始或中途离开不会将未结算奖励存入钱包，碰撞也不会直接扣除原有钱包余额。
- **救援**：R 将车辆恢复到最近通过的检查点附近，保留检查点进度并增加 3 秒用时。翻覆持续两秒或跌落至恢复阈值以下会触发自动救援。
- **解锁**：完成即可解锁下一关，不要求金牌或收齐金币。车库购买、选车在比赛外进行；车辆解锁价格由 [GameBalance.cs](Assets/Scripts/GameBalance.cs) 管理。
- **纪录**：保存各关个人最佳用时、漂移分数和收益。纪录页面显示真实存档结果，没有预设竞争者成绩。

核心流程见 [LevelManager.cs](Assets/Scripts/LevelManager.cs)、[RaceCourse.cs](Assets/Scripts/RaceCourse.cs) 和 [GameManager.cs](Assets/Scripts/GameManager.cs)。

## 十关主题

| 关卡 | 游戏内名称 | 环境与视觉地标 | 路宽 / 障碍数 |
|---|---|---|---|
| 1 | Harbor Loop | 港口集装箱区、货轮、吊机、装卸码头 | 12 m / 0 |
| 2 | S-Curve Sprint | 城市工业街区、仓库、管线、连续 S 弯 | 11 m / 2 |
| 3 | Switchback Climb | 松林盘山路、瞭望台、山坡与护栏 | 10.5 m / 4 |
| 4 | Quarry Figure Eight | 阶梯采石场、碎石堆、挖掘机与输送设备 | 10 m / 6 |
| 5 | Sunset Slalom | 日落海滨、沙滩、海面、棕榈与凉亭 | 9.5 m / 8 |
| 6 | Dockside Spiral | 渔港小镇、栈桥、渔船、临海建筑与灯塔 | 9 m / 10 |
| 7 | Canyon Needles | 红岩峡谷、层状岩壁、临路岩体与仙人掌 | 8.5 m / 12 |
| 8 | Ridge Hairpins | 高山连续发卡弯、山谷、远峰与山间建筑 | 8 m / 14 |
| 9 | Industrial Gauntlet | 重工业厂区、储罐、烟囱、大型架空管线 | 7.6 m / 16 |
| 10 | Midnight Pinball | 夜间街区、路灯、发光招牌与局部照明 | 7.2 m / 18 |

第 3、8 关具有约 10.3 m、15.5 m 的真实路线高差，山路最大坡度约 7.5%。工业区和夜间街区采用平整交叉路面；其他关卡有与主题相符的地形和道路衔接。路肩、回返缓坡、挡墙、护栏、排水设施与远景由生成器统一制作，各关拥有独立天空、雾、阳光和环境光。

关卡预览取自实际场景，保存在 [Assets/UI/RoutePreviews](Assets/UI/RoutePreviews)。十关环境审查总览见 [all-route-overviews.png](Review/Environment/all-route-overviews.png)。

## 项目结构与主要代码

```text
Assets/
  Scenes/DriftPrototype.unity   游戏主场景
  Scripts/                     运行时驾驶、比赛、菜单、存档与表现
  Editor/                      场景生成、素材适配、截图、验证和构建
  Editor/Tests/                EditMode 回归测试
  Models/ Materials/ Prefabs/  生成的网格、材质、预制体
  Shaders/                     天空、水面、轮胎痕迹、世界文字等着色器
  UI/RoutePreviews/             十张关卡预览
  UI/VehiclePreviews/           车辆预览
  ThirdParty/Kenney/            外部美术及来源、许可记录
  Docs/                        详细开发与搭建说明
Review/Environment/            环境验收报告、截图、测试与性能证据
Builds/                        本地产生的运行包
Packages/ ProjectSettings/     Unity 依赖与项目设置
```

| 文件 | 职责 |
|---|---|
| [CarController](Assets/Scripts/CarController.cs) | 输入、接地检测、推进、制动、转向与侧向抓地 |
| [CarPresentation](Assets/Scripts/CarPresentation.cs) | 车轮、车身姿态、刹车灯、烟雾与轮胎痕迹 |
| [CameraFollow](Assets/Scripts/CameraFollow.cs) | 行驶追尾镜头与菜单展示镜头 |
| [SceneLook](Assets/Scripts/SceneLook.cs) | 应用关卡光照与雾，离开关卡时恢复共享状态 |
| [LowPolyDriftPrototypeBuilder](Assets/Editor/LowPolyDriftPrototypeBuilder.cs) | 主场景、车辆、界面和关卡引用的组装 |
| [CourseDifficultyBuilder](Assets/Editor/CourseDifficultyBuilder.cs) | 十关路线、路宽、障碍、安全间隔与奖牌目标 |
| [CourseTerrainBuilder](Assets/Editor/CourseTerrainBuilder.cs) | 高度场、地形碰撞、路肩缓坡和山路设施 |
| [ShowcaseSceneryBuilder](Assets/Editor/ShowcaseSceneryBuilder.cs) | 第 1–2 关港口与工业街区 |
| [ThemedSceneryBuilder](Assets/Editor/ThemedSceneryBuilder.cs) | 第 3–10 关地标、主题布景和光照配置 |
| [ExternalArtBuilder](Assets/Editor/ExternalArtBuilder.cs) | Kenney 素材的尺度、材质、车轮与场景适配 |
| [RoutePreviewBuilder](Assets/Editor/RoutePreviewBuilder.cs) | 从实际关卡生成选关预览图 |

## 场景制作与重建

修改路线形状、地形、环境或 UI 时，优先修改对应生成器，再重建验证。手动编辑生成后的场景可以用于试验，但若希望保留，应将改动同步到生成器或单独保存。

**重建会覆盖 `DriftPrototype.unity` 以及生成的模型、材质、预制体和预览图。执行前应保存或提交需要保留的手工修改。**

常用入口：

| 入口 | 行为 |
|---|---|
| `Tools > PolyCar > Create Drift Prototype Scene` | 重建游戏主场景；素材清单存在时自动应用外部美术 |
| `Tools > PolyCar > Build All Routes With External Art` | 重建四辆车和全部十关的素材版本 |
| `Tools > PolyCar > Validate All Environments` | 检查已生成场景并输出环境截图和报告 |
| `EnvironmentDeliveryBuilder.RefreshVerifiedPreviews` | 在验证后刷新十张关卡预览并保存场景 |
| `EnvironmentDeliveryBuilder.RebuildAndReview` | 重建场景、运行环境检查，再运行十关物理驾驶 |
| `Tools > PolyCar > Enlarge Coins And Review` | 应用金币与驾驶 HUD 尺寸，捕获两种屏幕比例的对比图，并更新 macOS 运行包 |

静态方法可以通过 Unity 命令行 `-executeMethod` 调用。重建与预览捕获需要图形渲染环境，不要给这些任务添加 `-nographics`。不要用两个 Unity 进程同时修改同一个项目目录。

## 验证与证据

最近一次 Bug 修复及 81 项回归结果见 [修复与验证](Review/BugAudit/fixes.md)。

完整说明与已保存结果见 [十关环境实现与验收](Review/Environment/Acceptance.md)。报告将以下几类验证分开记录：

1. **EditMode 回归测试**：在 Unity 的 Test Runner 中运行 EditMode。测试涵盖驾驶输入与漂移、比赛流程与奖励、地形坡度、环境材质，以及真实切关时的光照应用和恢复。
2. **物理驾驶**：`DifficultyValidation.Run` 使用起始车辆与真实 Rigidbody 模拟，按路线和实体障碍规划行驶，通过所有有序检查点；不传送、不救援。结果保存在 [physics-driving.csv](Review/Environment/physics-driving.csv)。
3. **环境与镜头**：`EnvironmentValidation.Run` 检查道路和路肩支撑、路肩回返接缝、场景侵入、模型落地及采样追尾镜头遮挡，并生成每关总览与四个驾驶高度视角。结果见 [环境报告](Review/Environment/environment-report.md) 与 [驾驶视角合图](Review/Environment/all-route-driving-views.png)。
4. **运行包性能**：开发版提供场景渲染遍历模式，报告记录设备、分辨率、预热时间和逐关帧时间，见 [Player](Review/Environment/Player)。

自动驾驶验证物理可通行性；截图与采样检测验证特定视角和位置；渲染遍历使用运动学车辆、隐藏 UI 并关闭 VSync。它们不等同于人工连续试玩，也不代表所有设备或完整交互流程的帧率。重新修改场景后，应重新生成对应证据，而不是沿用旧报告作为新版本结论。

最新界面修正见 [Rank、金币与暂停页检查](Review/InterfaceFeedback/review.md)：Rank 使用与选关页一致的完整 16:9 图片；赛道金币统一为金色，直径比上一版增大约 33%，拾取范围匹配可见尺寸；驾驶 HUD 图标为 68×68、数字字号为 38（参考画布单位）。拾取即时更新本局计数，钱包仍在结算后入账。暂停页已移除玩法说明。

## 构建与运行包

[EnvironmentDeliveryBuilder.BuildPlayer](Assets/Editor/EnvironmentDeliveryBuilder.cs) 使用主游戏场景构建 macOS 开发版，输出到 `Builds/PolyCar-Environments.app`。

macOS 上可从项目根目录执行以下示例（Unity 安装路径应按本机调整；运行前关闭正在占用该项目的 Unity 编辑器）：

```bash
"/Applications/Unity/Hub/Editor/6000.2.7f2/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -projectPath "$PWD" \
  -executeMethod EnvironmentDeliveryBuilder.BuildPlayer \
  -logFile /tmp/polycar-player-build.log
```

构建成功后双击 `.app`，或运行：

```bash
open Builds/PolyCar-Environments.app
```

需要复现渲染测量时，给运行包传入绝对输出目录：

```bash
open Builds/PolyCar-Environments.app --args \
  --environment-benchmark /tmp/polycar-player-benchmark
```

普通启动进入游戏；渲染测量参数用于专门的性能遍历。`Builds` 中的运行包不随源码提交，需要在本机生成。其他平台可在 Unity Build Profiles 中安装相应模块、指定 `DriftPrototype.unity` 并构建，但需自行完成该平台的输入、显示和性能验证。

## 存档

存档由 [SaveManager.cs](Assets/Scripts/SaveManager.cs) 通过 Unity `PlayerPrefs` 读写。实际存储位置依操作系统以及 Player Settings 中的公司名、产品名而定；编辑器与运行包的存储行为也应按各自环境确认。

| 数据 | 键或前缀 |
|---|---|
| 钱包与当前车辆 | `PolyCar_TotalCoins`、`PolyCar_SelectedCar` |
| 已解锁车辆 | `PolyCar_Unlocked_` |
| 当前路线与最高解锁路线 | `PolyCar_SelectedRoute`、`PolyCar_HighestUnlockedRoute` |
| 已完成路线 | `PolyCar_RouteCompleted_` |
| 当前路线版本的最佳记录 | `PolyCar_v2_BestTime_`、`PolyCar_v2_BestRouteDrift_`、`PolyCar_v2_BestRouteCoins_` |
| 音频设置 | `PolyCar_MusicEnabled`、`PolyCar_SoundEnabled` |

路线记录使用 `v2` 前缀，避免旧短路线成绩与重新设计后的路线混用；旧记录保留，钱包、车辆所有权和关卡解锁沿用原键。重建场景不会自动清空存档。调试时如需重置，应明确选择要删除的项目键并先备份需要保留的进度。

## 美术来源与许可

车辆、工业建筑、赛车设施和自然素材来自 Kenney 的 Car Kit、City Kit Industrial、Racing Kit 与 Nature Kit。所用外部美术的许可为 **CC0-1.0**，各素材包附有原始 `License.txt`。

[Sources.json](Assets/ThirdParty/Kenney/Sources.json) 记录素材来源、选用模型及下载归档的 SHA-256；原始模型和许可保存在 [Assets/ThirdParty/Kenney](Assets/ThirdParty/Kenney)。项目将素材转换为 URP 材质并统一尺度，场景中的部分地形、建筑、船只和工程设备由代码生成。第三方素材许可不等同于对整个项目源码另行授予许可。

## 美术提升建议

十关已具有独立主题，但仍有从可玩原型提升到精修画面的空间。以下是后续建议，不代表已经完成：

1. **驾驶视角的场景层次**：优先改善第 6、9 关较空的区域。渔港增加成组的修船作业区和摊位，工厂增加装卸区和检修设施；围绕弯道入口、弯心和出口安排近景、中景、远景，并保留视线与驶出路面的容错空间。
2. **夜间街区的专属建筑组合**：第 10 关增加店面、雨棚、街角招牌和人行道，减少通用工业建筑的重复，强化商业街的空间特征。
3. **自然地形的轮廓变化**：第 4 关打破过于整齐的采石台阶，第 8 关增加不等高、成组的山峰；调整岩层断面和植被疏密，减少规则重复。
4. **材质与光照层次**：第 5 关保留暖色日落，同时增强冷色阴影与海面的对比；夜景加强车辆与背景的分离。通过适量磨损、色差和粗糙度变化区分沥青、沙土、混凝土和金属，保持统一的低多边形风格。
5. **动态表现**：按路面区分轮胎烟尘，增加轻微海浪和旗帜运动，并统一金币、检查点与漂移反馈的视觉语言。

建议先将第 6 关做成精修样板，用三个实际驾驶视角审查构图、可读性和性能，再将确认后的标准扩展至第 9、10 关及其他关卡。每次布景调整后重新检查道路侵入、镜头遮挡与目标设备帧率。

## 后续工作

- 人工连续试玩各关，结合键盘与手柄体验调整漂移手感、障碍节奏和奖牌目标。
- 验证完整手柄菜单导航、字体与对比度、超宽屏和较低分辨率。
- 在目标设备上测试完整比赛流程的性能、阴影与长时间运行表现。
- 完善发动机、轮胎、碰撞音效和简短漂移教学。
- 增加个人幽灵回放与检查点分段用时比较。
- 将稳定后的生成式菜单逐步整理为可复用 UI 预制体。

更多开发细节见 [搭建与玩法说明](Assets/Docs/LowPolyDriftPrototypeSetup.md) 和 [开发状态与后续计划](Assets/Docs/OptimizationRoadmap.md)。
