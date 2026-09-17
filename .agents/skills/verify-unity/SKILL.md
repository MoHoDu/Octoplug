---
name: verify-unity
description: Verify Octoplug compilation, tests, and Editor state safely.
---

# Verify Unity

1. Use `scripts/verify-unity.ps1` with an explicit project path.
2. Confirm the connected Editor matches that path and is not in Safe Mode.
3. Check ready/compile/play/scene-dirty state without mutation first.
4. Compile only when the Task requires it and the workspace is isolated for AI mutation.
5. Run relevant EditMode/PlayMode tests when project tests exist.
6. Report no tests as `NO_PROJECT_TESTS`, never `PASS`.
7. Distinguish test failure, compile failure, timeout, auth failure, and missing Editor.
8. Store generated reports under ignored `.harness/local/` and summarize evidence in handoff.
