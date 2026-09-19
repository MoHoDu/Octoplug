# Task Context Compaction Policy

## Goal

Keep default-read task context small while preserving all historical information.

Stored information may grow.
Default-read information must remain small.

## Default-read task files

Agents may automatically read only:

- meta.md
- plan.md
- todo.md
- handoff.md
- current log.md

Files under history/ are cold context and must not be read unless needed.

## Size budgets

Recommended limits:

- meta.md: 40 lines
- plan.md: 100 lines
- todo.md: 90 lines
- handoff.md: 50 lines
- log.md: 130 lines

Hard compaction thresholds:

- meta.md: 50 lines
- plan.md: 120 lines
- todo.md: 110 lines
- handoff.md: 60 lines
- log.md: 160 lines

The total default-read task context should target <= 6,000 tokens.

## Compaction triggers

Compact when:

- a document reaches its hard threshold
- a major milestone completes
- Human Verification round completes
- a plan is superseded
- the Task continues for more than one major phase
- before /clear or handoff to a new agent
- context-budget verification warns repeatedly

## Preservation rule

Never delete historical information before preserving it.

Compaction order:

1. Copy/move historical content into history/.
2. Preserve original wording where useful.
3. Verify the archive file exists.
4. Rewrite the current document to contain only authoritative current state.
5. Add references to relevant archive files.
6. Never rely on summary alone when the original contained important evidence.

## Current vs historical truth

Current task files must contain only current authoritative information.

Superseded:
- plans
- values
- implementation approaches
- failed experiments
- old verification rounds

must move to history/.

Historical content must not override current state.

## handoff.md

handoff.md is a snapshot, not an append-only journal.

It should contain only:

- current status
- what is working
- current blockers
- remaining work
- exact next action
- important paths/branches

Target: <= 40 lines.

## plan.md

plan.md contains only the currently active plan.

Superseded plans must be archived.

Target: <= 80 lines.

## todo.md

Keep:

- incomplete items
- recently completed milestone items needed for context

Archive old completed checklists.

## log.md

log.md records significant events only.

Do not record:
- file opens
- grep history
- command exploration
- repeated identical verification
- chain-of-thought style reasoning

Archive completed milestone sections when the log grows.

## history/

Use descriptive immutable files, for example:

history/
- 001-powerstrip-stabilization.md
- 002-power-ui-binding.md
- 003-multitap-runtime-upgrades.md

Optional:
history/index.md

Agents do not automatically load history/.

## Compaction safety

A compaction must not:

- remove unresolved blockers
- remove current design decisions
- remove current Human Verification requirements
- lose evidence required for future debugging
- change authoritative values