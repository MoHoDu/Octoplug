# MCP Configuration

## Project-Required Server

`.mcp.json` defines only `unity-editor-mcp`. It runs `scripts/unity-mcp.ps1`, which resolves the current repository/worktree root and starts:

```text
unity mcp --project-path <current-repository-root>
```

This avoids committing a developer-specific absolute path and ensures a task worktree targets its own Unity project.

Claude Code requires a user-local approval before it connects to a project MCP server. Review `.mcp.json` before approval. Do not approve a changed command blindly.

## Safety

- Multiple Unity Editors may be open. Every direct `unity status`, `unity command`, or test/integration invocation must also pass the explicit task `--project-path`.
- Before mutation, confirm the connected project path is the isolated task worktree, not the human workspace.
- Use read-only calls first: Pipeline inventory, status, tool discovery, and open scenes.
- Never test connectivity by creating, renaming, saving, importing, entering Play Mode, or mutating a Unity object.
- Do not put Pipeline local tokens, OAuth credentials, headers, or API keys in `.mcp.json` or `.env`.

## Local Tooling

The official Claude Unity plugin and Unity CLI are user-installed tooling. Their versions are verified during setup, not vendored into this repository. `com.unity.pipeline` remains a Unity package dependency.

If MCP is unavailable, use Unity CLI/Pipeline directly with an explicit project path. A sandbox may hide a genuinely running Editor; consult `unity pipeline list` and do not assume the Editor is closed from `unity status` alone.

## User-Scoped Services

Personal Notion, Drive, Figma, browser, search, and other productivity MCP servers remain user-scoped. Their auth state and commands must not be copied into this project. Stale or failed user MCP entries are local maintenance, not repository migration.

## Verification

After local approval or a Claude Code restart:

```text
claude mcp get unity-editor-mcp
claude mcp list
```

Then use read-only Unity status and scene-list calls for the explicit project. If the Editor is not visible, record the limitation and leave Unity content untouched.

## Task Verification

Run the reusable task verifier from the exact task worktree:

```powershell
pwsh -File scripts/verify-task.ps1 `
  -ProjectPath D:\github-worktrees\Octoplug\TASK-20260918-007-ui-integration
```

A matching ready Editor must already be open with Play Mode stopped and every open scene clean. The command builds the generated `.slnx`, recompiles through Unity Pipeline, discovers and runs the official PowerStrip and Wall Outlet runtime-upgrade EditMode suites under `Assets/Tests/Editor`, tracks only the new Console entries without clearing history, and finishes with `verify-fast.ps1`.

The default `RuntimeUpgradeTests` filter runs both suites and reports their counts separately: PowerStrip 4 and WallOutlet 7 (11 total at the current TASK-007 checkpoint). Use `-TestFilter <fixture-or-test-name>` only for a targeted rerun. Every official test reuses `UnityVerificationFixture`, authored prefabs, public production upgrade APIs, and `PlugSocketConnection`; it must not fabricate private serialized state or toggle authored children directly.

The summary must report Console continuity explicitly. A reset, dropped entries, or session mismatch is `Console Delta: UNRELIABLE` and fails Overall; a successful run requires a reliable same-session delta and zero new Console errors.
