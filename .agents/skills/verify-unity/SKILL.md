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

## Interaction Verification

현재 기능이 Click / Drag / Drop / Touch / UI Interaction을 포함하는지 확인한다.

포함한다면 자동 검증 결과를 다음처럼 분리한다.

- Logic Verification
- Runtime Integration Verification
- Human Interaction Verification

내부 메서드 직접 호출 테스트를
실제 사용자 입력 PASS라고 보고하지 않는다.

실제 입력 자동화가 불가능하면:

Status: HUMAN_VERIFY_REQUIRED

로 남기고 사용자가 직접 확인할 동작을 최대 3개로 정리한다.