# Decisions Log

## 2026-03-13
- Decision: Make the project task-first, not chatbot-first.
- Why: The product only becomes useful if it manages real local tasks and planning well.
- Tradeoff: General small talk and broad agent behavior stay out of scope for MVP.

## 2026-03-13
- Decision: Use Unity for the desktop client and avatar layer, with a separate Python local backend for business logic.
- Why: Unity fits avatar rendering, animation, and UI playback; Python fits task logic, orchestration, and local AI adapters.
- Tradeoff: The product becomes a two-process app and needs a clear local contract.

## 2026-03-13
- Decision: Keep SQLite as the local system of record.
- Why: The app is single-user, local-first, and needs reliable persistence without extra infrastructure.
- Tradeoff: Cross-device sync is explicitly out of scope for MVP.

## 2026-03-13
- Decision: Keep LLM and speech runtimes behind backend-owned adapters.
- Why: The assistant needs one place for diagnostics, fallback logic, and provider switching across Groq, Gemini, Ollama, whisper.cpp, Piper, and ChatTTS.
- Tradeoff: Packaging, setup, and runtime validation become more complex.

## 2026-03-13
- Decision: Keep conversation history, selected date, focus task, and planner context in the backend.
- Why: Model runtimes should not be trusted to own application state.
- Tradeoff: The backend becomes the required authority for even simple chat flows.

## 2026-03-13
- Decision: Limit voice input to push-to-talk in MVP.
- Why: It reduces STT error handling, background listening complexity, and privacy concerns.
- Tradeoff: The assistant feels less ambient in the first release.

## 2026-03-13
- Decision: Use amplitude-based mouth-open lip sync for MVP.
- Why: It is enough to make the avatar feel alive without delaying the release on viseme systems.
- Tradeoff: Mouth movement quality will look less natural than phoneme-aware lip sync.

## 2026-03-13
- Decision: Natural-language task mutations must resolve to structured backend actions before writing to SQLite.
- Why: Free-form model output is too error-prone for create, complete, and reschedule flows.
- Tradeoff: Intent routing and action validation add extra implementation work up front.

## 2026-03-13
- Decision: Keep `agent-platform` optional and outside the MVP critical path.
- Why: The desktop assistant should run with Unity, the local backend, SQLite, and bounded AI or speech adapters only.
- Tradeoff: Any operator automation or extra orchestration must remain additive.

## 2026-03-14
- Decision: Document the current AI runtime as Groq or Gemini by default instead of claiming the repo is already local-only.
- Why: The implemented backend currently routes through cloud providers for LLM responses, while Ollama remains a future local path.
- Tradeoff: Documentation now distinguishes more clearly between target architecture and implemented behavior.

## 2026-03-17
- Decision: Keep facial blendshapes on the split `Body_Head` mesh instead of introducing a separate face-only mesh for the prototype.
- Why: The repo already uses a 15-region body split, `AvatarRootController` expects a single `faceMesh` renderer, and putting the 28 minimum shapes only on `Body_Head` keeps the facial pipeline simple without disrupting outfit hiding rules.
- Tradeoff: `FaceVariant` stays reserved for later, and any future alternate face mesh will need to preserve the exact same blendshape names.

## 2026-04-05
- Decision: Keep Phase 1 module-boundary work inside the current Unity tree and route Settings through `ISettingsModule` instead of letting `AssistantApp` own the settings controller and per-toggle mutation logic directly.
- Why: Shell, Planner, and Chat already have explicit module boundaries in the current repo, while Settings was still leaking controller and state-mutation ownership back into the main runtime coordinator.
- Tradeoff: `AssistantApp` still owns backend settings transport and remains the top-level orchestrator until later extraction phases shrink it further.

## 2026-04-05
- Decision: Land the first Phase 2 Presentation/Application split inside the existing feature folders instead of waiting for a future `src/modules/...` tree.
- Why: The repo already has stable feature boundaries for Home, Chat, Planner, and Settings, so moving quick-add wording, chat-turn request planning, planner task-mutation summaries, and outfit command contracts into focused application services reduces UI coupling now without inventing a new runtime layout.
- Tradeoff: `AssistantApp` still coordinates those application services for now, and the avatar outfit contract exists ahead of any shipped wardrobe UI.

## 2026-04-05
- Decision: Adopt a four-layer documentation hierarchy with explicit source-of-truth ownership and an ADR folder instead of leaving docs governance implied across scattered pages.
- Why: Phase 3 requires docs to change with code, and the repo already had current-state docs plus trackers but still lacked a single governance reference for hierarchy, ownership, and deeper decision rationale.
- Tradeoff: The repo now has more navigation and governance pages to maintain, but they reduce doc drift and make future architecture changes easier to trace.

## 2026-04-05
- Decision: Standardize placeholder-safe avatar customization around a registry-backed asset catalog instead of ad-hoc item arrays or folder discovery.
- Why: The repo already has item, preset, and conflict-rule data types, and Phase 4 needs one registered source of truth for allowed avatar assets without claiming production content is complete.
- Tradeoff: New avatar content now needs registry maintenance and validator updates, but the boundary is clearer and safer for future shell or wardrobe work.

## 2026-04-05
- Decision: Separate repo-wide agent rules from the reusable task protocol by keeping `AGENTS.md` as the machine-facing rule file and adding a dedicated agent workflow document plus completion protocol.
- Why: Phase 5 needs a consistent before-or-during-or-after coding workflow, explicit completion reporting, and tracker-aware scope control without overloading the active queue files.
- Tradeoff: The repo now has one more governance page to maintain, but the workflow is easier to audit and reuse across future tasks.
