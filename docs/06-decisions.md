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
