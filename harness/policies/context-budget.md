# Context Budget

## Default Context

Load only:

1. `AGENTS.md`.
2. Current Task `meta.md`, `plan.md`, `todo.md`, and `handoff.md`.
3. One relevant Domain Map.
4. One current-stage Skill.

Do not load `log.md`, the full concept PDF, every Domain Map, all policies, or all Skills by default.

## Search Order

1. Current Task and relevant Domain Map.
2. Exact named files and symbols.
3. Adjacent domain files.
4. Broader repository search only when needed.

## Warning Thresholds

- `AGENTS.md`: 120 lines.
- Domain Map: 150 lines.
- `SKILL.md`: 120 lines.
- Task `plan.md`: 80 lines.
- `handoff.md`: 30 lines.

Thresholds are warnings, not permission to omit required safety information. Prefer links and concise ownership facts over duplicated prose.

## Maintenance

Split or compress a document when agents routinely read irrelevant sections. Update Domain Maps when navigation changes. Keep historical detail in task logs, not entrypoints.
