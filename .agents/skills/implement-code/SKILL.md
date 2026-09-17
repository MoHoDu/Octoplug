---
name: implement-code
description: Implement approved Octoplug C# within task scope.
---

# Implement Code

1. Read Task scope, relevant Domain Map, and `docs/standards/code-conventions.md`.
2. Confirm no unresolved decision blocks the behavior.
3. Reuse established code/contracts; if none exist, choose the smallest reversible design.
4. Keep domain logic separable from `MonoBehaviour` wiring where practical.
5. Add focused tests with new logic when a project test assembly exists or the Task establishes one.
6. Do not edit serialized Unity assets; hand off integration to `integrate-unity-editor`.
7. Run available compile/static tests and report `NO_PROJECT_TESTS` honestly.
8. Update todo, handoff, and Domain Map only for durable navigation changes.
