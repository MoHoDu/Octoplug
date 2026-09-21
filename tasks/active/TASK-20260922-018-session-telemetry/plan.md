# Plan

## Context
Create a low-overhead local telemetry pipeline that can reconstruct one InfiniteMode session without changing gameplay or regressing room generation performance.

## Approach
1. Audit authoritative runtime events/values and public Sheet headers; never recompute paths or scan the scene for events.
2. Add serializable schema v1 models, anonymous/session IDs, stable runtime IDs, memory recorder, snapshots, checkpoint/final serializer, and latest-final artifact API.
3. Add minimal instrumentation at room/door/object/movement/connection/demand/reward/session boundaries and auto-bootstrap in InfiniteMode without manual setup.
4. Add diagnostics, mapping documentation, focused unit/integration/performance-guard tests, and generate a real test JSON.
5. Run Unity compilation/tests/harness checks, then request one human playthrough while status remains HUMAN_VERIFY_REQUIRED.

## Files
- Expected: new `Assets/01_Scripts/Telemetry*`, focused changes in authoritative gameplay controllers/registries, Editor tests, `docs/domains/telemetry.md`, `docs/tools/telemetry.md`.
- Serialized scene: avoid if runtime bootstrap is sufficient; otherwise only `InfiniteMode.unity` through its exact worktree Editor.

## Risks
- Cross-domain event ordering, application shutdown finalization, static lifetime cleanup, JSON compatibility, and accidental high-cost telemetry queries.

## Verification
- Compile, focused telemetry and touched-domain tests, JSON round trip/Korean text/failure isolation/latest-final behavior, integration bootstrap, no extra grid/render/path calls, `verify-fast`, `git diff --check`, actual generated JSON, human playthrough.
