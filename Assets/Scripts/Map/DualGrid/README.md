# Dual Grid Tilemap

## 数据流

```
Data 普通 Tile
  → Brush Registry / Brushes（Tile → TerrainId）
  → Brush Registry / Terrains（TerrainId → Palette）
  → Palette（16 态 Sprite）
  → View Tilemap 自动刷新显示
```

## DualTileMap（场景）

- `DataTilemap`
- `BrushRegistry`
- `ViewTilemap`
- 可选 `Grid`、`AutoRefreshInEditor`

无需 Display Tile、无需 DualGridTile 资产；View 由组件内部运行时瓦片按格取 Palette 贴图。

## 资源

| 资产 | 作用 |
|------|------|
| Brush Registry | 笔刷映射 + Terrain→Palette |
| Palette | 16 态显示 |

## 菜单

- **Create → Map → Dual Grid**：Brush Registry、Palette
- **GameObject → 2D Object → Dual Tile Map**
