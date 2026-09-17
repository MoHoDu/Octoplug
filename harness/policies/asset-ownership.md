# Asset Ownership

## Exclusive Assets

A Task must list every serialized/shared asset it may mutate. Exclusive Assets commonly include:

- `.unity` scenes and `.prefab` prefabs.
- Important `.asset` files and ScriptableObjects.
- `ProjectSettings/**` and package manifests.
- Shared UI, materials, render settings, and source art/audio.

`None` is valid only when the task will not mutate these assets.

## Ownership Rules

- Only one active Task may own an Exclusive Asset.
- Ownership covers nested prefab blast radius, not just the directly opened file.
- The owner must name the intended change in the Task plan.
- Do not expand ownership silently. Update the Task and obtain any required Human Decision first.
- Code references do not imply permission to edit the referenced asset.

## Octoplug High-Blast-Radius Assets

See `docs/architecture.md`. In particular, Cable, shared info prefabs, Room, resident/product UI, and `InfiniteMode.unity` require careful ownership.

## Editing Method

When a live matching Editor is reachable, use Editor operations rather than raw YAML edits. Verify hierarchy/component state before and after, save only approved assets, and report every changed serialized file.

## Handoff

The handoff records owned assets, whether each is saved/clean, what was verified visually, and whether ownership can be released.
