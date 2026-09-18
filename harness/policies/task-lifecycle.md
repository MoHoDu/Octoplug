# Task Lifecycle

## States

`proposed` → `planned` → `active` → `blocked` or `verification` → `done`.

A Task is a folder containing `meta.md`, `plan.md`, `todo.md`, `handoff.md`, and `log.md`.

## Start

1. Allocate `TASK-YYYYMMDD-NNN` and create from `tasks/TEMPLATE/`.
2. Fill scope, exclusions, domain, branch/base, Exclusive Assets, Human Decisions, and Open Decisions.
3. Read only `AGENTS.md`, current Task files except `log.md`, one relevant Domain Map, and the current-stage Skill.
4. Create an isolated worktree before Unity mutation.

## During Work

- `plan.md` describes the approved approach and verification.
- `todo.md` is the current executable checklist.
- `log.md` is append-only detail and is not default context.
- New player-facing uncertainty goes through the Human Decision Gate.
- New serialized/shared assets require explicit ownership.
- Keep handoff current enough that another agent can resume safely.

## Verification and Done

Move to `verification` only when implementation is complete and scope has not drifted. Move to `done` only when required checks pass or exceptions are explicitly recorded.

Move the whole Task folder from `tasks/active/` to `tasks/done/` without rewriting history. Do not claim missing tests passed; report `NO_PROJECT_TESTS`.

## Task Completion Gate

Task는 다음 조건을 모두 만족해야 DONE 처리할 수 있다.

- 구현 완료
- 자동 검증 완료
- 필요한 경우 Human Verification PASS
- todo / handoff / log 최종 갱신
- 관련 Domain / Decision 문서 갱신 필요 여부 확인
- git diff 검토
- Task 범위 외 변경 없음 확인
- 커밋 완료
- 원격 저장소 push 완료

Human Verification이 필요한 Task는
사용자 PASS 전까지 DONE 처리하거나 최종 commit/push 하지 않는다.