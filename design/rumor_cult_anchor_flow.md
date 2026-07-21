# 教团情报到锚点进度链路

## 数据流

```text
RumorIntel 配置
  target_overlay_id + event_group_cfg_id + cult_action_id
        |
        v
RumorIntelSystem.TryPurchase
  校验购买地图与 target_overlay_id，一次性扣除情报点
        |
        v
RumorIntelMapSpawn.ApplyPurchasedRumorsOnMapLoaded
  在目标冒险加载后创建唯一 EventGroup，并注入运行时变量
        |
        v
EventGroup 完成输出 ApplyDemonCultAnchorAction
  校验地图、情报仍有效、动作与当前 logic_area 匹配
        |
        v
DemonCultSystem.TryApplyAnchorAction
  按 CultAnchorAction 将进度累计到对应锚点
        |
        v
成功后消费情报并销毁 EventGroup
```

## 配置职责

- `RumorIntel.target_overlay_id`：情报只在指定地图的商店中出现，也只能为该地图购买。
- `RumorIntel.event_group_cfg_id`：指定冒险刷新时生成的事件内容。
- `RumorIntel.cult_action_id`：事件解决后提交给教团系统的动作 ID。
- `RumorIntel.event_expire_days`：事件成功生成后还能保留的结算天数，当前全部配置为 1。
- `CultAnchorAction`：用 `logic_area_id + action_id + anchor_level` 映射目标锚点和本次进度。
- `CultAnchor`：持有建立阈值、区域归属和产出配置，不由情报或事件重复声明。

地图覆盖层、区域变体和逻辑地区是不同身份。当前测试情报配置 `target_overlay_id = village_01`，运行时会将 `village_01_d`、`village_01_n` 归一到其区域变体 `village_01`；锚点累计时再从当前覆盖层解析规范 `logic_area_id = village_01`。

## 生命周期

```text
Purchased --目标冒险加载成功--> Spawned --事件结算成功--> Resolved
    |                              |
    +--购买期限到期后移除          +--事件期限到期后移除
```

- `Purchased`：尚未生成；购买期限到达后移除。
- `Spawned`：事件已生成；记录独立的 `EventExpireSettlementDay`，当前为生成日加 1 天，不会因重复加载而续期。
- 同一探索内存读档或重新加载时复用唯一事件；新探索重建地图后，如果已生成条目找不到原唯一事件，则直接结束，不会再次生成。
- 跨过事件期限后，情报活动条目和 EventGroup 地图记录都会被清理；当前 1 天是单次探索内的最长保留期限。
- `Resolved`：仅在锚点动作成功后消费情报并销毁事件。

## 失败原则

- 地图不匹配、情报不存在、动作未配置、锚点已建立或其他锚点校验失败时，不消费情报，也不销毁事件。
- `ApplyDemonCultAnchorAction` 不回退到旧的教团影响逻辑，避免配置错误被“成功消费情报”掩盖。
- 唯一名 `rumor_event:{rumor_id}` 保证同一地图存档中同一情报最多存在一个有效事件。

## 当前示例与扩展

当前 `rumor_cult_anchor_demo` 是可直接解决的 EventGroup，用于验证整条链路，并仅维持 1 天。后续可在同一 EventGroup 内增加成员、战斗、调查和多阶段状态；最终阶段仍调用 `ApplyDemonCultAnchorAction`，无需改变情报、保存或锚点系统。

正式地图应在 Village_01 对应编辑器场景的 `AreaRoot/NamedPoint` 下配置 `RumorCandidate` 叶节点，并分别重新导出日夜探索使用的 MapExport。在正式点位完成前，运行时会退回玩家位置，便于验证功能但不适合作为最终内容摆放。

## EventGroup 阶段编排

新 EventGroup 可配置 `Stages`；没有阶段配置的旧资产继续使用原有 `InnerTriggers`，两套流程互不干扰。

每个阶段声明：

- 本阶段需要存在的成员；
- 完成条件采用全部满足或任一满足；
- 条件通过成员 ID 或成员标签选择目标；
- 完成时执行的内部交互 ID；
- 下一阶段，或将整个事件标记为完成。

当前类型化条件包括：

- `AllMembersInteractStatus`：目标交互成员全部达到指定状态；
- `AllMembersDefeated`：目标战斗成员全部被击败。

成员状态和击败结果由事件回调更新，并写入 `LogicEntityRecord4EventGroup`。事件组同时保存终态和 `OutcomeClaimed`，防止存读档、多个成员同帧完成或条件重复检查造成二次结算。EventGroup 销毁、完成或过期时会一并销毁仍存在的组成员。

来源产出不由 EventGroup 直接解释。阶段完成交互调用 `ResolveEventGroupOutcome`，再由 `EventGroupOutcomeRouter` 根据运行时 `event_outcome_kind` 分派给来源系统。当前 `rumor_cult_anchor` 处理器负责验证情报、提交锚点动作并消费情报；处理失败时 EventGroup 不进入完成终态。

当前验证模板：

- `rumor_cult_smear_demo`：维护三个 `paint_point` 交互成员，全部进入完成状态后结算；当前教团情报使用此模板。
- `rumor_cult_orc_ambush_demo`：初始只提供调查交互，进入阶段 1 后生成三个 `enemy` Orc，全部击败后结算。
