# 前期 home_01 流程与复活点方案

## 阶段边界

这里把“第一幕”拆成前期连续流程，而不是把收复 home_01 当成完整结束。

1. 初始地牢：莉莉丝苏醒，卡莱尔昏迷/被榨干，击退两波怪，解锁离开地牢的传送点。
2. 地牢外地图：完成指定营地任务，营地旁刷新昏迷卡莱尔，推进多环任务。
3. 卡莱尔苏醒：卡莱尔醒来并短暂帮助莉莉丝理解当代人类社会；此时仍锁定真身，不开放形态切换。
4. 卡莱尔自杀式袭击：他假意合作后使用教会圣物袭击莉莉丝，失败，进入沉默不配合状态。
5. home_01 求援：卡莱尔袭击失败后，大黄狗麦穗把玩家引向逃出村庄后昏迷的学徒少年艾文；由艾文报告村子被坏逼探险队/流匪占领。
6. 收复 home_01：进入 `home_01_fight`，救人、清点、夺回关键点，达到地图控制分数；马洛克短暂逃脱。
7. 初步复兴：home_01 转为普通/修复状态。玩家通过数环轻量任务熟悉村内 NPC、设施、近郊采集和狩猎；具体材料不承担主线世界观。
8. 自我封印仪式：玩家准备找埃蒙询问周边情况时，莉莉丝因存在强度过高而魔力失稳；咕啾提出改造教会封印残骸，卡莱尔为保护村落被迫第一次合作，由此解锁 Human 状态。
9. 防务与反扑：卡莱尔在冷战和虚弱中逐步参与防务；马洛克带强盗或流匪袭击村子并死亡。
10. 前往 village_01：莉莉丝以 Human 状态接触更大的文明聚落，并为村中伤员取得治疗物资。

## 主线任务规划

### M01 初始地牢脱出

- 目标：击退第一波怪。
- 目标：击退第二波怪。
- 目标：解锁/使用离开地牢传送点。
- 对话：莉莉丝苏醒、咕啾解释、卡莱尔保持昏迷。
- 系统验证：基础战斗、波次清理、传送点解锁。

### M02 地牢外营地

- 目标：抵达地牢外地图的指定营地。
- 目标：完成营地任务，例如检查营火、简易补给、异常痕迹。
- 目标：营地旁刷新昏迷卡莱尔。
- 对话：咕啾说明当前时代断层；莉莉丝对卡莱尔的观察。
- 系统验证：任务推进刷新动态角色、营地交互、多环任务。

### M03 卡莱尔苏醒

- 目标：完成若干照看/调查环节后，卡莱尔苏醒。
- 目标：卡莱尔短暂引导莉莉丝理解当前人类社会。
- 对话：卡莱尔礼貌、疏离、隐忍；莉莉丝轻佻试探。
- 系统验证：卡莱尔从昏迷实体切换为苏醒状态；Human / 真身切换仍未解锁。

### M04 圣物袭击

- 目标：完成 1-2 个短任务，给卡莱尔制造“支开莉莉丝/准备圣物”的时间。
- 目标：播放卡莱尔自杀式袭击剧情。
- 结果：袭击对莉莉丝几乎无效；莉莉丝救下他并单方面宣布欠债；卡莱尔沉默不配合。
- 对话：卡莱尔从“邪恶的古老者”逐渐过渡到冷处理。
- 系统验证：剧情中状态切换、角色状态锁定、任务分支推进。

### M05 home_01 求援与收复

- 目标：跟随麦穗找到逃出村庄后昏迷的艾文，由艾文报告 home_01 被占领。
- 目标：进入 `home_01_fight`。
- 目标：救村民、清理敌人、夺回灰露草预处理仓/仓屋/训练场等关键点。
- 目标：达到 home_01 的控制分数。
- 结果：马洛克短暂逃脱；home_01 从战斗 variant 过渡到普通/修复状态。
- 系统验证：一个 logic map 对应多个 map variant，即 `home_01` 与 `home_01_fight` 底图一致但动态物体不同。

### M06 初步复兴与自我封印

