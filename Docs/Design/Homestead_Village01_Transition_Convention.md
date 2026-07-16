# 家园—晨钟镇过渡地图设计约定

## 1. 设计定位

这张地图是 `homestead_01` 与 `village_01` 之间的正式过渡区域，不是几屏长度的连接走廊，但第一版只做简单原型。它的首要目标是让玩家感知两地之间存在真实距离，并保证双向传送关系清晰可靠；昼夜、黑市动态和复杂道路事件暂不接入。

地图暂定中文名：**晨钟镇南北道**。

该名称是设计名，不在本阶段创建配置或场景。实现时应采用独立且稳定的程序标识，避免把中文名直接作为资源键。

## 2. 规划中的资源标识

推荐使用以下标识，最终以配置表实际约定为准：

| 资源 | 推荐标识 | 说明 |
|---|---|---|
| 逻辑区域 | `village_01_road` | 过渡地图自己的逻辑区域，不复用 `village_01` |
| 地图变体 | `village_01_road` | 与逻辑区域保持一对一，便于地图和存档定位 |
| 运行时场景 | `Village01_Road` | 对应 `Assets/Scenes/Main/Village01_Road.unity` |
| 编辑器场景 | `Village01_Road_Editor` | 对应 `Assets/Scenes/Main/Village01_Road_Editor.unity` |
| Overlay | `village_01_road` | 原型阶段只使用单一 Overlay |
| MapChunk | `Village01_Road` | 与 `scene_name` 对齐的运行时地图块资源键 |
| MapExport | `village_01_road` | 单一 Overlay 的导出资源 |

过渡地图原型不参与昼夜切换，不创建 `village_01_road_d` 或 `village_01_road_n`。未来若需要昼夜表现，再按独立需求扩展 Overlay，不在原型阶段预留复杂动态实体。

## 3. 地图规模与方向

### 3.1 硬性尺度

- 主轴为南北方向。
- 南北有效长度至少 **200 格世界单位**，推荐规划在 `220—240` 格之间，为入口缓冲、道路节点和北端镇门留出空间。
- 横向不追求同等规模，但应有足够宽度形成道路分叉、排水渠、货运场和局部绕行路线；建议有效宽度约 `48—72` 格。
- 不把长度理解成一条笔直道路。200 格应由多个有明确用途的路段组成，并用视线、地形和设施切分节奏。
- 地图不使用运行时随机拼接；地面、房屋、道路、静态物、传送点和 NamedPoint 在编辑器中固定铺设。

### 3.2 五段式南北结构

| 段落 | 建议纵向范围 | 主要内容 | 作用 |
|---|---:|---|---|
| 南端家园出口缓冲 | 0—30 | 家园出口、旧路、低矮林带、简单安全区 | 让玩家确认已经离开家园 |
| 南部荒路与货运坡道 | 30—80 | 车辙、仓棚、排水渠起点、零散采集物 | 建立道路长度与资源动线 |
| 中段桥梁与分流口 | 80—130 | 桥、渠闸、废弃检查台、东西向支路 | 第一个空间记忆点和轻量事件点 |
| 北部排水渠段 | 130—180 | 简单涵洞、排水渠、废弃货场 | 保留环境方向，不接入黑市动态 |
| 北端晨钟镇镇门 | 180—240 | 规整石路、镇门、钟楼远景、守卫与传送点 | 进入 `village_01` 的明确门槛 |

## 4. 玩家传送与命名点

### 4.1 必须存在的连接

过渡地图至少需要三个稳定的 NamedPoint：

- `village_01_road_from_home`：从 `homestead_01` 抵达过渡地图的南端落点。
- `village_01_road_to_town`：过渡地图北端前往 `village_01` 的传送点或传送落点。
- `village_01_road_to_home`：返回家园的南端落点。

晨钟镇核心保留：

- `entry_bottom`：接收来自过渡地图的北侧入口。

建议连接关系：

