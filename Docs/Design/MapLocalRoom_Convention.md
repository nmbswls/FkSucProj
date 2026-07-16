# 同图室内房间布局与本地传送约定

## 适用范围

本约定适用于在同一大地图内，通过传送点进入独立坐标块室内房间的场景，例如 `Home_01` 的 `Bedroom`。

这类房间不是独立 Unity Scene，也不是任务专属地图；玩家仍然处在当前 Area/Overlay 中，只是被传送到远离主区域的室内坐标块。

## 运行机制

- 传送实体使用 `EntityInitInfo4Teleporter`。
- `TargetMapName` 为空或等于当前 Overlay 时，走同图本地传送。
- `TargetNamedPoint` 指向室内入口 `NamedPoint`。
- 表现层只负责黑幕、过渡和调用 `RequestLocalRoomTeleport`；地图与任务逻辑不直接操作场景对象。
- 返回传送点使用同一机制，目标指向主区域的命名点。

## Home_01 当前结构

`Home_01_Editor` 中，室内房间的地面与静态内容是拆分组织的：

- `AreaRoot/MapVariantRoot/GridRoot/Bedroom`
  - 放室内地面 Tilemap、行走碰撞和逻辑采样来源。
  - `Bedroom_Ground` 是地面 Tilemap/碰撞层的子节点。
- `AreaRoot/MapVariantRoot/Room`
  - 放房间语义/房间组织节点及其房间实例。
- 房间静态可视物体放在对应静态内容节点，不与动态实体混在一起。
- `AreaRoot/NamedPoint`
  - 放 `bedroom_in_entry`、`bedroom_out_entry` 等传送落点。
- `AreaRoot/DynamicRoot/Common`
  - 放传送器、房间内动态交互物和其他需要参与导出的动态实体。

当前 `Home_01` 的室内房间位于主村远处的独立坐标块，现有房间实例使用约 `(-100,-100)` 一带的坐标；这不是随机运行时生成，而是编辑器场景中的固定空间布局。

## 坐标规则

- 房间采用固定世界坐标平铺，不在任务配置中记录房间坐标。
- 每个房间有自己的坐标原点；地面 Tilemap、静态物体、碰撞、命名点和动态实体都必须以同一房间原点布置。
- 房间坐标块应远离主区域，并与其他房间保留至少 `4` 格安全间隔；间隔按实际房间包围盒计算，不按单个入口点计算。
- 入口和返回点必须位于可行走格，不能落在墙体、Hole、装饰碰撞或传送器本体重叠位置。
- 新房间不得只添加一个“看起来在远处”的静态节点；必须同时补齐地面 Tilemap、静态内容、房间节点、入口/返回 NamedPoint 和需要的动态实体。

## 导出规则

- 地面与 Tilemap 从 `GridRoot` 导出。
- 静态内容从 `Decorate`/`Trigger` 等静态导出层扫描。
- 动态实体从 `DynamicRoot/Common` 或对应 Overlay 目录导出。
- 命名点从 `NamedPoint` 导出。
- 通过 Unity 地图导出器生成 MapChunk/MapExport，禁止手改生成资产。
