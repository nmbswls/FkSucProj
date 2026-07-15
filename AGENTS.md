# Codex Project Instructions

## Role And Project Context

You are working as a senior Unity developer with strong product, design, and art judgment.

This project is a top-down 2D action game. It includes town stealth, dungeon combat and monster farming, simulation/management systems, and daily-life Japanese RPG style gameplay. In most gameplay states, the player controls the protagonist moving through maps.

## Gameplay Architecture

For large-map gameplay, preserve the existing logic/presentation separation.

- The logic layer is controlled by `GameLogicManager`.
- Map objects in the logic layer usually implement or inherit from `ILogicEntity`.
- Presentation objects usually inherit from `ScenePresentationBase`.
- Avoid coupling generic systems to feature-specific static methods or semantic assumptions.

## Map Editor Scenes And Export

Large maps use paired runtime and editor scenes.

- Runtime scene: `Assets/Scenes/Main/{SceneName}.unity`.
- Editor scene: `Assets/Scenes/Main/{SceneName}_Editor.unity`.
- `MapChunkEditorRoot.MapVariantSceneName` should match the runtime scene name, the Luban `AreaVariantInfo.scene_name`, and the MapChunk key.
- Prefer changing `Config/Datas/map.xlsx` and the editor scene. Regenerate/export MapChunk and MapExport assets through the Unity map exporter instead of hand-editing generated assets.

Editor scene hierarchy uses `AreaRoot` as the scene root. Preserve these names because exporter code resolves them by name:

- `AreaRoot/MapVariantRoot`: static map variant content. Legacy compatible names are `StaticRoot` and `StaticPrefabRoot`.
- `AreaRoot/MapVariantRoot/GridRoot`: grid and tilemap source. `GroundLayerNames` defines walkable sampling tilemap names; `Hole` is treated as the hole/non-walkable layer when present.
- `AreaRoot/MapVariantRoot/Decorate`: static decoration. Prefab instances and bakeable scene leaf objects are exported into chunk static data.
- `AreaRoot/MapVariantRoot/Trigger`: static trigger/prefab-provider layer. Non-decorate static layers export only objects with `MapScenePrefabProvider`.
- `AreaRoot/DynamicRoot`: dynamic entity generator roots.
- `AreaRoot/NamedPoint`: named point root.
- `AreaRoot/NamedPath`: named path root.

Additional map hierarchy rules:

- `GridRoot`, `Room`, `StaticOverlay`, `Roads`, and `Edge` are infrastructure folders and are skipped by static prefab scanning.
- FOV blockers are scanned under `MapVariantRoot` by `Layer = MapViewObc` plus `Collider2D`; do not rely on a folder named `FovObstacleRoot`.
- Portal networks are discovered by recursively scanning `PortalNetworkProvider` under `AreaRoot`; a `PortalNetworks` folder is only an organization convention, not a required name.

Dynamic entity export rules:

- Dynamic entities are scanned from `DynamicRoot`.
- `DynamicRoot/Common` is shared by all overlays.
- `DynamicRoot/{overlay_id}` is specific to the Luban `AreaOverlayStateInfo.id`.
- If neither a matching overlay folder nor `Common` exists, export may fall back to scanning the full `DynamicRoot`.
- Place dynamic logic entities with `DynamicEntityExportGenerator`.
- During export, `DynamicEntityRefreshInfo.InitInfo.Position` is overwritten with the generator transform world position.
- `FishingSpot` dynamic generators must have `UniqName`; the overlay export fails without it.

Named point and path rules:

- `NamedPoint` export only collects active leaf nodes under `AreaRoot/NamedPoint`.
- A named point's exported name is the leaf GameObject name.
- `NamePointGenerator` can override the point type, but the point still needs to be a leaf to export.
- `NamedPath` export recursively scans under `AreaRoot/NamedPath` for `NamePathProvider`.
- A named path name uses `NamePathProvider.Name` when present; otherwise it falls back to the provider GameObject name.
- Named paths store referenced `NamePointGenerator` GameObject names.

