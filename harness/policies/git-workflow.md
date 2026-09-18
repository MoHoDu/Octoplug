# Git Workflow

## Baseline

- Default/base branch: `dev`.
- Task branch: `task/TASK-YYYYMMDD-NNN-slug`.
- Task ID: `TASK-YYYYMMDD-NNN`.

## Safety

- Inspect status before edits and preserve pre-existing changes.
- Never stage, unstage, commit, amend, reset, rebase, push, force, or discard work unless the user explicitly requests that action.
- Do not bypass hooks or signing.
- Do not include unrelated changes in a task diff.
- Avoid direct Unity YAML conflict resolution unless the task explicitly authorizes it and Unity merge tooling is configured.

## Change Boundaries

`Allowed Scope`, `Do Not Modify`, and `Exclusive Assets` in `meta.md` are authoritative for the Task. A discovered need outside scope becomes an Open Decision or follow-up Task.

## Completion

Before a commit is requested:

1. Run the Task verification.
2. Review staged and unstaged diffs separately.
3. Run `git diff --check`.
4. Confirm no secrets, generated Unity state, or unrelated user work are included.
5. Update the handoff and Domain Map when durable ownership or navigation changed.

## Commit / Push Rule

Task 완료 시:

1. git status 확인
2. git diff --stat 확인
3. 주요 diff 검토
4. Task 범위 외 변경이 있으면 분리하거나 사용자에게 보고
5. Task 문서 최종 갱신
6. commit
7. 현재 Task branch를 origin에 push
8. local HEAD와 remote HEAD 일치 확인
9. working tree clean 여부 확인

권장 commit 형식:

<type>: <task summary>

예:
feat: complete 8-direction cable routing
fix: resolve plug socket reconnection
ui: refine product info display

## Exception

다음 경우 자동 commit/push 하지 않는다.

- 사용자가 명시적으로 금지한 경우
- Merge conflict가 있는 경우
- Task 범위 외 변경이 섞여 있는 경우
- Secret / .env / 개인 설정이 포함된 경우
- Human Verification이 아직 필요한 경우
- 테스트 실패가 남아 있는 경우