- 目标：完成数环轻量复兴任务，引导玩家在村内移动、与不同 NPC 交互，并去近郊完成一次简单采集或狩猎。
- 目标：卡莱尔以自爆后虚弱、拒绝交流的状态常驻村中，并作为第二环任务的交付对象；该交付不代表关系缓和。
- 目标：完成复兴小段后，任务引导玩家去找埃蒙询问周边情况。
- 触发：在前往埃蒙的必经路上，莉莉丝出现魔力失稳，播放无力表现并转移到村内房间。
- 目标：听取咕啾对低魔世界、存在强度和自我封印方案的说明，再去找卡莱尔。
- 结果：卡莱尔为避免村落被破坏而被迫合作；H 段落暂留空；仪式完成后正式解锁 Human 状态。
- 系统验证：一次性区域 Trigger、角色动作、同图房间传送、常驻 NPC 任务交付、持久化形态解锁。

### M07 防务、反扑与 village_01 前置

- 目标：完成必要的防务准备，并在后续村庄防卫中解决马洛克反扑。
- 目标：从埃蒙处得知周边道路、较大聚落以及村中伤员需要的治疗物资。
- 目标：以真身穿过 `village_01_road`；道路是 `IsDangerArea = true`、`IsCivilArea = false` 的普通危险通路，卡莱尔和村民无法穿过沿途的魔物活动区，莉莉丝是唯一可靠战力。
- 目标：任务 `216 穿过南北道` 在 `205` 完成后自动接取，道路中三只任务期绿色史莱姆承担首次清路战斗。
- 结果：清路完成后，城门前的一次性 Trigger 播放入城对话；对话明确说明莉莉丝收束封印、切换 Human，并在对话结尾立即传送至 `village_01/entry_bottom`，不把控制权交还给道路地图上的玩家。
- 首次城门对话先设置 `village_01.road_gate_open`；后续道路终点显示普通进城传送器，不再重复首达剧情，也不会把玩家卡在城门外。
- 结果：入城后接续 `215 买药救急`。从草药商人处取得草药敷剂后，必须返回 `home_01` 交给埃蒙并设置 `home_01.medicine_delivered` 才算完成。自我封印使莉莉丝能进入文明区域，解除封印后的真身则服务危险区域和搜打撤玩法。

## 支线任务规划

### S01 灰露草预处理

- 类型：home_01 普通生活支线。
- 内容：采回或抢回少量灰露草，在村里完成预处理。
- 作用：解释 home_01 为什么有被抢价值，但不富裕。
- 当前任务系统落点：可用 OwnItem/SubmitItem。

### S02 临时避难帐篷

- 类型：世界观支线。
- 内容：村民每年约一个月去 `village_01` 附近搭帐篷避开魔雾潮。
- 作用：解释周期性废弃与 home_01 的贫穷。
- 当前任务系统落点：更适合用对话/交互变量推进；如要任务化，需要新增 Interact/ReachPoint 目标类型。

### S03 民兵训练

- 类型：卡莱尔关系支线。
- 内容：卡莱尔训练民兵，莉莉丝旁观、帮忙或被动捣乱。
- 作用：让莉莉丝看到卡莱尔保护弱者的一面。
- 当前任务系统落点：可先用对话和变量；若要更 gameplay 化，建议新增 MapControl/Interact 目标类型。

### S04 失踪的村民/被藏起来的物资

- 类型：收复后清理支线。
- 内容：找回躲藏村民或被藏的灰露草工具。
- 作用：拉长 home_01 从“救下”到“恢复”的过程。
- 当前任务系统落点：Find/Interact 目标类型更合适，当前系统需变量桥接。

### S05 马洛克反扑预警

- 类型：防卫前置支线。
- 内容：发现马洛克与强盗联络、脚印、烟罐、被收买的流匪。
- 作用：让反扑不是凭空发生。
- 当前任务系统落点：对话/交互变量推进即可。

## 过渡对话规划

