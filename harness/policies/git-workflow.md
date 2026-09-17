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
