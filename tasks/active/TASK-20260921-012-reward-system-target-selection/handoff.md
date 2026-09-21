# Handoff

## Runtime Equipment
- Room and Reward equipment use one staged/finalized path. Rejected clones stay undiscoverable; accepted objects activate, synchronize physics, register room ownership, and notify topology.
- Reward target discovery reads the live runtime registry.

## Wall Outlet Rule
- RW007 and its target/effect paths are removed.
- Every seed/promoted gameplay-active room requires exactly one Wall Outlet before content readiness.
- RoomConfig socket min/max and weights `1:60, 2:25, 3:10, 4:4, 5:1` remain unchanged.

## Product Generation Fix
- Root cause: placement returned the first footprint-valid cell, then aborted the Product if that one cell failed routed reachability.
- `DefaultRoomContentBalance` remains the validated local Google Sheet-derived RC001–RC007 runtime catalog; no runtime Sheet access or sync was added.
- Placement now continues through later candidates and requires room containment, Walkable/free grid cells, no positive-area authored-bounds overlap with active Products/PowerStrips, and `CableRouteReachability` to a room-owned usable outlet within initial CableLength.
- Accepted Products reserve immediately, so same-batch Products cannot overlap even without a physics frame.
- The selected RoomConfig must finalize its exact Product type/count composition. Any shortfall rolls back objects created by that room-content call and suppresses `RoomContentReady`.

## Other Implemented UX
- RW009 uses clicked-point placement with bounded correction and selective dimming; additional overlap validation remains.
- Product Tooltip closes on pointer-down outside its panel while preserving inside interaction.
- Alert supports persistent selection instructions with transient override/restoration.

## Runtime Outlet Connection Fix
- Root cause: gameplay had retained a separate terminal-approach implementation from generation. Runtime lifecycle evidence ruled out missing prefab metadata, socket activation, room ownership, approach rotation, physics sync, and final grid refresh.
- Runtime and authored outlets use the same production prefab and terminal metadata. The generated socket is active, room-facing, present in the final grid, and resolves through normal magnetic candidate selection.
- Gameplay now delegates terminal routing to `CableRouteReachability`, matching Product-generation reachability. The route stays on Walkable/Door cells and appends the exact Socket position only as the terminal endpoint; ordinary Walls remain blocked.
- A narrow `TryCommitSocketDrop(pointer)` seam executes the existing production candidate/routing/graph/power/connect transaction without injecting a Socket or bypassing validation.

## Automated Verification
- Exact Unity `6000.3.9f1` compilation passed.
- Focused production-lifecycle test passed: generated terminal recognition, inward approach, shared route, magnetic candidate, exact endpoint, connection, powered state, disconnect, and reconnect.
- Full EditMode suite: `103/103` passed, `0` failed, `0` skipped. Existing authored-terminal and blocked-Wall/Door regressions remain green.
- `verify-fast.ps1` passed. `verify-harness.ps1` remains blocked by its Unity-content allowlist for the broader active Task changes.
- `verify-task-context.ps1` reports WARN only: active-task metadata is at warning thresholds but below hard limits.
- `git diff --check` passed with the existing LF→CRLF warning for `SessionProgressIntegrationTests.cs`.

## Remaining
- Focused Human Verification only: generate a room; confirm its Product and Wall Outlet; connect the Product Plug and verify magnetic correction, cable, and power; disconnect and reconnect.
- Door/Outlet exclusion and RW009 overlap remain pending but were not modified in this focused fix.
- Google Sheet sync remains out of scope.

Status: `HUMAN_VERIFY_REQUIRED`. Do not commit, push, or mark DONE.
