# 抽象团体（小团体）需求文档

## 0. 状态

- 需求定稿；实现按本文与 `dream_infiltration_system.md` 推进。
- 与现有「秘会 `CultSecretUnit`」区分：秘会是教团可派遣经营单位；抽象团体是入梦侧的第三层对象；**打满团体全部阶段后获得 1 个秘会**。

## 1. 定位与分层

在现有两类对象之间增加一层封装：

```text
路人（场景泛用 NPC）
  - 可交互 / 可榨取
  - 无独立档案进度（或仅有极短生命周期状态）
  - 不进入入梦入口列表

抽象团体 / 小团体（本需求）
  - 不上图当具名角色
  - 有独立落盘进度（阶段、胜负）
  - 通过 secretbase 入梦设施进入小游戏
  - 多阶段；阶段决定难度、出现条件、通关分数与小奖励
  - 全阶段完成后转化为 1 个秘会

具名 Character
  - character.xlsx + NpcCharacterPersist
  - TbCharDreamEntryInfo 多层梦境
  - 对话 / 任务 / 好感 / 入梦结果归属角色
```

硬约束（继承 `dream_infiltration_system.md`）：

1. 入梦只从 `secretbase` 入梦设施 → `DreamEntryPanel` 选择入口。
2. 世界对话 / 任务选项 / 地图交互 **不得**直接启动团体入梦。
3. 任务 / 教团查询只读持久化结算结果，不依赖一次性事件。
4. **仅夜间可入梦**；完成一局后**推进至下一天**。
5. **每天只允许入梦 1 次**（所有入口类型共用；进入小游戏即占用）。

## 2. 核心概念

| 概念 | 说明 |
|---|---|
| `group_id` | 团体稳定主键（string） |
| 阶段 `stage` | 从 1 递增的整数；表示该团体当前进度档 |
| 阶段配置 | 每 `(group_id, stage)`：难度、出现条件、权重、通关分数、主题 |
| 阶段奖励子表 | 通关小奖励与该关卡（阶段）绑定 |
| 当日刷新槽 | 全局每日最多刷新 **1** 个团体入口 |
| 全日入梦次数 | **每天只入梦 1 次**（团体 / Character / 设施点位共用；进入即消耗） |
| 落盘档案 | 每 `group_id` 一份独立 persist，不挂在 Character 上 |

## 3. 配表需求（Luban / Excel）

### 3.1 `abstract_group`（团体主表）

| 字段 | 类型 | 说明 |
|---|---|---|
| `group_id` | string | 主键 |
| `display_name` | string | 面板显示名 |
| `desc` | string | 简介 |
| `icon_path` | string | 可选图标 |
| `max_stage` | int | 最高阶段 |
| `tags` | list\<string\> | 可选：区域/精型/叙事标签 |
| `unlock_conds` | list\<CommonCheckCond\> | 团体是否进入候选池 |
| `retired_after_max` | bool | 打满后永久离开刷新池（默认 true） |

### 3.2 `abstract_group_stage`（阶段表）

主键：`group_id + stage`

| 字段 | 类型 | 说明 |
|---|---|---|
| `group_id` | string | |
| `stage` | int | 从 1 起 |
| `display_name` | string | 阶段标题 |
| `desc` | string | 阶段说明 |
| `appear_conds` | list\<CommonCheckCond\> | 该阶段可被今日刷新抽中的条件 |
| `refresh_weight` | int | 当日随机权重，≤0 不参与 |
| `dream_theme_id` | string | 入梦主题 |
| `dream_theme_display_name` | string | 主题显示名 |
| `difficulty_id` | string | 难度档标签（文档/检索用） |
| `required_score` | int | 通关所需对核心总伤害；≤0 则仅要求摧毁核心 |
| `core_max_hp` | int | 小游戏核心 HP |
| `player_max_hp` | int | 玩家 HP |
| `bullet_damage` | int | 直线弹伤害 |
| `aoe_damage` | int | AOE 伤害 |
| `projectile_damage` | int | 弧形炮弹伤害 |
| `reward_preview_desc` | string | UI 预览文案（可选） |

难度采用阶段表内嵌数值（方案 B），避免首期再拆 `dream_difficulty` 表。

### 3.3 `abstract_group_stage_reward`（阶段通关小奖励子表）

入梦通关奖励与**该入口关卡（团体阶段）**绑定。

主键建议：`group_id + stage + reward_index`

| 字段 | 类型 | 说明 |
|---|---|---|
| `group_id` | string | |
| `stage` | int | |
| `reward_index` | int | 同阶段多行 |
| `item_id` | string | 通关发放物品（可空） |
| `item_count` | long | |
| `faith` | long | 可选薄信仰 |
| `jingyuan` | long | 可选：入精元池的普通精元 |
| `fallen_base_amount` | long | 可选：增加基础沉沦人数（非扩散） |
| `only_first_clear` | bool | 通常 true（阶段只通一次） |

规则：

- **通关**才发阶段小奖励；失败不发。
- 奖励只读本阶段子表，不与 Character 梦境奖励表混用。
- 小游戏表现由阶段难度字段决定；**结算发奖以阶段子表为准**。

### 3.4 不与 Character 梦境表混用

- 不复用 `TbCharDreamEntryInfo.character_key` 硬塞假角色。
- `DreamEntrySourceKind` 增加 `AbstractGroupEntry`。
- 结算写入团体档案，不写 `NpcCharacterPersistByKey`。

## 4. 落盘数据（Persist）

```text
PlayerData.AbstractGroupById[group_id] -> AbstractGroupPersist
PlayerData.AbstractGroupDailyRefresh
PlayerData.DreamDailyLimit
```

