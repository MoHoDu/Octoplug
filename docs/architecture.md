# Octoplug Architecture

## Current State

Octoplug is a Unity `6000.3.9f1` 2D project using Universal Render Pipeline `17.3.0` with the 2D Renderer, Input System `1.18.0`, and Unity Pipeline `0.7.0-exp.1`.

The repository currently contains a design/content skeleton rather than implemented game systems:

- No custom C# exists under `Assets`.
- No project assembly definitions or project test assemblies exist.
- `Assets/01_Scripts/` is empty.
- The four manager objects in `InfiniteMode` are structural mount points with no custom components.
- Unity Test Framework is installed, but there are no project tests.

Do not describe planned systems as implemented architecture.

## Product Direction

The canonical product source is `docs/design/Octoplug 컨셉 기획서 최종.pdf`.

Octoplug is a minimalist puzzle/simulation inspired by Mini Metro. The player supplies appliances in a shared house through wall outlets, cables, and multitaps while responding to resident demand. The highest-priority demo content is Infinite Mode.

Initial design-backed domains:

1. Connection / Power
2. Resident / Demand
3. Room Generation
4. Reward / Progression
5. Infinite Mode

Player-facing rules inside these domains remain subject to the Human Decision Gate.

## Scene Roles

- `Assets/00_Scenes/SampleScene.unity` — default URP 2D template and the only current build-settings scene.
- `Assets/00_Scenes/Demo/Lobby.unity` — sparse placeholder.
- `Assets/00_Scenes/Demo/Result.unity` — sparse placeholder.
- `Assets/00_Scenes/Demo/InfiniteMode.unity` — primary design/integration scene.

`InfiniteMode` contains the house/room layout, product and connection roots, resident and tooltip UI, and empty `GameManager`, `ConnectionDirector`, `ResidentController`, and `RoomGenerator` objects. Their names express intended responsibility, not an approved C# design.

Changing the build list, lighting, hierarchy, object names, UI layout, or serialized scene content is outside the harness migration.

## Asset Organization

- `Assets/02_Resources/Art/` — effect, house, plug, product, and UI artwork.
- `Assets/02_Resources/Font/` — project fonts.
- `Assets/02_Resources/Materials/` — material assets.
- `Assets/02_Resources/Sound/` — currently empty.
- `Assets/03_Prefabs/` — multitaps, products, rooms, UI, and wall outlets.

Despite its name, `Assets/02_Resources` is not a Unity `Resources` folder. Future code must not assume `Resources.Load` works for this path. Serialized references, a true `Resources` folder, or Addressables require a later technical decision.

## High-Blast-Radius Assets

The following shared assets affect multiple prefabs or the main demo scene and should normally be listed as Exclusive Assets:

- `Assets/03_Prefabs/Products/Cable.prefab`
- `Assets/03_Prefabs/Multitaps/PowerInfo.prefab`
- `Assets/03_Prefabs/Products/ProductInfo.prefab`
- `Assets/03_Prefabs/Products/UseInfo.prefab`
- `Assets/03_Prefabs/Rooms/Room.prefab`
- `Assets/03_Prefabs/UI/UI_ProductTooltip.prefab`
- `Assets/03_Prefabs/UI/UI_ResidentCard.prefab`
- `Assets/00_Scenes/Demo/InfiniteMode.unity`

## Implementation Boundary

Future work separates two stages:

1. **Code implementation** — pure C# and data contracts where possible.
2. **Unity Editor integration** — serialized references, components, scenes, prefabs, and visual validation.

Editor integration requires an isolated Unity worktree, explicit Exclusive Assets, and the `integrate-unity-editor` skill. Unknown gameplay, visual, audio, balance, progression, or UX intent requires a Human Decision before the affected change.

## Deferred Technical Decisions

- Namespace and assembly-definition layout.
- Test assembly and fixture placement.
- Events versus direct references.
- ScriptableObject and persistence strategy.
- Balance-data source and Google Sheets integration.
- Asset-loading strategy.
- Audio architecture.
- Lobby/result navigation and build-settings composition.
