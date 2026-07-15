# 城镇建筑/设施 UI 与数据结构（实现索引）

本文档对照**当前代码**与 `human_civilization_system_home01.md` §10 的界面方案。世界观与玩法见 `home_01_town.md`、`human_civilization_system_home01.md`。

---

## 1. 设计里的「建筑详情」指什么

`human_civilization_system_home01.md` §10.2：选中一栋设施后展示**名称、等级、升级、经营方案（分支）、岗位与产出**。

**当前实现**：通用详情独立面板 `TownFacilityDetailPanel`；城镇管理列表 `MapTownManagementPanel` 点击设施行会打开详情。

---

## 2. UI（代码）

| 用途 | 类名 | PanelId | Prefab |
|------|------|---------|--------|
| 城镇管理列表 | `MapTownManagementPanel` | `MapTownManagementPanel` | `Assets/Resources/UI/Prefabs/MapTownManagementPanel.prefab` |
| **设施通用详情** | `TownFacilityDetailPanel` | `TownFacilityDetailPanel` | `Assets/Resources/UI/Prefabs/TownFacilityDetailPanel.prefab` |
| 废墟修缮浮层 | `RepairDetailPanel` | `RepairDetailPanel` | `Assets/Resources/UI/Prefabs/RepairDetailRequirePanel.prefab` |

脚本路径：

- `Assets/Scripts/BigMap/UI/Home/MapTownManagementPanel.cs`
- `Assets/Scripts/BigMap/UI/Home/TownFacilityDetailPanel.cs`
- `Assets/Scripts/BigMap/UI/Home/TownFacilityDetailOpenArgs.cs`
- `Assets/Scripts/BigMap/UI/Build/RepairDetailPanel.cs`

### TownFacilityDetailPanel（通用详情）

- 显示：设施名、等级（`DevelopmentLevel`）、等级描述
- 人员：`FacilityDefinition.max_workforce > 0` 且已建造时显示 Slider，写回 `AssignedWorkforce`
- 经营方案：读 `TbFacilityOperationPlan`，按 `min_level` / `unlock_conds` 解锁，写回 `OperationPlanId`
- 升级：调用 `TownFacilityDevelopmentManager.TryUpgradeFacility`（实例级 `InstanceId`）
- 入口：`MapTownManagementPanel` 列表行点击；也可 `TownFacilityDetailPanel.Open(args)` 直接打开

### MapTownManagementPanel（城镇管理）

- 左侧设施列表 + 右侧简要详情（劳动力）
- 点击列表行 → 打开 `TownFacilityDetailPanel`

---

## 3. 等级 vs 经营方案（分支）

| 维度 | 配置表 | 存档字段 | 说明 |
|------|--------|----------|------|
| 建筑等级 | `facility_development_level` | `DevelopmentLevel` | 解锁能力、升级消耗、等级基础日产出 |
| 经营方案 | **`facility_operation_plan`（新）** | `OperationPlanId` | 同等级下的分支经营；额外 `daily_outputs`、解锁条件 |
| 人员编制 | `facility_definition` | `AssignedWorkforce` | `max_workforce` / `Workforce` 能力 |

日结算：`TownFacilityDevelopmentManager` 合并**等级产出** + **当前经营方案产出**（方案可带倍率）。

---

## 4. 数据结构

### 4.1 存档（SaveData）

| 类型 | 说明 |
|------|------|
| `TownDevelopmentPersist` | 单城镇：繁荣、人口、影响力、安定、控制度等 |
| `TownFacilityPersist` | 单设施实例：`InstanceId`、`FacilityId`、`IsConstructed`、`DevelopmentLevel`、`OperationPlanId`、`AssignedWorkforce` |
| `SaveData.TownDevelopmentById` | key = logic area id（如 `homestead_01`） |

路径：`Assets/Scripts/Saving/SaveData.cs`

### 4.2 运行时

