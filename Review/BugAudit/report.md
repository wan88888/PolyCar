# 当前项目 Bug 检查

检查日期：2026-11-10。以下保留原始检查结果；两项已确认 Bug 及主光源选择风险现已修复，见 [修复与验证](fixes.md)。

## 已确认

### P2：碰撞闪烁中停用障碍物，重新进入关卡后尺寸和颜色不恢复

- 位置：`Assets/Scripts/ObstacleFeedback.cs:84–90`。
- 触发：撞到障碍物，在 0.08 秒闪烁结束前停用其对象或所属关卡。例如碰撞后立即暂停、退出并切换关卡，之后重新进入。
- 原因：恢复尺寸与颜色只写在协程等待之后；GameObject 停用会终止协程，没有 OnDisable/OnEnable 复位。
- 实测：使用实际 ObstacleFeedback 组件和运行时停用/启用生命周期，初始尺寸 `(1,1,1)`，碰撞后为 `(1.12,1.12,1.12)`，重新启用并等待后仍是 `(1.12,1.12,1.12)`。缩放施加在根物体上，因此碰撞体也会变大。
- 建议：在停用或重置关卡时统一恢复尺寸、材质属性与冷却状态。

### P2（开发工具）：界面验收脚本覆盖当前选关存档

- 位置：`Assets/Editor/InterfaceFeedbackReview.cs:51` 调用 `LoadLevel(0,true)`；`Assets/Scripts/LevelManager.cs:274–275` 立即调用 `SaveManager.SetSelectedRoute`。
- 触发：存档选中其他关卡后执行 `InterfaceFeedbackReview.Run`。
- 结果：验收结束或中途抛异常后，`PolyCar_SelectedRoute` 留在 0；重新打开游戏会默认选第 1 关。重开场景不会恢复 PlayerPrefs。
- 证据等级：代码路径确认；本轮未为此重复运行会生成场景与构建的验收工具。
- 建议：运行前备份相关 PlayerPrefs，在 finally 中恢复。该问题影响开发验收，不影响普通玩家单独运行游戏包。

## 待验证风险

### 主光源查找可能误选车库预览灯

`SceneLook.Apply` 用第一个非 layer 31 的方向光作为太阳，但 `GaragePreview.Start` 创建的 Studio key 物体仍在默认 layer 0，只把灯光的 cullingMask 设为 layer 31。两者筛选条件不一致，查找顺序也未固定。本轮实际运行中十关均选中了正确的 Directional Light，因此不列为已复现故障。建议明确引用场景太阳，或统一预览灯的隔离方式。

## 验证范围与结果

- 当前代码的 **73 项 EditMode 测试全部通过**，见 [测试结果](editmode-results.xml)。现有测试未覆盖上述障碍物中途停用生命周期和验收脚本的存档恢复。
- 在 Unity Play Mode 中启动实际主场景，逐关切换并记录太阳引用；十关均正确。
- 运行时复现障碍物闪烁中断，原始结果见 [runtime.txt](runtime.txt)。诊断脚本备份见 [ProjectBugAudit.cs.txt](ProjectBugAudit.cs.txt)，已从 Assets 中移出。
- 暂停追尾镜头的零帧时间问题没有复现，平滑速度保持有限值，未列为 Bug。
- `ResetCar` 会清空禁用控制时的刹车输入，但正常开始流程的 `BeginRun` 会重新施加刹车；目前没有确认玩家可触发的倒计时溜车故障，未列为 Bug。
- 本轮没有完成十关人工连续通关，也没有验证全部设备和分辨率；测试通过不代表项目完全无 Bug。
