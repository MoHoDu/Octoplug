# Octoplug Harness Migration Map

## Scope

This map governs migration of the existing Unity repository to the AI-native harness described by `하네스_계획.md`. It does not authorize gameplay implementation or Unity content changes.

## Protected Baseline

| Existing | Target | Action | Reason |
|---|---|---|---|
| `docs/design/Octoplug 컨셉 기획서 최종.pdf` | Same path | Keep | Canonical design source; preserve staged Git LFS object byte-for-byte. |
| `UNITY_AI_HARNESS_GUIDE_v1.md` staged rename | `harness/references/UNITY_AI_HARNESS_GUIDE_v1.md` | Keep | Preserve the user's staged 100% rename and its content. |
| `하네스_계획.md` | Same root path | Keep | Current migration specification; leave untracked and unmoved. |
| `.gitattributes` | Same path | Keep | Existing Unity merge and Git LFS behavior. |
| `Assets/**` | Same paths | Keep | Read-only migration input; scenes, prefabs, art, and settings are out of scope. |
| `Packages/**` | Same paths | Keep | Unity Pipeline is already installed and current. |
| `ProjectSettings/**` | Same paths | Keep | No Unity setting change is required by the harness. |
| Existing empty harness directories | Same paths | Refine | Populate only with specified, useful content; do not add placeholder files. |

## Merge or Refine

| Existing | Target | Action | Reason |
|---|---|---|---|
| `.gitignore` | Same path | Merge | Preserve Unity rules and add only secret, local adapter, and harness-state exclusions. |

## Create

| Target | Action | Purpose |
|---|---|---|
| `AGENTS.md` | Create | Thin, model-neutral project entrypoint. |
| `CLAUDE.md` | Create | Thin Claude adapter to `AGENTS.md`. |
| `.env.example` | Create | Non-secret local configuration contract. |
| `.mcp.json` | Create | Portable project-level Unity MCP registration through a repository-relative wrapper. |
| `docs/architecture.md` | Create | Evidence-backed current architecture and boundaries. |
| `docs/domains/INDEX.md` and five initial maps | Create | Compact maps for design-backed domains only. |
| `docs/standards/code-conventions.md` | Create | Provisional conventions until project C# establishes precedent. |
| `harness/policies/*.md` | Create | Decision, isolation, ownership, Git, lifecycle, and context rules. |
| `harness/roles/*.md` | Create | Responsibility and authority boundaries. |
| `harness/mcp/README.md` | Create | Unity MCP setup, scope, and safety notes. |
| `tasks/TEMPLATE/*.md` | Create | Repeatable task state, scope, decisions, verification, and handoff. |
| `.agents/skills/*/SKILL.md` | Create | Short project procedures; no duplicate vendor Unity skill. |
| `scripts/*.ps1` | Create | Safe setup, task/worktree, MCP, and verification helpers. |

## Deferred

- Gameplay C#, assemblies, ScriptableObjects, tests, scenes, prefabs, ProjectSettings, and build configuration.
- CI configuration and automated builds.
- Google Sheets/balance-data integration, persistence, audio architecture, and player-facing UX behavior.
- Final namespace, asmdef, event, save-data, and data-authoring architecture.
- Personal or user-scoped MCP services and authentication state.
- Any domain map without stable design or implementation evidence.

## Open Decisions for Later Gameplay Tasks

- Connection validity, routing, capacity, overload, and recovery rules.
- Resident spawning, demand, satisfaction, and failure rules.
- Room generation, placement, adjacency, and unlock behavior.
- Reward formula, progression pacing, and Infinite Mode difficulty.
- Lobby/result flow, persistence, UI interactions, art direction, and audio direction.

These do not block harness construction. They block only the related gameplay implementation.

## Delete Candidates

None.

## Migration Guardrails

- No file deletion, staging, unstaging, commit, reset, or push.
- No changes under `Assets/**`, `Packages/**`, or `ProjectSettings/**`.
- New harness files remain unstaged unless the user explicitly requests otherwise.
- Machine paths, credentials, OAuth state, and Unity local tokens must not be tracked.
