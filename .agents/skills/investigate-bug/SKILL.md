---
name: investigate-bug
description: Reproduce and isolate an Octoplug defect before fixing it.
---

# Investigate Bug

1. Record concrete inputs/state, expected result, and observed result.
2. Read the Task, relevant Domain Map, and narrowest implicated files.
3. Reproduce without mutating unrelated assets or masking errors.
4. Distinguish code defects, serialized-data defects, design ambiguity, and environment/tool failures.
5. Form and test the smallest hypotheses; record discarded paths in `log.md`.
6. If expected behavior is unclear or player-facing, use `request-human-decision`.
7. Add a regression test when feasible, then apply the smallest scoped fix.
8. Verify the original scenario and nearby regression boundary.
