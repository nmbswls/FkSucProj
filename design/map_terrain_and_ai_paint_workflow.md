# 地图地形与 AI Paint 工作流

## 目标与分层

地图主体必须保持逻辑层与表现层分离：

- `GridRoot` 下的逻辑 Tilemap 决定可走区域与逻辑高度。
- DualTile 或普通 Tilemap 负责可重复、自适应的地表表现。
- Paint 背景负责主体区域之外的不规则视觉延伸，不参与可走性判断。
- 静态装饰、动态实体、NamedPoint 和 NamedPath 继续遵循地图导出规范。

AI 补图不能改变逻辑地形。可走边界、坡面、出生点、传送器和碰撞必须先在编辑器场景中确定。

## 编辑器场景结构

目标场景为 `Assets/Scenes/Main/{SceneName}_Editor.unity`，根节点为 `AreaRoot`。

```text
AreaRoot
  MapVariantRoot
    GridRoot
      Ground
      Hole
      <DualTileMap>
        Data
        View
      <普通视觉 Tilemap>
    Decorate
    Trigger
  DynamicRoot
  NamedPoint
  NamedPath
```

`MapChunkEditorRoot.MapVariantSceneName` 必须与运行时场景名、Luban `AreaVariantInfo.scene_name` 和 MapChunk key 一致。

## 逻辑地形

所有可移动格必须在 `GroundLayerNames` 指定的不可见逻辑 Tilemap 上有 Tile。默认层名为 `Ground`。

- 平地使用 `Assets/Arts/Tile/ground/asset/g*.asset`。
- 斜坡使用 `Assets/Arts/Tile/ground/asset/s*.asset`。
- 平地和坡面语义由 `Assets/Resources/MapLogicHeightConfig.asset` 定义。
- 当前斜坡统一为南低北高，在单格内从 `SouthLogicY` 连续插值到 `NorthLogicY`。
- `Hole` 上存在 Tile 时，该位置不可走，并覆盖 Ground 判断。

不要用视觉 Tile、AI 背景或装饰碰撞代替 Ground 标记。

## 地表表现

- 需要根据邻格自动拼接的地表使用 `DualTileMap`：在 `Data` 层铺笔刷，由 `View` 层自动生成 16 状态表现。
- 纯点缀且不需要邻格适配的内容使用普通 Tilemap。
- 导出时 `MapChunkVisualBaker` 会把 DualGrid View 和规则 Tile 烘焙为静态 Tile。
- DualTile 的 `Data`/`View` 是内部层，不应被当作逻辑 Ground。

## Paint 切图

打开 `Window/Map Paint Background`：

1. 选择 `AreaRoot` 并同步 Scene Name。
2. 设置 `PaintWorldRect`，随后 Snap 到 chunk 网格。
3. 选择 chunk，执行 `Export For AI`。
4. 输出位于 `Assets/Resources/MapChunk/{SceneName}/PaintExport/export_ai/`。

默认配置为：

- chunk 世界尺寸：32。
-运行时 PPU：16。
- Paint Export PPU：16。
- 中心块：512x512。
- 邻域扩展：每边 25%，因此 AI 输入通常为 768x768。
- Mask 颜色：洋红色。

AI 输入会优先拼入相邻的 painted 结果，其次使用捕获模板。这样连续处理相邻 chunk 时可以维持边界一致。

## 亿创补图约束

使用亿创新版 `yichuang_generate_model` 和模型 `gpt-image-2`。不要使用旧版 `text2image`，也不要把模型名写成 `gpt_image_2`。

补图必须：

- 保持输入图片尺寸不变。
- 保留已有 Tilemap、道路、悬崖、物体及其像素位置。
- 只把洋红区域扩展成与地图风格一致的自然背景。
- 不新增可被误认为道路、入口、桥梁或可交互点的结构。
- 不在 chunk 边缘制造明显接缝。
- 不添加文字、水印、UI 或角色。

如果 MCP 无法直接读取本地文件，必须使用可上传本地图片的亿创网页流程；不得退化为不带输入图的纯文字生成。

## 导入与运行时打包

AI 返回图必须为方形且不小于中心块。中心块尺寸直接使用；完整上下文尺寸直接裁中心；亿创常见的 1024x1024 等更高分辨率输出会先归一到 manifest 上下文尺寸，再裁取中心区域。

交互方式：

1. `Import Painted PNG...` 写入 `PaintExport/chunks/painted_x_y.png`。
2. `Sync Selected` 或 `Sync All` 裁取中心区域。
3. 生成 `Sprites/bg_x_y.png` 和 `Prefabs/bg_x_y.prefab`。
4. 将 `MapChunk/{SceneName}/Prefabs/bg_x_y` 写入 `MapChunkDatabase.BackgroundKey`。

自动化方式使用 `MapPaintBackgroundAutomation`：

```powershell
Unity.exe -batchmode -quit -projectPath <project> `
  -executeMethod MapPaintBackgroundAutomation.ImportAndSyncFromCommandLine `
  -mapPaintMap <SceneName> -mapPaintX <x> -mapPaintY <y> `
  -mapPaintInput <painted.png> -logFile <log>
```

修改逻辑 Tilemap 后，重新执行完整 MapChunk 导出；只更新 Paint PNG 时，单 chunk Sync 足以刷新背景 prefab 和数据库键。

## 运行时读取

- `MapVariantMapResources` 从 `Resources/MapChunk/{SceneName}` 加载 `MapChunkDatabase`。
- `MapChunkManager` 根据 chunk 坐标异步实例化 `BackgroundKey` 和 `TilemapKey`。
- `WorldAreaRoot` 从 `WalkGridKey` 和 `GroundLayerNames` 绑定逻辑地面。
- AI 背景挂到 `BackgroundChunkRoot`，不参与 `IsWorldPosWalkable` 或逻辑高度采样。

## 验收

- `GroundLayerNames` 能找到至少一个逻辑 Tilemap。
- 所有预期可走格都有有效平地或坡面 Tile，Hole 没有误覆盖。
- AI 返回图尺寸符合 manifest，已有地图内容没有明显漂移。
- 相邻 painted chunk 在边缘连续。
- `MapChunkDatabase` 对应 chunk 同时保留需要的 `TilemapKey`，并写入正确 `BackgroundKey`。
- `bg_x_y.prefab`、`bg_x_y.png` 和 `GridRoot.prefab` 存在。
- 运行时进入目标区域后背景、地形、可走性和坡面高度均正确。
