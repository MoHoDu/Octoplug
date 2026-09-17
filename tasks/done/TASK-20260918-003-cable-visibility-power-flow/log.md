# Task Log

## 2026-09-18

- Checked existing sorting orders before changing anything: Grid=-90, Floor=-1, Base Cable=0, Product/Multitap sprite=1, Wall=2/3. Base Cable was already numerically behind Product; there was simply no free integer between 0 and 1 for a new Flow layer, so Base moved to -1 to open up 0 for Flow. Product's own sorting order was not touched.
- `CablePathRenderer` extended (not replaced) with an optional `flowLine` field; `Render()` now applies the same simplified point list to both Base and Flow — one path computation, two renderers.
- New `CablePowerFlowEffect`: shows/hides the Flow LineRenderer and scrolls its material's texture offset. Owns no power logic — `SetPowered(bool)` is the only real entry point; `previewPowered` is an Inspector-only preview flag, explicitly documented as not a game rule.
- Reused the existing empty `ElectricEffects` child of `Line` for the new Flow `LineRenderer` — zero new GameObjects in `Cable.prefab`.
- New `Assets/02_Resources/Materials/CableFlow.mat`: same shader as the proven Base Cable material (`Sprites/Default`), textured with the existing `Assets/02_Resources/Art/Effect/Dot.png` (already wrap=Repeat), tiled ×6 — placeholder color/speed pending an art/design pass.
- Verified via PlayMode + reflection: Base/Flow LineRenderers get identical position counts and identical points from a single drag call; `previewPowered` toggling correctly enables/disables the Flow line.
- Re-ran the full P0-1C drag/routing regression (6/6 PASS) — no regression from these rendering-only changes.
- Confirmed via `git diff` that `Room.prefab` (Door Collider) and Product sprite prefabs are unchanged.

## 2026-09-18 (user verification follow-up)

- User confirmed: Base Cable renders behind Product — PASS.
- User could not find `previewPowered` in the Inspector. Diagnosed via runtime inspection: `CablePowerFlowEffect` lived on `ElectricEffects`, 5 levels deep (`TV/Connection/Plug/Line/ElectricEffects`) — genuinely hard to discover.
- Fix: moved `CablePowerFlowEffect` (component only, via `Cable.prefab`) up to the Cable root ("Connection" inside TV — 1 level under the Product), alongside the already-there `CableInfo`/`CableRoutingController`. `flowLine` still references the same LineRenderer on `ElectricEffects`, which was not moved. No new field, no logic change.
- Re-verified: field wiring intact, Base/Flow still receive identical paths, `previewPowered` toggle still works, full P0-1C drag/routing regression 6/6, console clean, `Room.prefab`/Product prefabs unchanged.
- Product/PowerStrip overlap alpha fade intentionally **not implemented** this round — recorded as a deferred TODO in `docs/domains/connection-power.md` for a later play-test-driven decision.

## 2026-09-18 (Runtime Rendering bug round)

