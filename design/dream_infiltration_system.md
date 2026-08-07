# 入梦玩法约束

## 固定入口

项目中“入梦”专指玩家在 `secretbase` 使用入梦设施进入的**内嵌弹幕躲避小游戏**（`DreamDodgeGameplayPanel`：移动躲避弹幕/AOE，拾取倾向物攻击核心）。

固定流程：

1. 玩家在世界中通过任务、关系或其他条件解锁某个 Character 梦境入口。
2. 解锁状态写入持久化数据，不立即启动小游戏。
3. 玩家返回 `secretbase`，与入梦设施交互。
4. `DreamEntryPanel` 展示普通梦境点位、已解锁的 Character 梦境，以及当日刷新的抽象团体入口（见 `abstract_group_dream_system.md`）。
5. 玩家在该面板选择入口后进入 `DreamDodgeGameplayPanel`。
6. 结算结果写回对应归属档案：Character → `NpcCharacterPersist`；抽象团体 → 独立 `AbstractGroupPersist`。
7. **任意入口完成一局（胜负均算）并关闭结算后，世界推进至下一天**（夜间 → 白天 + 日结）。

城镇 NPC 对话、任务选项、地图交互点和过场不得直接打开角色入梦或团体入梦。代码层不提供 Dialogue `OpenCharacterDream` / 直开团体梦境命令。

抽象团体（路人与 Character 之间的第三层）的完整需求见：`design/abstract_group_dream_system.md`。

## 入口分层与奖励语义（已定）

世界对象三层仍是 **路人 → 小团体 → Character**。入梦设施内：

- **浅梦路人** = 抽象 `dream_passerby`（不关联 `UnitNpc` / 场景路人），通用小黑人形象 + 品级 `ECommonGrade`（见 `common_grade.md`）。
- **区域** `dream_passerby_region` 只是抽象条件：今晚是否允许从该区域概念刷人；不是可点地点。
- **小团体**与路人混排在地图区；团体 UI 更大且带边框。
- **角色梦境**独立显示在右上角。

交互：点小人选中 → 下方详情 → 点「入梦」开局（选中不占次数）。

| 入口 | 通关奖励语义 | 失败 |
|---|---|---|
| 浅梦路人 `PasserbyEntry` | 表内薄奖：欲望碎片 / 少量精元池 / 少量基础沉沦 | 无奖 |
| 角色梦境 `CharacterEntry` | **只记**尝试/倾向胜负（任务/关系） | 仍记尝试 |
| 小团体 `AbstractGroupEntry` | 阶段奖 + 满阶段秘会 | 不发阶段奖 |

### 每晚显示数量

初版固定 **6**（`DreamPasserbyService.DefaultNightlyDisplayCount`）。  
是否可成长：建议**可成长**，但别太快——一天一局下数量主要影响「选局丰富度」而非刷次。优先挂入梦设施等级或薄属性（如 `DreamPasserbyNightlyCount`），上限建议 8～10，避免地图拥挤盖住团体辨识。

## 时段与日限（已定）

| 规则 | 说明 |
|---|---|
| 仅夜间可入梦 | `GameLogicManager.DayPeriod == Night` 才允许打开入口或开始小游戏；白天交互应提示并拒绝 |
| 入梦后推日 | 关闭结算面板后调用时段推进：`Night → Day` 并触发 `HandleOneDayBalance` |
| 每天只入梦 1 次 | 与推日叠加保证；另以 `DreamDailyLimit.DreamUsedToday` 在**进入小游戏时**置位，防止同夜中途退出后再开 |
| 中途取消 | 从玩法退回入口仍占用今日次数，**不推日**；关闭入口面板不占用次数 |

推日后玩家处于白天，需再等到夜间才能再次入梦。日结时清 `DreamUsedToday`，并刷新抽象团体当日入口。

## Character 入口

Character 梦境使用 `TbCharDreamEntryInfo`：

- `character_key`：结果归属的角色。
- `priority`：同角色多层梦境的顺序。
- `has_locals`：入口出现前必须存在的角色局部状态。
- `no_locals`：入口出现前必须不存在的角色局部状态。
- `show_conds`：入口显示必须满足的通用条件，可直接查询任务阶段等既有事实。

任务阶段限定的梦境入口应使用 `show_conds` 直接查询 `TaskStep`，不要再用 Character local switch 镜像任务状态。只有真正属于角色自身、独立于任务生命周期的解锁事实，才使用 `has_locals/no_locals`。入口解锁和小游戏执行是两个阶段，不能合并成一个对话命令。

## 任务 Objective

入梦任务 Objective 读取 Character 持久化结果：

```text
character_key + char_dream_entry_id + result_requirement
```

它不监听一次性的“入梦结束”事件，也不由梦境系统直接调用任务 API。事件只能提示界面刷新，任务完成事实必须来自持久化查询，因此任务初始化、自动推进和读档恢复使用同一数据源。

支持的结果条件：

- `AnyAttempt`
- `AnyWin`
- `ForceWin`
- `SoothingWin`
- `TrickWin`

## 结果存储

结果保存在：

```text
PlayerData.NpcCharacterPersistByKey[character_key]
  .DreamEntryWinCounts[char_dream_entry_id]
```

当前记录尝试次数以及三种倾向胜利次数。旧存档没有尝试次数时，以已有胜利次数兼容推导最小完成量。

## 小游戏形态

- UI 内嵌弹幕躲避：玩家在 `PlayArea` 移动，躲避直线弹与 AOE，拾取暴力/安抚/计谋倾向物后向核心发射弧形炮弹。
- 常规通关：核心 HP 归零；失败：玩家 HP 归零。
- 抽象团体额外可用阶段表 `required_score`：核心摧毁且对核心总伤害 ≥ 目标分才算通关（见团体文档）。
- 难度由入口上下文写入 `DreamGameplayContext`（主题、核心/玩家 HP、弹伤等）；团体入口从阶段表读取。
