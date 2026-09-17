---
name: integrate-unity-editor
description: Integrate approved Octoplug changes through a targeted Unity Editor.
---

# Integrate Unity Editor

1. Read workspace isolation and asset ownership policies.
2. Require an isolated task worktree and explicit Exclusive Assets for mutation.
3. Run Pipeline inventory and target every command with the task `--project-path`.
4. Confirm the matching Editor is ready, not compiling, and on the expected project/scene.
5. Discover available commands; do not assume command names.
6. Inspect hierarchy/components before editing.
7. Apply only approved component/reference/asset changes and save only owned assets.
8. Reinspect, compile, and run applicable tests; capture visual checks when required.
9. List every serialized file changed and its clean/saved state in handoff.
10. Never hand-edit Unity YAML while a matching Editor is reachable.