- User reported: initial Cable invisible until first click; Flow visible only in Scene View while `Connection` selected, not in Game View; Flow pattern perpendicular to the cable.
- Diagnosed via live Play Mode inspection + Game View screenshots (`capture_game_view`):
  - `CableRoutingController` only ever called `pathRenderer.Render(...)` from `OnDragged`/`OnDragEnded` — never on `Start()`. Before any drag, both LineRenderers kept Cable.prefab's original authored 2-point mockup segment (`(0,0,0)`→`(1.3,0,1)`, in Plug-local space), which is why nothing visible was drawn between Origin and Plug, and why Flow (an even tinier fraction of that stub) was only noticeable when a user zoomed into it manually in Scene View.
  - Fix: added `CableRoutingController.RenderInitialPath()`, called from `Start()`, which routes Origin→the *current, unmoved* Plug position through the grid (reusing the existing `BuildWorldPath`/`GridPathfinder` — no new routing logic) and renders it immediately. An invalid authored position logs a warning and is left untouched, per the human-placement policy. `CableRoutingGridService.Grid`'s existing lazy-build guarantee (from the P0-1B fix) already makes the grid ready by the time this runs, regardless of component Start() order — no artificial delay needed.
  - Confirmed via screenshot: Base Cable and Flow both draw correctly from frame one now.
  - Separately (not the same bug): Flow's LineRenderer never had `widthCurve` copied from Base, so it rendered at Unity's default flat width — several times wider than the thin Base cable. Stretched across that excess width, the Dot texture read as a large, jagged, perpendicular-looking mass instead of small parallel dots. Fixed by copying `baseLine.widthCurve` to `flowLine.widthCurve` (kept the existing 0.7x `widthMultiplier`). Confirmed via a zoomed-in Game View screenshot: the flow segment now runs parallel to the cable, following its bends correctly.
  - Also corrected the UV scroll sign in `CablePowerFlowEffect` (`Update()`) so the pattern visually moves Origin→Plug (LineRenderer's Tile UV.x runs Origin→Plug; increasing `mainTextureOffset.x` samples toward higher UV, which visually slides content the other way) — reasoned from Unity's documented UV convention; exact perceived direction still needs a human's eyes since frame-by-frame automated capture couldn't reliably advance real Play Mode time.
- Re-verified: full P0-1C drag/routing regression 6/6, Base/Flow path-sharing test still passing, console clean, `Room.prefab` untouched.
- **Found, flagged, not fixed (out of scope):** the scene diff for `InfiniteMode.unity` includes an unrelated `TV.prefab` nested-UI PrefabInstance override block (RectTransform anchors/size/position all zeroed) that lines up with a pre-existing "Prefab mismatch: Transform vs RectTransform... some references might be lost" console warning already present before this session's edits began. This looks like Unity auto-repairing old, unrelated data on a scene save rather than anything from the Cable-rendering work; recorded for the user to review rather than silently accepted or reverted.

## 2026-09-18 (Power Flow animation bug round)

- User confirmed the previous three visual defects were fixed, but reported the visible, parallel Flow pattern remained stationary in Game View.
- Runtime inspection proved `CablePowerFlowEffect.Update()` was active, `previewPowered` enabled the Flow LineRenderer, the LineRenderer had a runtime material instance, and `mainTextureOffset.x` changed over time. `Dot.png` already used Repeat wrap mode, and the Editor was not paused.
- Controlled Game View captures with the runtime material forced to offsets `(0,0)` and `(0.5,0)` were byte-identical while using `Sprites/Default`. This isolated the defect to the shader: `Sprites/Default` does not apply `_MainTex_ST`, so changing Material tiling/offset cannot alter its rendered sprite texture.
- Minimal rendering-only fix: switched `CableFlow.mat` to `Universal Render Pipeline/Unlit` and configured transparent alpha blending. Retained the same Dot texture, ×6 tiling, orange placeholder color, LineRenderer path/sorting/width, and `CablePowerFlowEffect` API/logic.
- Repeated the controlled offset captures after the switch: the images differed visibly. Two natural Game View captures three seconds apart also differed, confirming the existing per-frame UV scroll now affects actual rendered output.
- Scroll remains negative in `CablePowerFlowEffect` so the visual pattern moves Origin→Plug under the LineRenderer's Origin→Plug Tile UV convention. Exact perceived direction remains a Human Play Check.
- A final full regression command was attempted after the material change but blocked by a temporary shell safety-classifier timeout. This is recorded as pending rather than silently counted as a pass.
- Product-overlap alpha fade remains deferred, and no Socket Connection work was started.

## 2026-09-18 (Final Human Play Check)

- User confirmed **PASS**: Power Flow continuously moves in Game View.
- User confirmed **PASS**: Cable is visible immediately before any interaction.
- User confirmed **PASS**: Base Cable/Flow sorting behind Product is correct.
- User confirmed **PASS**: Flow direction is Cable Origin → Plug.
- Product/PowerStrip overlap alpha fade remains **DEFERRED** by explicit user instruction.
- TASK-20260918-003 is complete and moved to `tasks/done/`.