```text
homestead_01
  -> village_01_road_from_home
village_01_road
  -> village_01_road_to_home
village_01_road
  -> village_01/entry_bottom
village_01/entry_bottom
  -> village_01_road_to_town
```

返回传送不应把玩家送到入口传送器本体上，也不应落入道路阻挡、排水渠、守卫碰撞或装饰碰撞中。入口、出口和传送器之间至少保留一个可行走缓冲区。

### 4.2 过渡地图不是独立小房间

这张地图应使用大地图标准的逻辑/表现分离和地图导出机制：

- `AreaRoot/MapVariantRoot/GridRoot`：南北主道路、桥面、渠岸和可行走采样层。
- `AreaRoot/MapVariantRoot/Decorate`：道路静态物、路牌、仓棚、桥梁、钟楼远景等可烘焙内容。
- `AreaRoot/MapVariantRoot/Trigger`：需要通过 `MapScenePrefabProvider` 导出的静态触发或表现对象。
- `AreaRoot/DynamicRoot/Common`：只放原型所需的传送器和必要交互点；暂不放商队、守卫和黑市事件实体。
- `AreaRoot/NamedPoint`：家园入口、晨钟镇入口、返回点和事件落点。
- `AreaRoot/NamedPath`：原型阶段可暂不创建；未来加入守卫或商队后再补充道路路径。

禁止只创建一张看起来很长的 Tilemap，再把传送器放在两端；必须同时补齐地图变体、Overlay、静态导出、动态实体导出、NamedPoint 和传送网络。

## 5. 道路与环境设计

### 5.1 主道路

- 主道路不是完全笔直，建议有两次轻微偏转和一次桥梁/坡道变化。
- 南段较自然、泥土和木制结构较多；北段逐步出现石板、路缘和规整排水口。
- 越靠近晨钟镇，视觉秩序越强，但不要提前出现完整城镇建筑。
- 玩家始终能通过路牌、钟声方向、远景和地形判断北方位置。

### 5.2 排水渠

- 排水渠沿地图纵向的一侧延伸，在中段通过桥梁或渠闸与主路发生关系。
- 排水渠只承担基础环境表现和地形分隔，不默认扩展为地面液体战斗系统。
- 原型阶段只放固定静态渠岸、桥或涵洞，不接入夜间 Overlay、动态搬运和潜行事件。
- 正式黑市交易点仍放在晨钟镇低渠棚户区；过渡地图暂不放置黑市入口任务。

### 5.3 黑市伏笔（后续预留）

原型阶段不实现黑市伏笔，只在地形上预留后续扩展空间：

1. 排水渠和涵洞保留可扩展的边界。
2. 静态货场预留少量空地，不摆放正式黑市 NPC。
3. 后续再由菲剧情决定是否把涵洞连接到晨钟镇黑市。

## 6. 原型阶段的事件与战斗边界

原型阶段按“可安全通过的长距离连接地图”处理：

- 南端、中段、北端均以静态道路和简单阻挡为主，不安排连续战斗。
- 可保留一处简单地标，例如桥梁或路牌，用于确认玩家行进方向。
- 原型阶段不安排商队、巡逻、黑市或夜间事件。
- 关键任务不应要求玩家在 200 格道路上反复来回；传送关系确认后，再决定是否增加快捷路。
- 若未来加入危险区，使用独立 `AreaEffect` 或明确的战斗实体，不把排水渠语义扩展成通用液体类型。

## 7. 晨钟镇大钟的远景关系

- 大钟位于 `village_01` 镇中心，不放在过渡地图。
- 过渡地图北段提供一次远景或声音提示：能看到钟楼顶部、旗帜或听到低沉钟声。
- 进入晨钟镇后，大钟应成为第一视觉焦点，周围地面、建筑轴线和人流都比贫民区规整。
- 大钟是秩序与时间的象征，也可以成为日夜、巡防、公告和剧情转折的公共计时器。

