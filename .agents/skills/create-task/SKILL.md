---
name: create-task
description: Create a scoped Octoplug task from the repository template.
---

# Create Task

1. Read `harness/policies/task-lifecycle.md` and `git-workflow.md`.
2. Inspect existing IDs in `tasks/active/` and `tasks/done/`; allocate `TASK-YYYYMMDD-NNN` without collision.
3. Copy `tasks/TEMPLATE/` to `tasks/active/<task-id>-<slug>/`; never overwrite.
4. Complete every field in `meta.md`, including Allowed Scope, Do Not Modify, Exclusive Assets, Human Decisions, and Open Decisions.
5. Use base `dev` and suggest `task/<task-id>-<slug>`.
6. Do not create a branch/worktree unless explicitly requested.
7. Confirm the new task files are the only changes produced.

Prefer `scripts/create-task.ps1 -WhatIf` before creation.
