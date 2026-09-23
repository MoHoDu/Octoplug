# Task Metadata
- **ID / Title:** TASK-20260923-019 / Telemetry Upload and Survey Submission
- **Status:** active
- **Owner / Agent:** MoHoDu / Claude Code
- **Domain:** telemetry; survey submission; game flow; build pipeline
- **Base / Branch:** latest dev / feat/survey-submission
- **Started / Updated:** 2026-09-23
- **Current Stage / Skill:** human verification / implement-code

## Allowed Scope
- Upload authoritative finalized telemetry to the fixed production `/exec` endpoint.
- Persist upload metadata, build encoded Form URLs, open from Result/Lobby.
- Inject token into Windows builds from environment or `.env.local`, with cleanup.
- Add focused tests, scene binding, documentation, and runtime/build verification.

## Do Not Modify
- Gameplay, balance, progression, rewards, telemetry schema, Form/endpoint contracts, replay, or analytics.
- Existing UI hierarchy/visuals.
- TASK-018 `Assets/01_Scripts/Telemetry/` or tests without ownership resolution.
- OAuth/Google credentials; never log or track the upload token.

## AI Setup Allowed
- SurveySubmission code/tests, existing Lobby Survey binding, ignored generated config, temporary Development build.

## Exclusive Assets
- `Assets/00_Scenes/Demo/Lobby.unity` — existing Survey binding only.
- GameFlow scene setup/tests; SurveySubmission runtime/editor/test paths.
- `.env.example` and generated-secret ignore rules.

## Human Decisions
- Use supplied production endpoint and exact Form mappings.
- Preserve Lobby/Result visuals and authored `Canvas/Buttons/Servey` name.
- Token must never be printed, documented, or committed.
- Use the separate worktree and requested branch.

## Open Decisions
- None. No new Lobby popup; no-session emits a safe diagnostic.

## Verification State
- Endpoint duplicate check, tests, scene binding, Windows build, cleanup, and standalone startup passed.
- Windows gameplay/browser verification remains pending.
- Verification: HUMAN_VERIFY_REQUIRED
