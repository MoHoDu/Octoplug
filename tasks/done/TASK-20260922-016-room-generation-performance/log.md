# Task Log

## 2026-09-22
- User reported a severe frame drop and brief game freeze during Room generation. No cause was assumed.
- Static call-path inspection found each successful promotion performed two full `RenderState` passes, recreating every runtime door/wall state, plus three scene-wide routing-grid rebuilds.
- Added Profiler markers and changed the successful path to incremental promoted-room/new-door/new-hint rendering with two required grid rebuilds. Full rollback rendering remains unchanged.
- Unity compile, Production Room Generation 4/4, Room Placement 16/16, diff check, and fast harness checks passed.
- User reported verification PASS and explicitly requested completion, commit, and push.
