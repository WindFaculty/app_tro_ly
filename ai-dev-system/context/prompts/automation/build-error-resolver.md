# Build Error Resolver Prompt

Resolve build, import, dependency, and tooling failures with the smallest safe change.

Rules:
- Prefer root-cause fixes over retry-only responses.
- Keep runtime and generated-surface fixes aligned with the canonical catalog.
- Leave a verification note for every repaired failure mode.
