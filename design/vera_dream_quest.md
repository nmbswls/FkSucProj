# 薇拉入梦任务：水盆里的旧影

## 剧情位置

- 任务 ID：建议 `213`
- 任务名：水盆里的旧影
- 发布角色：薇拉，`character_key = home_vera`
- 前置：完成 `205 干净一点再见人`
- 入梦入口：`char_dream_entry_id = 101`
- 任务性质：薇拉个人任务，也是角色入梦与任务系统第一次正式联动验证。

## 剧情内容

收复村庄后，薇拉负责清理伤员、衣物和临时住处。她做事仍然利落，却开始反复确认已经洗净的水盆，偶尔会对不存在的铃声作出反应。

她过去侍奉的贵族宅邸在一次撤离中失去秩序。当时她仍被要求维持灯火、热水和整洁，仿佛只要礼仪没有崩坏，主人们就还会回来。她理性上知道那段生活已经结束，身体却仍在梦里重复当时的动作。

薇拉不会把这件事说成求救。她只承认自己最近无法安睡，并允许莉莉丝进入梦境确认原因。莉莉丝的动机既有观察人类梦境的兴趣，也有对薇拉实际能力的认可。

梦境视觉主题：过长的宅邸走廊、不断重新变脏的水盆、无人回应的召唤铃、门缝后已经熄灭的灯。首期仍复用现有入梦小游戏，不新增专用玩法规则。

## 任务步骤

### 213_s1 进入薇拉的梦境

- 接取成功后，任务进入 `213_s1`；梦境入口直接查询该任务阶段，不写额外角色开关。
- 玩家离开 home_01，返回 `secretbase`，使用基地内的入梦设施。
- 入梦设施界面读取 `TbCharDreamEntryInfo`，显示已经解锁的“薇拉的梦境”。
- 玩家在设施界面选择该入口后，执行角色入梦入口 `101`。
- Objective 类型：`CharacterDream = 7`。
- 参数：`obj_p0 = 101`，`obj_p1 = AnyWin(1)`，`obj_p5 = home_vera`，`obj_progress = 1`。
- Objective 每次读取 `WorldNpcCharacterPersistRegistry.GetDreamEntryResultCount()`，不消费结算事件。
- 入梦失败会增加尝试次数，但不会完成本环；任意倾向胜利一次后自动进入 `213_s2`。

### 213_s2 梦醒后的谈话

- 目标：与薇拉交谈。
- 薇拉不需要突然袒露全部过去，只确认梦中那栋宅邸已经没有人在等她维持原状。
- 最终结果：设置 `home_01.vera_dream_done`，完成任务。
- 关系结果：薇拉开始主动协助莉莉丝处理清洁、衣装和进入人类聚居地前的准备，但仍保留自己的判断和边界。

## 数据流

```text
入梦结算
  -> RecordDreamEntryResult(home_vera, 101, won, tendency)
  -> WorldNpcCharacterPersistRegistry
  -> NpcCharacterPersistData.DreamEntryWinCounts
  -> SaveData.PlayerData.NpcCharacterPersistByKey

QuestObjectiveRuntime.GetCurrProgress()
  -> GetDreamEntryResultCount(home_vera, 101, AnyWin)
  -> 返回持久化累计值
  -> 自动步骤完成并进入下一环
```

结算事件至多用于通知 UI 刷新，不能作为任务完成事实。玩家在任务期间保存并读档后，Objective 应从角色数据源重新得到正确结果。

## Secret Base 入口配置

`CharDreamEntryInfo`：

```text
id            = 101
character_key = home_vera
priority      = 1
has_locals    =
no_locals     =
show_conds    = TaskStep,213,0,0,0,213_s1,0
```

上例中的 `has_locals` 应为空；薇拉的接取成功对话不写角色局部状态，也不启动小游戏。`DreamEntryPanel` 在 secretbase 入梦设施打开时枚举 `TbCharDreamEntryInfo`，通过 `show_conds` 查询任务是否正处于 `213_s1`，满足时生成“薇拉的梦境”入口。梦境胜利后任务自动进入 `213_s2`，入口随任务阶段变化自然隐藏。

## 对话资源

- `vera_dream_accept`：薇拉说明睡眠异常并允许入梦。
- `vera_dream_accept_ok`：接取成功确认，不写镜像任务状态。
- `vera_dream_accept_fail`：接取失败占位。
- `vera_dream_remind`：薇拉等待玩家在可以安睡的地方进入梦境，不启动小游戏。
- `vera_dream_after`：胜利后回到现实的谈话。
- `vera_dream_complete`：任务完成确认。

## 实现边界

“入梦”固定属于 `secretbase` 设施玩法。城镇 NPC、普通对话、任务按钮和地图交互点都不能直接启动入梦小游戏；它们只能改变 Character 梦境入口的解锁状态。

任务 Objective 依赖的是角色持久化数据，而不是 `PlayerCharacterDreamFinishedEvent` 一类瞬时事件。当前 `PlayerQuestSystem` 的自动步骤会周期查询 Objective，因此不需要给梦境系统反向持有任务或调用专用任务 API。

角色场景放置仍应在 `Home_01_Editor.unity` 中按正式位置完成后重新导出；本任务设计不指定坐标。
