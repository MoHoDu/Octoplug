## Technical Plan
1. Inspect WAV import metadata and authoritative Resident Demand/Plug lifecycle boundaries.
2. Add a reusable runtime one-shot SFX player that loads approved local clips and owns one non-spatial AudioSource without scene/prefab mutation.
3. Trigger `need_spawn` only when a new active Demand is actually created and `need_fail` only on an authoritative failed outcome.
4. Trigger `plug_connect` only after a connection is committed and `plug_disconnect` only after a connected Plug is actually detached.
5. Prevent duplicate playback from UI refreshes, topology notifications, retries, or no-op disconnect calls.
6. Add focused trigger tests and perform human audible verification.

## Verification
- Confirm all WAV files decode and import as AudioClip assets.
- Unity compile and focused Resident Demand/Plug tests.
- Human Play Mode check for each of the four one-shot triggers.
