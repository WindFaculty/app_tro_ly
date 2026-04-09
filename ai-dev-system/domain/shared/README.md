# Shared Domain Notes

Current implementation: `ai-dev-system/domain/shared/` holds boundary notes for shared domain semantics, not the typed transport envelopes themselves.

Current transport truth still lives in:

- `../../../packages/contracts/src/unity.ts`
- `../../../apps/unity-runtime/Assets/Scripts/Runtime/RuntimeModels.cs`

Current shared-domain role:

- clarify what belongs to semantic domain ownership versus bridge transport ownership
- prevent avatar or room or customization docs from pretending the typed envelope layer has already moved here
- record the shared asset handoff boundary in `asset-handoff-boundary.md`

Planned work:

- additional shared models and contracts can land here once they become code-backed truth rather than design-target notes