## 8. 设计阶段的 Excel 与导出接入清单

本节只规定未来施工时必须补齐的内容，不在当前阶段直接修改表格。

### 8.1 逻辑地图与地图变体

- 在 `Config/Datas/map.xlsx` 的 `logic_area_info` sheet 中增加 `village_01_road`。
- 在地图变体数据中增加同名变体，指向 `Village01_Road`。
- 在 `Config/Datas/map.xlsx` 的 `area_variant_info` sheet 中增加 `village_01_road`，`logic_area_id=village_01_road`，`scene_name=Village01_Road`。
- 在 `Config/Datas/map.xlsx` 的 `area_overlay_state_info` sheet 中只增加一个 `village_01_road` Overlay，`map_data_name=village_01_road`；原型阶段不配置昼夜切换。
- 如需世界地图可见，在 `world_map_big_map` sheet 增加与过渡地图变体关联的边界和底图记录；过渡地图不应默认覆盖晨钟镇核心的显示边界。
- 原型阶段设置为可传送、可进入但非家园；建议 `is_civil_area=true`、`is_danger_area=false`、`always_alert=false`，不引入夜间风险判定。
- 地图显示名使用“晨钟镇南北道”，晨钟镇核心显示名使用“晨钟镇”。

### 8.2 场景与编辑器场景

未来创建：

- `Assets/Scenes/Main/Village01_Road.unity`
- `Assets/Scenes/Main/Village01_Road_Editor.unity`

编辑器场景必须使用 `AreaRoot` 根节点，并补齐 `MapChunkEditorRoot.MapVariantSceneName`、`MapVariantRoot`、`GridRoot`、`Decorate`、`Trigger`、`DynamicRoot`、`NamedPoint` 和需要的 `NamedPath`。

### 8.3 导出 JSON 与资源

通过 Unity 地图导出器生成，不手写生成资产：

- `Assets/Resources/MapChunk/Village01_Road.asset`
- `Assets/Resources/MapExport/village_01_road.asset`
- 如存在 Portal Network：`Assets/Resources/MapExport/village_01_road_portal_networks.json`
- 运行时配置 JSON 由 Luban/项目配置生成流程产出，不把导出 JSON 当作配置源。

### 8.4 传送器接入

- 家园出口传送器目标改为 `village_01_road_from_home`。
- 过渡地图南端返回传送器目标为家园的既有入口点。
- 过渡地图北端传送器目标为 `village_01/entry_bottom`。
- 晨钟镇核心返回传送器目标为 `village_01_road_to_town`。
- 传送点必须在编辑器场景中通过 `EntityInitInfo4Teleporter` 配置，并随单一 Overlay 正确导出。

## 9. 与菲剧情的衔接调整

菲的主要剧情仍发生在晨钟镇核心的低渠棚户区，过渡地图原型不依赖菲的动态内容：

- 菲在玩家首次抵达晨钟镇后，通过贫民区外围的线索出现。
- 《借来的面包》暂不依赖过渡地图事件；后续可以再把货运物资或涵洞作为任务线索。
- 过渡地图不放置完整黑市 NPC、正式市场设施或菲的固定住所。

## 10. 设计验收标准

- 南北有效长度不少于 200 格世界单位，且由至少五段具有不同功能的道路空间组成。
- 家园、过渡地图、晨钟镇核心三者各有独立逻辑地图/地图变体身份，不能通过复用同一逻辑区域掩盖连接关系。
- 玩家可以双向传送，并且两侧落点不重叠、不落在阻挡或危险实体上。
- `village_01` 的大钟只在晨钟镇核心成为主地标；过渡地图只提供远景或声音预告。
- 排水渠只作为静态环境预留，正式黑市仍归属低渠棚户区。
- 原型导出只需验证 MapChunk、单一 MapExport、NamedPoint 和动态传送器；不验证昼夜 Overlay 切换。
- 本设计确认前不创建场景、不改 Excel、不运行地图导出器。