Map export resources:

- Variant/MapChunk export writes `Assets/Resources/MapChunk/{SceneName}.asset`.
- Chunk support assets live under `Assets/Resources/MapChunk/{SceneName}/`, including `Prefabs`, `Sprites`, `BakedTiles`, and `SceneBake/{map_data_name}`.
- Overlay/MapExport export writes `Assets/Resources/MapExport/{map_data_name}.asset`.
- Portal network JSON is written to `Assets/Resources/MapExport/{map_data_name}_portal_networks.json` when portal networks exist.
- Runtime loading uses `Resources/MapChunk/{scene_name}` and `Resources/MapExport/{map_data_name}`.

Exporter validation details:

- Missing `AreaRoot`, missing `MapChunkEditorRoot`, or empty `MapVariantSceneName` are export-blocking errors.
- Missing usable `GridRoot` or tilemaps is a validator warning, but actual MapChunk export fails if no tilemap source can be resolved.
- Missing `Decorate` and `Trigger` warns that static export may be empty.
- Missing `DynamicRoot` or matching overlay folders warns that dynamic export may be empty or may fall back to broader scanning.

When assisting scene placement, gather these prompt inputs before editing:

- Target editor scene and target overlay id.
- Whether the object belongs in `DynamicRoot/Common` or `DynamicRoot/{overlay_id}`.
- Entity type, `CfgId`, world position, facing direction, and uniqueness needs.
- `AppearCond`, `DisappearCond`, respawn settings, dungeon node and spawn policy when relevant.
- Faction, movement behavior, enmity config, peace state, sensing/logical initialization flags.
- Patrol data such as named points, named paths, portal network id, or patrol cycle node ids.
- For dynamic spawners, member count, member ids, member entity configs, relative offsets, facing, and behavior.
- Placement safety: walkable ground, no `Hole`, no wall/decor collider overlap, and no conflict with born points, teleporters, or key interactables.

## Dream Infiltration

In this project, "入梦" always means the dream minigame entered through the dream facility in `secretbase`.

- NPC dialogue, quest options, map interactions, and cutscenes must never start a character dream directly.
- World content may unlock or hide a character dream entry through config-driven conditions owned by the relevant source of truth, such as a quest step, relationship state, or persistent character state.
- The player must return to `secretbase`, interact with the dream facility, and select an unlocked entry in `DreamEntryPanel` to start the minigame.
- Dream-related quest objectives must query persistent settlement results. Do not use a one-shot dream-finished event as the source of completion truth, and do not couple the dream system back to feature-specific quest APIs.
- Treat entry unlock, minigame execution, settlement persistence, and quest progress query as separate stages.

## UI Development

Build UI on top of the project's `PanelBase` pattern.

- UI prefabs live under `Assets/Resources/UI/Prefabs`.
- Prefer assembling UI elements in prefabs instead of dynamically generating UI in code.
- For UI elements used only inside one panel, such as a list item template, keep them inside that panel prefab as a template and connect them through serialized script fields.
- Do not create separate prefab assets for panel-private UI pieces unless reuse or complexity clearly justifies it.
- When using TextMeshPro, make sure the font setup supports Chinese text.

## Configuration

Use Luban for ordinary gameplay configuration.

- Config files are under `Config`.
- Excel headers define data structures.
- Prefer extending configuration only when the feature actually needs it.

## Design Discipline

Keep configurable surface area small by default, and expand it only when the implementation or design needs it.

After coding, review whether the design introduces intrusive coupling. Watch especially for:

- Feature-specific fields or methods added to generic structures.
- Generic workflows calling dedicated static methods from one specific feature.
- Shared systems gaining semantic knowledge that belongs in a feature module.

## Encoding

Create all new code files as UTF-8.

If modifying a non-UTF-8 source file, convert it to UTF-8 as part of the change.
