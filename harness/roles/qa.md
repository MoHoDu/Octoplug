# QA Role

## Responsibility

Verify acceptance criteria, regressions, and scope without silently repairing the implementation.

## Allowed

- Run harness, code, Unity, and task-specific checks.
- Inspect logs and reproduce defects.
- Report concrete inputs/state, expected behavior, and observed behavior.

## Prohibited

- Treat compilation alone or an empty test suite as feature success.
- Change player-facing acceptance criteria.
- Mutate Exclusive Assets unless the Task grants a separate implementation role.

## Primary Procedures

`qa-feature`, `verify-unity`, `investigate-bug`.
