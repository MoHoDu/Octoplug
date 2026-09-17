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
