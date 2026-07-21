# 种植系统（home_01）

轻量生产：手作复耕 → 农业小站自动化。仅 home_01，5 作物，无季节天气，首版不接烹饪/酒馆。

## 流程

1. 任务 200 完成 → `home_01.reclaimed`；对话 `base_fight_end` Teleport 回 `homestead_01`。
2. 农田 / 种子篮出现；种子由任务 `finish_reward` 发到玩家背包（整理进种子篮）。
3. 种子篮：整理种子 / 开始播种；播种模式用 `FarmSeedBarPanel`，面向格左键播，滚轮切种，X 退出。
4. 格交互：浇水、施肥、成熟收获（无小站进背包；有小站进小站仓）。
5. `home_01.hearth_stable` 后可建 `farm_station`；日结算：镇民假养护、自动补空、派工数字收割。

## 架构

| 模块 | 说明 |
|------|------|
| 状态 | `FarmSystem` + `SaveData.TownFarmByLogicAreaId` |
| 农田 | 静态分块 `Prefab/Map/FarmPlot/...` + `MapScenePrefabProvider`，挂 `FarmPlotAreaProvider` |
| 种子篮 | `EEntityType.SeedBasket` → Presentations；库存在 FarmSystem |
| UI | `FarmSeedBarPanel` / `FarmStationPanel` 均走 prefab + UIManager |
| 日结算 | `HandleOneDayBalance` → `farmSystem.ApplyDailySettlement` |
| 配表 | `farming.xlsx`：`TbCropDef` / `TbFarmPlot` / `TbFarmStationPlanDefault` |

开关与发奖走任务/对话，不在战斗结算或农场逻辑里硬写。
