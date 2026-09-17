# Octoplug Agent Guide

Octoplug is a Unity 6 2D minimalist puzzle/simulation about supplying shared-house appliances through outlets, cables, and multitaps while responding to resident demand. The repository currently holds design content and a Unity scene/prefab skeleton; custom gameplay C# and project tests are not established.

## Start Here

1. Read this file only; do not recursively load every link.
2. Locate the current folder under `tasks/active/`.
3. Read its `meta.md`, `plan.md`, `todo.md`, and `handoff.md`—not `log.md` by default.
4. Read one relevant map from `docs/domains/INDEX.md`.
5. Read one current-stage skill from `.agents/skills/`.
6. Check Open Decisions and Exclusive Assets before editing.

If no current Task exists, use the `create-task` procedure. Do not start gameplay or Unity asset work from an unscoped request.

## Project Facts

- Unity: `6000.3.9f1`; URP `17.3.0` with 2D Renderer.
- Unity Pipeline: `0.7.0-exp.1`.
- Base branch: `dev`.
- Priority demo scene: `Assets/00_Scenes/Demo/InfiniteMode.unity`.
- Canonical design source: `docs/design/Octoplug 컨셉 기획서 최종.pdf`.
- Current architecture: `docs/architecture.md`.

## Boundaries

- Follow `harness/policies/human-decision-gate.md` for gameplay, balance, progression, UX, scene/prefab, art, audio, and other player-facing choices.
- Follow workspace isolation and asset ownership policies before Unity mutation.
- Scenes, prefabs, shared UI, important `.asset` files, packages, and ProjectSettings require explicit Exclusive Assets.
- Never let human and AI mutate the same Unity project directory concurrently.
- Use an adjacent task worktree with its own `Library`; target every Editor command with its exact project path.
- Prefer a live matching Editor over raw `.unity`, `.prefab`, or `.asset` YAML edits.
- Do not stage, commit, push, reset, or discard work unless explicitly requested.
- Never expose or track secrets, OAuth state, local MCP credentials, or Unity Pipeline tokens.

## Policies

- Human decisions: `harness/policies/human-decision-gate.md`
- Workspace isolation: `harness/policies/workspace-isolation.md`
- Asset ownership: `harness/policies/asset-ownership.md`
- Git: `harness/policies/git-workflow.md`
- Task lifecycle: `harness/policies/task-lifecycle.md`
- Context budget: `harness/policies/context-budget.md`
- Click/Drag/Touch/UI 기능은 `harness/policies/runtime-interaction-validation.md`를 따른다.
- 구현 전 사람이 먼저 준비해야 할 Scene/Prefab/UI가 부족하면 `harness/policies/human-setup-required.md`를 따른다.

Roles live under `harness/roles/`. Policies take precedence over role descriptions and skills.

## Skills

Project procedures live under `.agents/skills/`. Do not copy or replace the user-installed official Unity CLI skill. The local `.claude/skills` adapter is created by `scripts/setup-links.ps1` and remains ignored.

## Verification

- Fast harness checks: `pwsh -File scripts/verify-fast.ps1`
- Full harness checks: `pwsh -File scripts/verify-harness.ps1`
- Unity checks: `pwsh -File scripts/verify-unity.ps1 -ProjectPath <task-worktree>`

No project tests must be reported as `NO_PROJECT_TESTS`, not as a pass. Record failures, skipped checks, and environment limitations faithfully in the Task handoff.

## Narrow Search Order

Current Task → one Domain Map → exact named files/symbols → adjacent domain → broader search. Load the concept PDF, all policies, all domains, or task logs only when the current question requires them.
