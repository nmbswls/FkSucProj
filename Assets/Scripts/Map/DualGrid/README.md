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

View 使用工程内 `DualGridViewTile` 资产铺格，`GetTileData` 按 mask 从 Palette 取 Sprite（编辑器可正常显示）。

## 资源

| 资产 | 作用 |
|------|------|
| Brush Registry | 笔刷映射 + Terrain→Palette |
| Palette | 16 态显示 |

## 菜单

- **Create → Map → Dual Grid**：Brush Registry、Palette
- **GameObject → 2D Object → Dual Tile Map**
