# Contracts Package

Current implementation:
- Shared TypeScript source of truth for desktop host and Unity bridge contract names.
- Canonical command/event list for the rebuild workstream Phase 6.

Manual validation required:
- Rust and Unity runtime must still be compiled and exercised on a target machine before these contracts can be treated as runtime-verified.

Planned work:
- Tighten code generation or schema export if the repo later needs stricter cross-language validation.
