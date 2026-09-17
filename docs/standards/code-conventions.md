# Provisional Code Conventions

Octoplug currently has no custom C# precedent. These conservative defaults keep the first implementation readable without pre-deciding gameplay architecture.

## C# and Unity

- Follow the repository's Unity Editor version and supported C# language level.
- One primary public type per file; filename matches the type.
- Use `PascalCase` for types, methods, properties, and public members.
- Use `camelCase` for parameters and locals; use `_camelCase` for private fields.
- Prefer private serialized fields over public mutable fields.
- Keep `MonoBehaviour` lifecycle methods small and delegate non-Unity logic to plain C# types where practical.
- Avoid per-frame allocations and scene-wide lookup calls in update loops.
- Check destroyed/missing Unity objects at integration boundaries and emit actionable errors.
- Comments explain intent, constraints, or Unity-specific hazards; do not narrate obvious code.

## Boundaries

- Keep domain logic separate from scene/prefab wiring where practical.
- Do not embed balance constants, player-facing text, asset paths, or scene object names without an approved data/authoring decision.
- Avoid static mutable global state unless a task explicitly justifies it.
- Do not use `Resources.Load` for `Assets/02_Resources`; it is not a Unity `Resources` folder.
- Treat serialized field renames as data migrations; use Unity migration attributes when appropriate and verify affected assets.

## Tests

- Add EditMode tests for plain domain logic when that logic is introduced.
- Add PlayMode tests only when Unity runtime behavior requires them.
- A missing test suite is `NO_PROJECT_TESTS`, not a pass.
- Test assembly layout will be chosen with the first implementation task; do not create empty assemblies for the harness.

## Deferred Until First Code Task

- Root namespace and folder-to-namespace mapping.
- Assembly-definition boundaries.
- Event bus, dependency injection, service location, or direct-reference policy.
- ScriptableObject/data-authoring policy.
- Async/coroutine strategy.
- Save-data format and versioning.

Record these as technical decisions when concrete code makes the trade-offs visible. Player-facing consequences still require the Human Decision Gate.
