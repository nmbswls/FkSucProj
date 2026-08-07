# 前期 home_01 任务、对话与 NPC Routine 基准

本文是前期任务内容、NPC 对话职责和村子重建前后站位的统一设计基准。

## 1. 前期任务范围

正式前期任务分为三段：

| 阶段 | 任务 | 作用 | 当前状态 |
|---|---|---|---|
| 地牢外卡莱尔线 | `300` 临时包扎、`301` 清理退路、`302` 简易担架 | 让玩家救下卡莱尔，并完成他从昏迷到苏醒的切换 | 已有任务目标、卡莱尔专用对话和条件，需继续核对正式文本与站位 |
| home_01 收复与重建 | `200` 收复拼合村、`201` 火堆盟约、`202` 清点活人、`203` 锅不能灭、`204` 保住灰露草、`205` 干净一点再见人、`206` 夜路要有人看、`207` 村东林缘调查、`208` 带回雾线样本、`209` 林缘立足、`210` 不属于森林的脚印 | 完成救援、恢复基本生活、开放森林路线 | 已有任务骨架和部分变量，正式对话、角色归属和场景调度未完成 |
| 森林前置 | `211` 北路花露、`212` 心木回声 | 把 home_01 从临时据点推进到可继续探索的前哨 | 已有任务与部分对话，角色归属仍需从埃蒙拆分到薇拉/艾文 |
| 可选角色支线 | `213` 水盆里的旧影 | 薇拉入梦与关系线的第一条完整支线 | 已接入 Luban 和 Secret Base 入梦入口 |

`100-102` 是早期测试/教程占位任务，不纳入正式 home_01 前期内容；`300-302` 虽然编号较高，但属于进入村庄主线前的正式前置。

## 2. NPC 与 prefab 盘点

### 已有 InitialVillage prefab

以下 prefab 已存在，可以直接作为正式角色表现基础：

| 角色 | CharacterKey | prefab | 当前场景状态 |
|---|---|---|---|
| 埃蒙 | `home_elder` | `InitialVillage_Emon.prefab` | 已在 `Home_01_Editor` 放置，CfgId 为 `homestead_elder` |
| 灰露棚看管人/杜兰 | `home_merchant` | `InitialVillage_Chef.prefab` | 已在 `Home_01_Editor` 放置，CfgId 为 `homestead_merchant` |
| 艾文 | `home_evan` | `InitialVillage_Boy.prefab` | prefab 已有，尚未接入 home_01 正式场景 |
| 赫恩 | `home_hearn` | `InitialVillage_NightWatchman.prefab` | prefab 已有，尚未接入 home_01 正式场景 |
| 麦穗 | `home_dog_maisui` | `InitialVillage_YellowDog.prefab` | prefab 已有，尚未接入 home_01 正式场景 |
| 薇拉 | `home_vera` | `InitialVillage_Vera.prefab` / `vera_civil.prefab` | 已在 `Home_01_Editor` 放置 |
| 卡莱尔 | `carlisle` | `game_init_luca_injured.prefab` 及现有 awake 配置 | 已用于 game_init、test_link_a；收复后由 `NpcRoutineBinding` 使用 `game_init_carlisle_awake` 在 home_01 创建虚弱、冷战常驻状态 |

当前 `Home_01_Editor`/`homestead_01.asset` 中已经落地的关键 NPC 包括埃蒙、灰露棚看管人、薇拉和收复后的卡莱尔。卡莱尔不是场景 `DynamicEntityExportGenerator`，而是由 `npc_routine.xlsx` 的 Binding/Profile/Rule 在 `Home_01 + homestead_01` 初始化时创建，并锚定到 `home_carlisle_recovery`。艾文、赫恩和麦穗仍需按各自出现条件完成正式场景接入。

## 自我封印主线接入

自我封印不占用任务 id，也不新增“仪式任务”。它发生在任务 `204` 灰露草交付后的剧情停顿点；此时下一环 `205` 已引导玩家去找埃蒙，由地图必经 Trigger 和三段 Dialog 串联：

1. `home_01_self_seal_collapse`：灰露棚与埃蒙站位之间的窄长 Trigger 检查 `TaskFinish 204`。触发带不得覆盖灰露草交付 NPC 的交互站位，避免玩家仍在任务面板中时抢播；玩家向埃蒙移动时播放莉莉丝 `knocked_down` 动作，写入 `home_01.self_seal_plan_known`，随后静默传送到 `homestead_01 / bedroom_in_entry`。
2. `home_01_self_seal_briefing`：房间 Trigger 在玩家抵达后播放咕啾说明，写入 `home_01.self_seal_briefed`，引导玩家寻找卡莱尔。
3. `home_01_self_seal_carlisle`：以高优先级 NPC 对话绑定到 `game_init_carlisle_awake`。H 正文和演出保留空位；收尾先写入 `player.human_form_unlocked`，再切换到 Human。

