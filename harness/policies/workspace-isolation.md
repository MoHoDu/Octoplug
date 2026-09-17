# Workspace Isolation

## Rule

A human and an AI must not mutate the same Unity project directory concurrently. Unity worktrees are independent projects and each keeps its own generated `Library`.

## Human Workspace

The primary checkout is the human workspace. Harness setup may use it, but future gameplay or Unity Editor mutation tasks should run in a dedicated adjacent worktree.

## AI Worktree

- Default root: `D:/github-worktrees/Octoplug` (override locally through `WORKTREE_ROOT`).
- Branch: `task/TASK-YYYYMMDD-NNN-slug` from `dev` unless the Task says otherwise.
- Never share or copy `Library`, `Temp`, `Obj`, `Logs`, or `UserSettings` between worktrees.
- Open the worktree as its own Unity project and wait for import/compile completion.

## Unity Targeting

- Every Editor-driving CLI command must pass `--project-path` for the task worktree.
- MCP launches through the repository wrapper, which resolves the current worktree root.
- Before mutation, confirm the connected Editor project equals the Task workspace.
- If multiple Editors are open or targeting is ambiguous, stop rather than guessing.
- Sandboxed tooling may hide a live Editor; verify with the human or Pipeline inventory before falling back.

## Exit

Do not remove a worktree with uncommitted changes, unmerged commits, an open Unity Editor, or unresolved Exclusive Assets. Removal requires an explicit request and a clean safety check.
