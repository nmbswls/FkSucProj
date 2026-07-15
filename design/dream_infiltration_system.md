# 入梦玩法约束

## 固定入口

项目中“入梦”专指玩家在 `secretbase` 使用入梦设施进入的小游戏。

固定流程：

1. 玩家在世界中通过任务、关系或其他条件解锁某个 Character 梦境入口。
2. 解锁状态写入持久化数据，不立即启动小游戏。
3. 玩家返回 `secretbase`，与入梦设施交互。
4. `DreamEntryPanel` 展示普通梦境点位和已解锁的 Character 梦境。
5. 玩家在该面板选择入口后进入 `DreamDodgeGameplayPanel`。
6. 结算结果写回对应 Character 的持久化档案。

城镇 NPC 对话、任务选项、地图交互点和过场不得直接打开角色入梦。代码层不提供 Dialogue `OpenCharacterDream` 命令。

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