### 4.1 `AbstractGroupPersist`

| 字段 | 说明 |
|---|---|
| `GroupId` | |
| `CurrentStage` | 当前进度（默认 1；通关后 +1） |
| `HighestClearedStage` | 已通关最高阶段 |
| `AttemptCountByStage` | 每阶段尝试次数 |
| `ClearCountByStage` | 每阶段通关次数 |
| `BestScoreByStage` | 可选 |
| `LastRefreshSettlementDay` | 可选，防连抽 |
| `Retired` | 永久移出刷新池 |
| `SecretUnitGranted` | 是否已因本团体发放过秘会（防重复） |

### 4.2 全局日刷与日限

```text
PlayerData.AbstractGroupDailyRefresh
  .SettlementDayIndex
  .SelectedGroupId
  .SelectedStage

PlayerData.DreamDailyLimit
  .SettlementDayIndex
  .DreamUsedToday
```

日结（`HandleOneDayBalance`）：清 `DreamUsedToday`，并重新 roll 团体入口。  
打开 `DreamEntryPanel` 时若日刷过期，也可懒刷新。

与入梦推日的关系：完成一局关闭结算后会跨日结，因此日刷会在推日时更新；次日夜间打开入口看到新的团体槽。

## 5. 刷新规则

### 5.1 时机

- 结算日开始（日结钩子），或打开 `DreamEntryPanel` 发现日刷过期时 roll。
- 当日最多 **1** 个团体入口。

### 5.2 候选与加权

对每个已解锁且未 Retired 的团体：

1. `stage = CurrentStage`（未建档视为 1）
2. 查阶段行；校验 `appear_conds`、`refresh_weight > 0`
3. 加权随机抽 1 个；候选空则今日无团体入口

可选：连续 N 日不抽同一 `group_id`（首期不做）。

### 5.3 每日只入梦一次（已定）

- 全局每天只允许入梦 **1** 次（设施 / Character / 团体共用）。
- 点进任一入口开始小游戏 → `DreamUsedToday=true`。
- **不存在失败后重刷入口的问题**：次数在进入时已耗尽；且完成局后推日。
- 若今日已入梦，面板其余入口不可点。

### 5.4 阶段推进与秘会（已定）

通关（摧毁核心，且若 `required_score>0` 则总伤害 ≥ 之）时：

1. 发该阶段子表小奖励
2. `CurrentStage += 1`（封顶 `max_stage`；已满则保持）
3. 更新 `HighestClearedStage`
4. 若 `HighestClearedStage >= max_stage` 且尚未 `SecretUnitGranted`：
   - 若 `retired_after_max`：`Retired = true`
   - `TryAcquireSecretUnit(sourceId: "abstract_group:{group_id}")`
   - `SecretUnitGranted = true`（席位策略首期：直接获取，与现有 API 一致）

失败：不发奖、不掉阶段、已占用今日入梦次数；完成局仍推日。  
下一阶段若要继续挑战：须等之后某日再次刷到该团体。

## 6. 入梦入口接入

### 6.1 面板分区

1. 地图区：抽象浅梦路人（日刷约 6）与今日小团体（0 或 1）混排；团体更大带边框
2. 右上角：具名 Character 梦境（独立）
3. 下方详情：选中后显示，再点入梦

### 6.2 上下文

| 字段 | 说明 |
|---|---|
| `EntrySource = AbstractGroupEntry` | |
| `AbstractGroupId` | |
| `AbstractGroupStage` | 日刷锁定阶段 |
| 难度 / 目标分 | 来自阶段表 |

进入时记 `DreamUsedToday`；结算写团体档案并发奖/推阶段/满阶段发秘会。

### 6.3 入口并存但次数共用（已定）

- 面板可同时列出多类入口。
- **每天只开一局**：任选其一后全部锁定至次日（推日后为新的一天）。

## 7. 已拍板 vs 仍可后补

### 7.1 已拍板

| 项 | 结论 |
|---|---|
| 通关奖励 | 绑定入口关卡；`abstract_group_stage_reward` |
| 秘会 | 打完该团体**全部阶段**后获得 1 个 |
| 每日次数 | 每天只入梦 1 次；夜间限定；完成后推日 |
| 玩法形态 | 内嵌弹幕躲避小游戏 |
| 席位满 | 首期直接 `TryAcquireSecretUnit`（与现有教团 API 一致，无额外闸） |

### 7.2 仍可后补

- 面板称呼 / 进门前短 dialog
- CommonCond 团体查询（任务门控时再加）
- HUD 红点
- GM 强制今日团体
- 首期内容量（建议 2～3 团体 × 3～5 阶段）
- 秘会席位满时的补偿/排队策略细化

## 8. 非目标（首期不做）

- 团体上大地图当对话 NPC
- 团体好感 / 送礼
- 伪 `character_key` 冒充 Character
- 对话直开团体梦境
- 每阶段直接给秘会

## 9. 实现切片

1. Excel 三表 + `__tables__` + JSON/CodeGen
2. Persist + 日刷 + 全局每日 1 次入梦 + 夜间/推日
3. `AbstractGroupEntry` + Panel 条目
4. 结算：阶段奖 + 推进 + 满阶段秘会
5. （可选）Cond / 红点

## 10. 验收标准

1. 团体独立存档，读档阶段不丢。
2. 日刷最多 1 个团体；权重与条件生效。
3. 仅夜间可入梦；每天只能入梦 1 次；完成后推日。
4. 通关发阶段子表奖励；失败不发但占次数且仍推日。
5. 打满全部阶段获得 1 个秘会，且不重复发。
6. 仅 secretbase 入梦设施可进。
