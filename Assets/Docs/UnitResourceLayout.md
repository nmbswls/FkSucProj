# Unit Resource Layout

Lightweight convention for unit presentation assets.

## Unique Units

Unique NPCs and monsters keep their presentation resources under their own folder:

```text
Assets/Resources/Prefab/Presentations/Npc/{unit_id}/
  {unit_id}.prefab
  Sprites/
  Anim/
  VFX/
  Audio/
```

Use the same `unit_id` prefix for local files:

```text
forest_leaf_slime.prefab
Sprites/forest_leaf_slime_left.png
Sprites/forest_leaf_slime_right.png
Anim/forest_leaf_slime_idle_loop.anim
```

When a prefab is nested in its unit folder, set `unit_npc.prefab_name` to the nested Resources path, for example:

```text
forest_leaf_slime/forest_leaf_slime
```

## Shared Families

Do not duplicate common clips for many units. Put reusable assets under a shared family folder:

```text
Assets/Resources/Prefab/Presentations/Npc/Shared/Slime/
Assets/Resources/Prefab/Presentations/Npc/Shared/Humanoid/
Assets/Resources/Prefab/Presentations/Npc/Shared/ForestSmallMonster/
```

## Animation

The project uses Animancer. Do not create AnimatorController assets for new unit content.

Allowed animation assets:

- `.anim` clips that Animancer can play directly.
- Scripted squash/stretch, rotation, bobbing, and sprite swapping driven by presentation code.
- Shared `.anim` clips where motion is generic.

Avoid:

- New `.controller` files for unit animation.
- Copying identical clips into every unit folder.
- Placing final unit runtime assets under `Assets/Arts/Generated`.

## Generated Assets

`Assets/Arts/Generated` is for previews and temporary generation output. Once accepted, move final unit sprites and clips into the corresponding `Assets/Resources/Prefab/Presentations/Npc/{unit_id}` folder.
