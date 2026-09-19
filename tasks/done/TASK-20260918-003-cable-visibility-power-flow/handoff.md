# Handoff

## Status

- Current stage: DONE
- Last completed action: user completed the Human Play Check and confirmed all required Cable/Flow visuals PASS
- Next action: none for this Task; Product overlap alpha fade remains deferred

## Changed Files (this fix, on top of the prior P0-1C.1 commit content)

- Modified: `Assets/01_Scripts/Power/Cable/CableRoutingController.cs` — added `RenderInitialPath()`, called from `Start()`; no change to drag/drop/routing decision logic
- Modified: `Assets/01_Scripts/Power/Cable/CablePowerFlowEffect.cs` — flipped the UV scroll sign so the pattern visually moves Origin→Plug
- Modified (Unity-authored, via live Editor): `Cable.prefab` — Flow LineRenderer's `widthCurve` copied from Base (previously used Unity's default flat curve, causing an oversized/distorted flow shape)
- Modified (Unity-authored, via live Editor): `CableFlow.mat` — switched from `Sprites/Default` to transparent `Universal Render Pipeline/Unlit`; retained the existing Dot texture, ×6 tiling, orange placeholder color, sorting, and animation component
- No Routing, Input, CableLength, Room, Door, or Scene-layout code/data changed by the animation fix

## Verification

- **Logic/Runtime (PASS before the shader-only change):** Base LineRenderer has 6 points (a real route) before any drag/click; Flow shares the identical path; `previewPowered` toggle works; full P0-1C drag/routing suite passed 6/6; console was clean
- **Animation diagnosis (PASS):** `CablePowerFlowEffect.Update()` ran, the Flow renderer was enabled, and its runtime material offset changed, but screenshots at offsets 0 and 0.5 were byte-identical under `Sprites/Default`, proving that shader ignored `_MainTex_ST`
- **Animation fix verification (PASS):** after switching to transparent URP Unlit, forced-offset Game View screenshots differ; natural captures three seconds apart also differ, proving the existing per-frame UV scroll now changes rendered output
- **Final automated regression after the shader-only change:** not rerun at closure because Unity CLI calls remained blocked by the local safety-classifier timeout; earlier 6/6 regression evidence is retained but not misreported as post-fix execution
- **Human Interaction (PASS, 2026-09-18):** user confirmed continuous Game View Flow, initial Cable visibility, Product-behind sorting, and correct Origin → Plug direction

## Deferred TODO (unchanged, not mixed into this fix)

- Product/PowerStrip overlap alpha fade — see `docs/domains/connection-power.md`.

## Flagged, not fixed (out of scope for this task)

- `InfiniteMode.unity`'s diff includes an unrelated `TV.prefab` nested-UI PrefabInstance override (RectTransform anchors/size/position zeroed) matching a pre-existing "Prefab mismatch... some references might be lost" warning that predates this session. Please review before accepting (search the scene diff for `guid: 85b0eaeb6254de240bc8ee2aa321069c`).

## Resume Context

Task ownership can be released. Read this handoff when planning later Connection/Power work.

## Human Verification

- **PASS (2026-09-18):** Power Flow continuously renders in Game View, Cable is visible initially, Cable/Flow sorting behind Product is correct, and Flow moves Cable Origin → Plug.
