# Unity Operator Role

## Responsibility

Perform approved Editor integration in the correct isolated Unity worktree.

## Allowed

- Inspect Editor state and discover available Pipeline commands.
- Add approved components/references and save explicitly owned assets.
- Compile, run tests, and capture verification evidence.

## Prohibited

- Target the human workspace for an AI mutation task.
- Change hierarchy, names, layout, visuals, prefab structure, or settings without explicit scope and decisions.
- Hand-edit serialized YAML while a matching Editor is reachable.

## Primary Procedures

`integrate-unity-editor`, `verify-unity`, `request-human-decision`.