- D01 地牢脱出后：咕啾说明莉莉丝刚醒、卡莱尔状态异常。
- D02 营地初到：莉莉丝观察时代变化，咕啾误读现代物件。
- D03 发现昏迷卡莱尔：莉莉丝不悦但保留兴趣，咕啾提示圣光残留。
- D04 卡莱尔醒来：卡莱尔假意感激并提出帮助，莉莉丝试探。
- D05 圣物袭击前：卡莱尔制造支开莉莉丝的理由。
- D06 圣物袭击后：莉莉丝救下卡莱尔，宣布欠债；卡莱尔沉默。
- D07 麦穗引路与艾文求援：替换旧“强盗往北走”过场（`transition_a_bandits_north`）。麦穗负责引路，昏迷的艾文苏醒后报告 home_01 被占领。
- D08 进入 home_01_fight：展示村民被扣押、灰露草仓/训练场被占。
- D09 收复阶段中场：卡莱尔被动指出敌人布局或救人优先级。
- D10 马洛克逃脱：莉莉丝压制自身，优先救人，没有追杀到底。
- D11 收复后：村民感谢；卡莱尔自爆后仍然虚弱，独自停留在村中某处，对莉莉丝保持冷战。
- D12 复兴任务交付：卡莱尔作为第二环交付对象，只确认物资或伤员情况，不主动延伸对话。
- D13 失稳触发：莉莉丝在去找埃蒙的必经路上出现无力和魔力外溢征兆。
- D14 房间说明：咕啾解释低魔世界、教会封印残骸和自我封印方案，引导玩家去找卡莱尔。
- D15 仪式合作：卡莱尔为了保护村落被迫同意；H 正文和演出留空；完成后解锁 Human 状态。
- D16 反扑前夜：卡莱尔防务安排，莉莉丝调侃但认可。
- D17 马洛克死亡后：敌对线闭环，卡莱尔对莉莉丝的判断再松动一层。
- D18 village_01 前置：埃蒙说明道路、治疗物资和城镇位置。
- D19 南北道清路：真身击败道路上的任务魔物，确认只有莉莉丝能够穿越这段危险地带。
- D20 晨钟镇城门：咕啾提醒文明区的人群会察觉异常；莉莉丝收束封印。对话结束动作固定为“强制 Human -> 静默传送 `village_01/entry_bottom`”，不得先结束对话再让玩家在城门前自由移动。

形态、区域访问和搜打撤结界的统一规则见 `player_form_area_and_extraction_rules.md`。

NPC 责任和各任务的真实实现状态见 `home_01_npcs_and_content_ownership.md`。其中 `207-212` 已按麦穗、艾文、杜兰、赫恩和薇拉重新分配。注意“已有任务行”不等于“剧情已经完成”：当前 `200-210` 大多只有目标和前后条件，麦穗求援、火堆盟约、收复中场、马洛克逃脱等正式对话与角色调度仍未实现。

## 当前复活点支持情况

当前代码已经有几类相关概念：

- `Config/Datas/save_point.xlsx`：存档点/传送点配置，当前字段包括 `save_point_id`、`area_var_id`、`show_unlock_conds`、`target_named_point` 等。
- `Config/Datas/born_points.xlsx`：地图初始落点配置，当前由 `MapSpawnPointUtil.ResolveMapInitialSpawnPoint(mapId)` 用于卧室出击/真身潜入落点。
- `GameLogicManager.GetCurrentReviveMap()`：当前硬编码复活地图。没有 `base_clear` 时返回 `game_init`，否则返回 `homestead_01`。
- `GameLogicManager.OnBattleEnd(false)`：遭遇战失败时调用 `GetCurrentReviveMap()`，然后 `PreparePlayerSwitchArea(reviveMapName, true)`。
- `BigMapFinishPanel`：确认后也走 `GetCurrentReviveMap()`。
- `EncounterBattleManager.FinishBattle()`：若是 `IsDefeatMode`，目前直接 `MainGameManager.Instance.QuitToSecretBase()`，绕过 `GetCurrentReviveMap()`。

因此，当前“完成一系列任务前不进 secretbase，而是在地图复活”能用硬编码做到，但不适合继续扩展。最大问题是失败返回逻辑分散：普通失败、BigMapFinishPanel、defeat encounter 不是同一个解析入口。

## 推荐数据结构

不要把复活点直接塞进 `save_point`，也不要让任务代码直接调用 secretbase。建议新增一个轻量的“失败返回/复活规则”配置表，并配一个统一解析器。

### ReviveDestination 运行时结果

