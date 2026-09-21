# Task Metadata

- **ID:** TASK-20260922-017
- **Title:** Gameplay SFX Hooks
- **Status:** done
- **Owner:** MoHoDu
- **Agent:** Claude Code
- **Domain:** audio; resident-demand; connection-power
- **Base:** dev
- **Branch:** task/TASK-20260922-017-gameplay-sfx-hooks
- **Started / Updated:** 2026-09-22
- **Current Stage:** human verification
- **Current Skill:** qa-feature

## Allowed Scope

- Integrate `need_fail`, `need_spawn`, `plug_connect`, and `plug_disconnect` WAV files as non-looping one-shot SFX.
- Add reusable runtime audio playback and authoritative demand/plug hooks.
- Add focused exactly-once tests and Audio Domain documentation.

## Do Not Modify

- Demand scheduling/failure rules, plug rules, balance, progression, UI, visuals, source WAVs, scenes, prefabs, mixers, packages, or ProjectSettings.
- Do not invent gameplay events when an existing lifecycle API is authoritative.

## AI Setup Allowed

- Runtime-created persistent non-spatial AudioSource and focused test objects.

## Exclusive Assets

- `Assets/02_Resources/Resources/Audio/GameplaySfxCatalog.asset` (new).
- Existing WAV files are read-only inputs.

## Human Decisions

- 2026-09-22 (MoHoDu): map the four named WAVs to demand failure/spawn and plug connect/disconnect; all use one-shot playback.
- 2026-09-22 (MoHoDu): use a Resources `GameplaySfxCatalog` with direct clip references and no scene/prefab mutation.

## Open Decisions

- Use neutral 2D volume/pitch defaults; revise only if audible verification finds a problem.

## Verification State

- Automated focused tests PASS.
- 2026-09-22 Human audible verification: PASS.
- Verification: PASS