| 类型 | 说明 |
|------|------|
| `FixedFacilityInfo` | 列表行：地图 `InstanceId` + `FacilityId` + 实体引用 |
| `HomeDataManager` | `EnsureFacilityPersistRecord`、`TrySetFacilityOperationPlan`、`TrySetFacilityWorkforce` |
| `FacilityOperationPlanCatalog` | 经营方案查询（`Assets/Scripts/BigMap/Home/FacilityOperationPlanDefinitions.cs`） |
| `TownFacilityDevelopmentManager` | 升级、日结算 |
| `GameWorldPersistStateManager` | 实例级等级读写 |

---

## 5. Luban 配置表清单（供检查）

### 5.1 `Config/Datas/facility_definition.xlsx`

| Sheet | Luban 表 | 主键 | 用途 |
|-------|----------|------|------|
| （单表） | `TbFacilityDefinition` | `facility_id` | 显示名、能力位（含 Workforce）、`max_workforce` |

JSON：`demo_tbfacilitydefinition.json`

### 5.2 `Config/Datas/facility_development.xlsx`

| Sheet | Luban 表 | 主键 | 用途 |
|-------|----------|------|------|
| `logic_area_homestead_req` | `TbLogicAreaHomesteadReq` | `logic_area_id` | 区域收复/控制度门槛 |
| `facility_development_definition` | `TbFacilityDevelopmentDefinition` | `logic_area_id+facility_id` | 某区域某设施是否可发展 |
| `facility_development_level` | `TbFacilityDevelopmentLevel` | `logic_area_id+facility_id+level` | 每级描述、升级消耗、`daily_outputs` |
| **`facility_operation_plan`** | **`TbFacilityOperationPlan`** | **`logic_area_id+facility_id+plan_id`** | **经营方案：显示名、`min_level`、`unlock_conds`、方案日产出** |

JSON：

- `demo_tbfacilitydevelopmentdefinition.json`
- `demo_tbfacilitydevelopmentlevel.json`
- **`demo_tbfacilityoperationplan.json`（新）**

### 5.3 `Config/Datas/home_facility.xlsx`

| 用途 | Luban |
|------|-------|
| 地图放置 footprint、预览 | `TbFixedFacility` / 相关 home 设施配置 |

与存档 `TownFacilityPersist` 分离：前者是场景静态/export，后者是玩家进度。

### 5.4 `Config/Datas/map_homestead.xlsx`（旧路径，并行存在）

| Sheet | 说明 |
|-------|------|
| `homestead_building` / `homestead_building_upgrade` | 旧 homestead 升级链；新功能优先 `facility_development_*` + `facility_operation_plan` |

### 5.5 `Config/Datas/__tables__.xlsx`

需注册 `demo.TbFacilityOperationPlan` → `facility_operation_plan@facility_development.xlsx`。

> 注：当前 `gen.bat` 因既有表路径问题可能失败；`FacilityOperationPlanConfig` / JSON 已手写落盘，Luban 修好后可重生成覆盖。

---

## 6. 数据流

```
HomeFacilityLogicEntity (地图 InstanceId)
    → HomeDataManager.RefreshFixedFacilities
    → TownFacilityPersist (DevelopmentLevel / OperationPlanId / AssignedWorkforce)
    → FacilityDevelopmentCatalog + FacilityOperationPlanCatalog

MapTownManagementPanel（列表）
    → TownFacilityDetailPanel（详情：等级 / 人员 / 方案 / 升级）
    → TownFacilityDevelopmentManager（日结算、升级校验）
```

---

## 7. 与 §10 四页签的差距

| 页签 | 设计 | 代码现状 |
|------|------|----------|
| 总览 | §10.1 | `MapTownManagementPanel` 顶部有繁荣/人口，未独立页签 |
| 建筑 | §10.2 | **详情面板已接等级/方案/人员/升级**；HUD 入口仍待接通 |
| 科技 | §10.3 | `HumanTechTreePanel`（独立） |
| 贸易 | §10.4 | 未并入城镇管理 |

---

## 8. 命名约定

- **通用详情 UI**：`TownFacilityDetailPanel`（非 `MapTownManagementPanel` 内嵌区）
- **单设施存档行**：`TownFacilityPersist`
- **经营方案 id**：`OperationPlanId`，配表 `plan_id`

---

## 9. 相关设计文档

- `human_civilization_system_home01.md` — §10 界面方案
- `home_01_town.md`
- `early_home01_flow_and_respawn.md`
