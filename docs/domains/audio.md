# Audio Domain Map

## Runtime Ownership

- `Assets/01_Scripts/Audio/GameplaySfxCatalog.cs` owns references to approved gameplay one-shot clips.
- `Assets/01_Scripts/Audio/GameplaySfxPlayer.cs` loads the catalog from `Resources/Audio/GameplaySfxCatalog` and plays clips through one persistent, non-spatial runtime `AudioSource`.
- `Assets/02_Resources/Resources/Audio/GameplaySfxCatalog.asset` is the version-controlled runtime catalog.
- Source WAV files remain under `Assets/02_Resources/Sound/` and are not runtime lookup authorities by filename.

## Authoritative Triggers

- Resident Demand creation plays `need_spawn` immediately after `ResidentDemandState.StartDemand` commits.
- Resident Demand failure plays `need_fail` when `Advance` returns a failure `DemandOutcome`.
- A new Plug/Socket pairing plays `plug_connect` after both connector references commit.
- An explicit disconnection plays `plug_disconnect` only when an existing pairing was removed.

No-op connection calls, rejected connections, repeated disconnects, topology-only notifications, assignment refreshes, and UI refreshes are silent.

## Current Boundaries

- Playback is one-shot, non-looping, neutral volume/pitch, and 2D.
- No AudioMixer, scene object, prefab, balance rule, or gameplay event is owned by this domain.
- Loudness, priority, and mixing changes require a new human decision if audible verification identifies a need.
