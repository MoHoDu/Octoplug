# Plan

## Context

Finalized session telemetry already exists locally, but players cannot upload it or open a correctly prefilled feedback form. Add the optional production upload/submission path while leaving gameplay and authored UI untouched.

## Approach

1. Reuse `SessionTelemetryService.TryGetLatestCompletedSession` as the only artifact lookup.
2. Add centralized non-secret endpoint/form configuration, local/build secret providers, and Windows generated-config cleanup.
3. Add a testable upload client, durable per-session state store, URL builder/opener abstraction, and idempotent submission coordinator.
4. Invoke the coordinator once on Result entry and from the existing Lobby Survey button, isolating all failures.
5. Bind only the existing Lobby button through the isolated task Editor; add focused contract/controller/scene tests.
6. Run automated, Editor, Windows build, runtime, and secret-leak verification; remain human-blocked until requested scenarios pass.

## Files

- Expected modifications: new `Assets/01_Scripts/SurveySubmission/`, `Assets/Editor/SurveySubmission/`, `Assets/Tests/Editor/SurveySubmission/`; Lobby/Result controllers; GameFlow scene setup/tests; `Lobby.unity`; `.env.example`; ignore/docs/task context.
- Read-only references: `Assets/01_Scripts/Telemetry/`, `Assets/00_Scenes/Demo/Result.unity`, Apps Script and Google Form contracts from the approved request.

## Risks

- GameOver finalization and Result scene transition ordering.
- Duplicate upload/open from lifecycle re-entry or concurrent button presses.
- Generated token surviving a failed build or appearing in logs/diffs.
- Network/browser failures interfering with Result/Lobby behavior.

## Verification

- Exact request/response/URL/state tests, duplicate prevention, retry, no-session safety, token precedence/sanitization.
- Unity compile, focused and touched-domain tests, serialized binding checks, harness checks.
- Editor scenarios A-D and Development Windows Player end-to-end verification.
- Git status/diff/generated-output secret scan without printing the token.
