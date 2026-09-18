# Plan

## Context

P0-1C already owns Plug input, cable routing, length limits, and invalid-drop recovery. P0-1D should add physical Plug ↔ Socket state only. Current connector components expose raw one-sided assignment methods, but no code pairs them. Socket GameObjects have authored `SocketConnector` references and no Collider2D; `InfiniteMode` currently contains no outlet or multitap instance, so human scene setup is required before runtime verification.

## Recommended Approach

1. Keep `PlugConnector` and `SocketConnector` as raw endpoint state. Add a small code-only connection arbiter that atomically maintains the symmetric invariant between both endpoints and rejects occupied Sockets.
2. Use distance-based nearest-Socket acquisition, not physics. Enumerate enabled `SocketConnector`s and accept only the nearest endpoint within the approved radius.
3. Subscribe `CableRoutingController` to `PlugDragInput.DragStarted`; disconnect the current pair immediately when a connected Plug starts dragging.
4. At the start of `OnDragEnded`, attempt Socket connection before existing invalid-drop recovery. Route to the nearest walkable cell adjacent to the Socket, append the exact Socket position through the existing orthogonal endpoint-jog helper, and reject paths beyond `CableLength`.
5. On success, place the Plug at the exact Socket transform, render that path, atomically pair endpoints, and update the last valid position. On occupied/out-of-range/no-Socket failure, preserve the existing P0-1C fallback.
6. Do not wire watts or `CablePowerFlowEffect.SetPowered`; physical connection is not yet electrical validity.
7. Human setup: place at least one wall-outlet prefab under `Connections/Wall_Outlets` and position it against a Room wall. This is not delegated to AI by the Task.

## Files

- Expected future modifications:
  - `Assets/01_Scripts/Power/Connection/PlugSocketConnection.cs` plus Unity metadata.
  - `Assets/01_Scripts/Power/SocketConnector.cs` only if a code registry is selected.
  - `Assets/01_Scripts/Power/Cable/CableRoutingController.cs`.
  - `docs/domains/connection-power.md` after implementation.
- Planning-only modifications now: this Task folder.
- Read-only references:
  - `Assets/01_Scripts/Power/PlugConnector.cs`, `SocketConnector.cs`, `CableInfo.cs`.
  - `Assets/01_Scripts/Power/Input/PlugDragInput.cs`.
  - `Assets/01_Scripts/Power/Routing/GridPathfinder.cs`.
  - Cable, wall-outlet, multitap, Room prefabs and `InfiniteMode.unity`.

## Risks

- Socket cells mounted on walls are blocked; connection must path to adjacent walkable space and use the exact Socket only as the final orthogonal endpoint.
- Adjacent Sockets are approximately `0.53` units apart; an acquisition radius at or above half that spacing is ambiguous.
- Half-updated Plug/Socket state is possible unless pairing is centralized and atomic.
- Runtime/Human verification is impossible until a Socket-bearing prefab is placed in the scene.
- No test assembly exists; project test result remains `NO_PROJECT_TESTS` unless a separately approved test architecture is added.

## Verification

- Logic: symmetric connect/disconnect, occupied rejection, idempotence, reconnect behavior, nearest-Socket selection, path-length rejection, and invalid-drop fallback.
- Runtime Integration: authored outlet Socket discovery, exact snapping, cable route to wall-mounted Socket, disconnect-on-drag-start, reconnect, and no Console errors.
- Regression: P0-1C drag/routing/length/wall/Door/invalid-drop checks and P0-1C.1 Base/Flow shared path and UV scroll checks.
- Human Play Check: connect, occupied rejection, disconnect, reconnect, and invalid drop with real mouse input.
