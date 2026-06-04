# Dual Grid Tilemap

逻辑格（Data）存笔刷与地形 ID，视图格（View）相对 Data 偏移半格，按四角邻接算 16 态自动拼贴。

## 文件

| 脚本 | 作用 |
|------|------|
| `DualTileMap` | 场景根：Data/View、刷新、`ViewLayer` 配置 |
| `DualGridCore` | 半格偏移、四角 mask、StableHash |
| `DualGridTile` | View 层 `TileBase`，运行时按 mask 选图 |
| `DualGridTilePalette` | 16 槽位 + 每槽多 Sprite 变体 |
| `DualGridBrushRegistry` | 笔刷 Tile → `TerrainId` |

编辑器：`Assets/Scripts/Editor/DualGrid/`（创建菜单、Palette 4×4、Scene 悬停探针）。

## 快速使用

1. 在带 **Grid** 的父物体下（或先建 `MapGrid` 挂 Grid），再 **GameObject → 2D Object → Dual Tile Map** 建 Data/View；创建时**不会**自动加 Grid，优先用父级。
2. 在 Project 窗口 **Create → Map → Dual Grid** 下创建 **Brush Registry**、**Palette**、**Display Tile**（挂 Palette）。
3. Registry：把 Data 笔刷 Tile 映射到 `TerrainId`（同一地形共用 ID）。
4. Palette：Inspector 竖排 mask 0–15 填转角图；`TerrainId` 与 Registry 一致。
5. `DualTileMap`：绑 `Brush Registry`、Data Tilemap；`View Layers` 里填 View Tilemap、`DisplayTile`、`Palette`（或只填 `TerrainId`）。
6. **只在 Data 上画**；View 由 `tileChanged` / 编辑器 `AutoRefreshInEditor` 刷新。

## 概念

```
Data 格 (x,y)     View 角点格相对 Data 偏移 (+0.5, +0.5) 格
笔刷 Tile          DualGridTile + Palette 按 mask 显示
```

- **Mask**（相对 View 角点）：bit0 左下 · bit1 右下 · bit2 左上 · bit3 右上；四角在 Data 上是否为同一 `TerrainId`。
- **多地形**：多个 `TerrainId`、多个 `ViewLayer`，各层独立 mask 与 Palette。
- **变体**：同 mask 多 Sprite 时用 `StableHash(cell)` 确定性随机。

## 层级建议

```
MapGrid (Grid)              ← 同区域共用一个
  DualTileMap               ← 仅 DualTileMap，多 View 用 ViewLayers
    Data / View / ...
```

## 注意

- Data 用普通 `Tile`，不靠自定义 Data Tile；地形语义只在 Registry。
- 无父级 Grid 时 Inspector 会提示；`DualTileMap.Grid` 可手动指定以覆盖自动解析。
- 改层级或嵌套类型后，场景中 `View Layers` 引用可能需在 Inspector 重绑。
- 与大地图 `WalkGrid` / `WorldAreaRoot` 无关，需自行对接逻辑层若要用。
