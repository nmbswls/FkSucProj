# 秘闻与潜入情报系统

## 术语对应

| 策划用语 | 当前代码/配置名 | 含义 |
| --- | --- | --- |
| 秘闻点数 | `rumor_points` | 用于购买秘闻的物品型货币。 |
| 秘闻定义 | `RumorIntel` / `TbRumorIntel` | 一条可购买情报的文本、价格、期限和探索效果。 |
| 固定秘闻 | `ERumorIntelKind.Fixed` | 满足出现条件时持续可购买，不参与每日随机候选。 |
| 随机秘闻 | `ERumorIntelKind.RandomPoolEntry` | 从 `TbRumorRandomPool` 按权重生成的当日候选；每张目标地图同时只能激活一条。 |
| 秘闻候选 | `RandomOfferRumorIds` | 针对某张目标地图、某个结算日生成但尚未购买的随机秘闻。 |
| 已购秘闻/待生效秘闻 | `ActiveIntel` / `RumorActiveEntry` | 已付款，绑定目标地图，等待下一次进入该地图时应用。 |
| 秘闻有效期 | `ExpireSettlementDay` | 在进入目标地图前允许保留的期限，不表示效果实体在地图中的存活时间。 |
| 秘闻落点 | `ENamedPointType.RumorCandidate` | 地图导出的候选点；没有候选点时当前实现退化为玩家出生位置。 |
| 秘闻效果 | `ERumorEffectType` | 当前支持额外宝箱 `Chest` 和特殊 NPC `Npc`。 |
| 出发潜入界面 | `UIBedroomDeployPanel` | 选择 `IsMagicSensitiveArea` 地图、打开秘闻购买面板并开始潜入。 |
| 秘闻购买面板 | `RumorIntelShopPanel` | 显示固定候选、随机候选和该目标地图的待生效秘闻。 |

## 当前数据流

1. `UIBedroomDeployPanel` 选择一个 `AreaOverlayStateInfo.Id`。
2. 秘闻按钮以该 Id 打开 `RumorIntelShopPanel`。
3. `RumorIntelSystem.TryPurchase(mapId, rumorId)` 扣除 `rumor_points`，并把记录写入该地图的 `MapRumorPersist.ActiveIntel`。
4. 玩家开始潜入并加载目标 Overlay。
5. `GameLogicManager.PostNewAreaLoaded()` 调用 `RumorIntelMapSpawn.ApplyPurchasedRumorsOnMapLoaded()`。
6. 效果在 `RumorCandidate` 中随机选点并创建实体 Record；只有成功排队创建的秘闻才从 `ActiveIntel` 消费。

## 配置与职责边界

- `rumor_tables.xlsx` 负责秘闻定义、随机池和全局候选数量。
- `AreaOverlayStateInfo.Id` 是购买状态和实际生效地图的共同键。
- 地图编辑场景负责布置 `RumorCandidate`，MapExport 负责导出。
- `RumorIntelSystem` 负责候选、购买、期限和持久化，不直接创建地图实体。
- `RumorIntelMapSpawn` 负责把待生效秘闻转换成地图实体，不负责扣费。

## 当前限制

- `RumorIntel` 尚无 `target_map_ids` 或区域标签字段。虽然购买状态按所选地图隔离，但固定秘闻和默认随机池本身仍对所有目标地图共用，无法配置“只在森林出现”的秘闻。
- 当前只有导出了 `RumorCandidate` 的地图能获得合理落点；缺失时会刷在玩家附近并输出警告。
- 当前效果只支持宝箱和 NPC。路线提示、警戒变化、敌人标记等后续效果应扩展 `ERumorEffectType`，不要在购买 UI 中写死。