```csharp
public struct ReviveDestination
{
    public string MapOverlayId;
    public string TargetPoint;
    public Vector2? TargetPos;
    public bool ResetMap;
    public bool ForceHumanMode;
    public bool EnterSecretBaseContext;
}
```

这是所有失败返回入口的共同结果。`GameLogicManager`、遭遇战、BigMapFinishPanel 只关心这个结果，不关心为什么前期不能进 secretbase。

### TbReviveRule 配置表建议

字段保持小：

- `rule_id`：规则 id。
- `priority`：优先级，数值越大越先匹配。
- `reason`：触发原因，例如 `PlayerDeath`、`EncounterDefeat`、`ManualAbandon`、`BigMapFinish`，也可先用 string。
- `source_overlay_id`：可空；限制当前地图。
- `source_logic_area_id`：可空；限制当前逻辑区域。
- `match_conds`：`CommonCheckCond` 列表。
- `target_mode`：`FixedMap`、`CurrentMap`、`LastSafeAnchor`、`SecretBase`。
- `target_overlay_id`：固定地图时填写。
- `target_named_point`：目标命名点，可空。
- `reset_map`：是否重置地图运行状态。
- `force_human_mode`：是否强制人形。

前期示例：

```text
early_before_home_defense:
  priority = 1000
  reason = PlayerDeath|EncounterDefeat|BigMapFinish
  match_conds = 未完成 home_01 防卫闭环
  target_mode = CurrentMap
  target_named_point = revive_near_player 或 BornPos
  reset_map = true
  force_human_mode = true

after_home_defense:
  priority = 100
  reason = PlayerDeath|EncounterDefeat|BigMapFinish
  match_conds = TaskFinish(home_01_defense)
  target_mode = FixedMap
  target_overlay_id = homestead_01
  target_named_point = BornPos
  reset_map = true
  force_human_mode = true

default_secretbase:
  priority = 0
  reason = ManualAbandon
  target_mode = SecretBase
  target_overlay_id = secret_base_hub
```

如果 `CommonCheckCond` 暂时不支持“未完成任务”的否定条件，可以先用正向变量桥接：例如 `revive_secretbase_unlocked` 由 home_01 防卫任务完成后设置。规则只判断这个变量是否存在；没有命中就走 early fallback。

## 推荐代码结构

新增一个独立解析器，避免通用系统耦合具体剧情：

```csharp
public static class ReviveDestinationResolver
{
    public static ReviveDestination Resolve(GameLogicManager glm, EReviveReason reason)
    {
        // 1. 遍历 TbReviveRule，按 priority 匹配 reason/source/conds
        // 2. 生成 ReviveDestination
        // 3. 没有规则时 fallback 到当前地图 BornPos 或 homestead_01
    }
}
```

然后替换入口：

- `GetCurrentReviveMap()` 逐步废弃，或改成调用 Resolver 后只返回 `MapOverlayId`。
- `OnBattleEnd(false)` 改为 `Resolve(... EncounterDefeat)`，再调用 `PreparePlayerSwitchArea(dest.MapOverlayId, dest.ResetMap, dest.TargetPoint, dest.TargetPos)`。
- `BigMapFinishPanel` 改为 `Resolve(... BigMapFinish)`。
- `QuitToSecretBase/AbandonToSecretBase` 不再作为所有失败默认，只保留为明确进入 secretbase 的能力。
- `EncounterBattleManager` 的 defeat mode 不直接 `QuitToSecretBase()`，而是调用一个统一的 `QuitEncounterByDefeat(reason)` 或让 `InnerQuitEncounterAfterAbandon` 使用 Resolver。

## 当前阶段的最小实现建议

为了前期跑通，可以分两步：

1. 先把硬编码从 `GetCurrentReviveMap()` 挪到 `ReviveDestinationResolver`，规则暂时写在代码常量里：
   - 没有 `revive_secretbase_unlocked`：回当前地图或 `game_init`/`test_link_a` 指定点。
   - 有 `revive_secretbase_unlocked`：回 `homestead_01` 或后续 secretbase。
2. 再把规则迁到 Luban 表，避免后续每改剧情阶段都要改代码。

这样功能本身简单，但结构上已经把“剧情进度”“复活目的地”“secretbase 入口”解耦了。