三个阶段只使用持久化 Global Switch 控制可重入和完成状态。正式解锁结果只有 `player.human_form_unlocked`，不再增加 `self_seal_complete` 等重复变量。`entry_01` 在该开关存在前不生成，确保玩家不能绕过仪式进入晨钟镇南北道。

## 3. 任务与对话归属

### 卡莱尔

- `300`：接受包扎，保持防备，不主动讲述过去。
- `301`：说明退路和附近威胁，只给可执行信息。
- `302`：接受担架，短暂恢复意识；醒来后对莉莉丝保持礼貌和距离。
- `D01`：村外营地初见，卡莱尔把莉莉丝当作危险但暂时有用的同行者。
- `D02`：苏醒后解释人类聚落的基本规则，避免把他写成教学旁白。
- `D03`：收复村庄后被动提供防务判断，承认村民需要帮助，但不主动请求留下。
- `D04`：火堆盟约中只接受最低限度的合作，不公开评价莉莉丝的善恶。
- `D05`：防务任务中开始主动提出站位和巡逻建议，关系推进来自行动而非表白。

### 埃蒙

- `201`：代表幸存者在火堆边感谢莉莉丝，同时明确村民仍然害怕她。
- `202`：清点活人，提供村民、伤员和失散物资的事实信息。
- `207`：确认村东旧路和临时标记，开放村东通路。
- `208`：接收雾线样本，判断森林路线是否可继续使用。
- `211`：只在花露样本异常时做最终判断，不再作为薇拉的日常任务发布者。
- `212`：接收艾文的记录，判断森林之心不是普通自然现象。
- 重建前语气：谨慎、短句、先问损失和人数。
- 重建后语气：开始安排分工，但仍拒绝使用“村长”或“领主”称谓。

### 杜兰

`home_merchant` 继续使用现有配置 key，但显示名应从“灰露棚看管人”逐步明确为杜兰。

- `203`：收集木料并恢复公共火堆。
- `204`：抢救灰露草，说明预处理比采集本身更重要。
- `209`：森林入口清理后提供第一份补给，并解锁普通森林食材需求。
- `D06`：火堆盟约中主动递出热食，是第一个用实际行为回应莉莉丝的人。
- 重建前语气：只关心燃料、锅和能不能撑过今晚。
- 重建后语气：开始考虑明天的产量、保存和交换，不再只处理眼前火堆。

### 薇拉

- `205`：负责临时住处、衣物和伤员清洁，不再交给埃蒙发布。
- `211`：负责花露容器、采集注意事项和交付；她是花露线的实际知识来源。
- `213`：接取“水盆里的旧影”，通过 Secret Base 入梦设施进入梦境。
- `D07`：第一次主动询问莉莉丝是否需要清理衣物或伤口，关系推进保持克制。
- 重建前语气：动作明确、避免亲近、优先处理伤员。
- 重建后语气：开始安排生活空间，偶尔主动照顾莉莉丝，但不将其写成无条件信任。

### 赫恩

- `206`：负责路标、夜间信号和村外警戒，不再绑定到埃蒙。
- `210`：发现人类脚印并解释偷猎者的行动路线；任务完成后报告风险仍可能回来。
- `D08`：收复后的第一次正式交谈，承认自己只能示警，不能和卡莱尔一样训练民兵。
- 重建前语气：寡言、报告事实、避免承诺。
- 重建后语气：开始主动安排巡逻班次，对村外路线有明确意见。

### 艾文

- `M05/D09`：作为逃出村庄的求援者报告 home_01 被占领，不承担复杂任务说明。
- `208`：负责记录雾线样本编号和采样位置。
- `212`：记录森林之心的反应，最终把记录交给埃蒙判断。
- `D10`：苏醒后第一次完整对话，解释敌人搜刮灰露草和食物的动机。
- 重建前语气：伤弱、事实化、害怕遗漏信息。
- 重建后语气：开始像学徒一样工作，负责样本、简图和森林观察记录。

### 麦穗

麦穗不说人话，不承担正式任务发布、物品提交或成人内容。

- `D11`：通过吠叫、回头和拉扯衣角把玩家引向艾文。
- `207`：带玩家寻找旧路标，但任务文本不显示麦穗的对白。
- `210`：在补给点附近持续朝森林方向示警，帮助玩家理解脚印方向。
- 重建前站在村外入口、幸存者和艾文之间。
- 重建后回到村内公共区域和东侧道路之间巡回。

## 4. routine profile 与站位

home 常驻 NPC 不使用逐个 NPC 的 `DynamicEntityExportGenerator` 作为生成入口。它们属于地图人口，由 routine 系统在进入对应 map variant/overlay 时根据 `NpcRoutineProfile` 和 `NpcRoutineBinding` 创建；`NamedPoint` 是 routine 的站位锚点，不是 NPC refresh generator 的出生点。

