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
