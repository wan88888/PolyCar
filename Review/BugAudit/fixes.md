# Bug 修复与验证

2026-11-10，本轮检查发现的两项 Bug 及一处风险均已处理。

## 修复内容

1. **障碍物闪烁中断**：`ObstacleFeedback.OnDisable` 调用统一复位入口，停止闪烁协程并恢复尺寸、颜色和碰撞冷却。`LevelManager` 加载或重开关卡时也复位该关所有障碍物，覆盖不切换根物体的重开流程。尚未 Awake 的非活动对象不会被缩放为零。
2. **验收脚本覆盖选关存档**：`InterfaceFeedbackReview.Run` 使用 try/finally 保存和恢复 `PolyCar_SelectedRoute`；原本不存在的键不会被留下。正常完成、异常退出均恢复原值，并恢复调用前的 Time.timeScale。
3. **预览灯误选风险**：车库预览灯的 GameObject 和照射范围均隔离在 layer 31。`SceneLook` 优先使用有效的 RenderSettings.sun，回退查找时排除禁用灯、预览层灯及只照射预览层的灯。

## 验证结果

- **81/81 EditMode 测试通过**，包括新增 8 个案例：闪烁复位的尺寸/颜色/冷却、选关键存在与不存在、成功与异常退出、有效与无效首选太阳。见 [测试结果](fixed-editmode-results.xml)。
- **Unity Play Mode 验证通过**：碰撞后尺寸为 `(1.12,1.12,1.12)`，停用后重新启用并等待，恢复为 `(1,1,1)`；十关切换均选中实际场景太阳。见 [运行日志](fixed-runtime.txt)。
- **完整界面验收与 Mac 构建通过**：将测试选关设为索引 7，实际执行包含截图及构建的完整验收后仍为 7；外层保护恢复用户原始选择。见 [交付检查](fixed-delivery.txt)。
- **运行包已更新**：`Builds/PolyCar-Environments.app`。

这些验证覆盖本轮发现的问题，不代表所有设备和所有玩家操作路径都已穷尽验证。