当前 `NpcRoutineSystem` 已具备完整的人口创建入口：地图初始化调用 `EnsureConfiguredNpcsCreated()`，它按 `NpcRoutineBinding` 的 map variant、overlay、character key 和 NPC cfg 创建缺失的 NPC record，再由 `ApplyInitialPlacement()` 与现有 Rule 处理初始站位。场景动态生成器不是 home 常驻人口的来源。

常驻 NPC 的正确链路是：

```text
NpcRoutineBinding
    -> 当前 map variant + overlay + CharacterKey
    -> NpcRoutineProfile / fallback rule
    -> 创建或恢复 LogicEntityRecord4Npc
    -> NamedPoint 初始站位
    -> NpcRoutineSystem Tick 处理日常移动
```

### 需要新增的 NamedPoint

以下名字是稳定的场景契约，需在 `Home_01_Editor.unity` 的 `NamedPoint` 下创建，并在 fight 版/重建版分别放到对应区域：

| NamedPoint | 用途 |
|---|---|
| `home_pre_survivor_fire` | 重建前幸存者临时火堆 |
| `home_pre_east_watch` | 重建前村东临时警戒点 |
| `home_pre_herb_shed` | 重建前灰露草棚/抢救点 |
| `home_pre_injured_shelter` | 重建前伤员和临时住处 |
| `home_post_fire` | 重建后公共火堆 |
| `home_post_herb_shed` | 重建后灰露草棚 |
| `home_post_watch` | 重建后村外巡逻点 |
| `home_post_east_path` | 重建后村东入口 |
| `home_post_sample_table` | 重建后样本记录处 |
| `home_post_vera_room` | 重建后薇拉的生活/清洁区域 |

### 重建前 profile

绑定建议：`map_variant = Home_01`，`overlay_id = home_01_fight`。

| NPC | profile | 主要站位 |
|---|---|---|
| 埃蒙 | `home01_emon_pre` | `home_pre_survivor_fire`，负责清点幸存者 |
| 杜兰 | `home01_duran_pre` | `home_pre_herb_shed`，不离开火堆和物资点 |
| 薇拉 | `home01_vera_pre` | `home_pre_injured_shelter`，处理伤员 |
| 赫恩 | `home01_hearn_pre` | `home_pre_east_watch`，短距离巡回 |
| 艾文 | `home01_evan_pre` | `home_pre_survivor_fire` 或救援事件专用点；只在求援后出现 |
| 麦穗 | `home01_maisui_pre` | 村外入口与 `home_pre_east_watch` 之间巡回 |

重建前不把 NPC 放进完整生活路线；他们的移动半径小，站位围绕“活下来、清点、警戒、救伤员”展开。

### 重建后 profile

绑定建议：`map_variant = Home_01`，`overlay_id = homestead_01`。

| NPC | profile | 主要站位 |
|---|---|---|
| 埃蒙 | `home01_emon_post` | `home_post_fire` 与 `home_post_sample_table`，白天处理记录，夜间回火堆 |
| 杜兰 | `home01_duran_post` | `home_post_herb_shed`，白天处理灰露草，夜间回 `home_post_fire` |
| 薇拉 | `home01_vera_post` | `home_post_vera_room`，白天在伤员/生活区工作，夜间回房间 |
| 赫恩 | `home01_hearn_post` | `home_post_watch` 与 `home_post_east_path` 之间巡逻 |
| 艾文 | `home01_evan_post` | `home_post_sample_table`，白天整理记录，夜间回火堆 |
| 麦穗 | `home01_maisui_post` | `home_post_fire` 与 `home_post_east_path` 之间巡回 |

每个 profile 至少包含 `Day`、`Night` 和 `Any` fallback 三类规则。任务中的临时站位可以用更高优先级的 `TaskStep` 条件规则覆盖，但不应把普通站位写进任务代码。

## 5. 实现顺序

1. 修正 `character_key` 和任务归属：增加 `home_evan`、`home_hearn`、`home_dog_maisui`，将 `205/206/211/212` 从错误的 `home_elder` 绑定拆开。
2. 将正式对话按角色拆分，先完成 `201-212`，再补齐 `300-302` 的文本一致性。
3. 在两个 home_01 editor scene 创建 NamedPoint；不为常驻 NPC 增加逐个 refresh generator。
4. 扩展 `npc_routine.xlsx` 的 profile/rule/binding，分别绑定 `home_01_fight` 与 `homestead_01`。
5. 将 routine 系统扩展为可按 binding 创建/恢复 home NPC record，再检查存档、动态表现和站位切换。

验收标准：任务发布人、Objective 提交人、对话角色、场景中实际 NPC 和前后状态站位必须一致；仅有任务行或 prefab 文件不能视为内容完成。
