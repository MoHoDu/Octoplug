# Human Decision Gate

## Purpose

AI may choose routine technical implementation details within an approved Task, but it must not invent player-facing intent.

## Human Decision Required

Stop the affected work and record a Human Decision when a choice changes:

- Gameplay rules, difficulty, balance, progression, rewards, or failure conditions.
- Player controls, interaction flow, confirmation behavior, accessibility, or UX.
- Scene composition, hierarchy meaning, prefab structure, shared UI, lighting, camera, or visual feedback.
- Art, animation, audio, wording, localization, or narrative direction.
- Product scope, priority, acceptance criteria, or destructive migration behavior.

Provide the current evidence, a recommended choice, viable alternatives, and concrete impact. Continue unrelated work that does not depend on the answer.

## Agent-Operable Decisions

Within Allowed Scope, AI may normally choose:

- Local naming, small refactors, error handling, and test structure.
- Reuse of established repository patterns.
- Tool invocation and verification order.
- Non-player-facing implementation details that do not create architectural lock-in.

Escalate technical decisions when they are costly to reverse, cross domains, alter public contracts, or affect serialized Unity data.

## Recording

- Put unresolved items in the Task's `Open Decisions`.
- Put answered items in `Human Decisions`, with date and decision owner.
- Update a Domain Map or decision record only when the answer becomes durable project knowledge.
- Never treat silence, a background notification, or a prior unrelated approval as authorization.
