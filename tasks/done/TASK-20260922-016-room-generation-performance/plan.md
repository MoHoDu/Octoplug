## Investigation Plan
1. Reproduce a room promotion in the task Editor and collect frame/profiler evidence around `PromoteCurrentHint`.
2. Attribute time and allocations across room rendering, routing-grid rebuilds, wall/outlet planning, weighted product placement, path reachability, registry scans, and logging.
3. Remove redundant work first; then split safe work across frames only if synchronous reductions are insufficient.
4. Preserve room/content transaction semantics and event order.
5. Add focused regression/performance coverage and repeat the Room 1-5 scenario.

## Verification
- Unity compile and focused room-generation tests.
- Profiler comparison before/after using the same promotion scenario.
- `scripts/verify-fast.ps1` and human Play Mode verification.
