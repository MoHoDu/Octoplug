---
name: qa-feature
description: Validate an Octoplug feature against approved acceptance criteria.
---

# QA Feature

1. Read the Task acceptance criteria, decisions, Domain Map, and handoff.
2. Derive checks for happy path, boundary state, failure/recovery, and nearby regressions.
3. Use the correct worktree and explicit Unity project path.
4. Run automated checks first, then approved manual/visual checks.
5. Do not change acceptance criteria or silently repair findings.
6. Report each issue as state/input → observed result → expected result, with evidence.
7. Mark untestable criteria and missing tests explicitly.
8. Update verification state and next action in handoff.
