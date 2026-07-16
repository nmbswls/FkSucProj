# Home 01 厨房废墟与房间布局约定

## 角色

- `home_merchant` 的正式名为 **萨赫尔**。
- 萨赫尔来自南方的赤沙沿岸，肤色为深棕色；职业是流浪厨师兼野外炊事人。
- 服装使用轻薄旅行衣、头巾、皮革工具腰包、腕带和凉鞋；禁止厨师帽、白色厨师服和专业白围裙。
- 料理能力来自长期远行、就地取材和火堆经验，不来自正式厨房编制。

## 房间引用

- 厨房废墟使用 `Home_01` 现有的同图室内房间机制，不在本任务文档中重新定义房间坐标。
- 通用的 Tilemap、静态物体、Room 节点、NamedPoint 和本地传送规范见 `Docs/Design/MapLocalRoom_Convention.md`。

## 一次性可破坏物

- 前门堵塞使用 `DynamicEntityExportGenerator + EntityInitInfo4DestroyObj`。
- `WillRespawn=false`；需要多次攻击的残骸优先使用独立 `DestroyObj` 配置的 `HitCount`，不要伪装成可复活 NPC。
- 破坏后的剧情开关由 DestroyObj 配置中的 `BreakOutputs` 写入玩家变量；变量名使用 `home_01.{feature}_cleared`。
- 门、传送点和远房间内的交互点只通过 `CommonCheckCond(CheckVariable)`读取该变量。
- 破坏物的 `DisappearCond` 也读取同一变量，保证读档和重新进入地图时保持一次性状态。
- 破坏物必须放在可攻击、可到达的位置；不得压住传送落点、关键 NPC 或可行走主通道。

## 厨具任务约定

- 任务使用 `home_kitchen_front_rubble` 作为前门残骸，`HitCount=3`，破坏后写入 `home_01.kitchen_front_cleared`。
- 远房间交互实体使用唯一名 `home_kitchen_utensils`，配置名同名；仅在前门标记存在时出现。
- 任务目标顺序：与萨赫尔接任务 → 破坏前门残骸 → 通过已存在的远房间传送 → 交互取回厨具 → 回村交给萨赫尔。
- 新增实体必须从 editor scene 导出生成 `MapExport`，禁止手改生成的 `homestead_01.asset`。
