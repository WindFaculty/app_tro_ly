# Architecture As-Is

Updated: 2026-03-14

This document describes the implemented repo state today, not the target assistant design.

## 1. Current Topology
- `unity-client/` is now the Unity client workspace for the assistant.
- `local-backend/` now exists as a dedicated FastAPI + SQLite backend for the assistant.
- `agent-platform/` is still an existing Python service and tool platform with its own APIs and docs.
- `docs/` and `tasks/` describe the assistant target and now track the active implementation milestones.

## 2. What Exists Today

### Unity side
- The Unity project now includes assistant runtime scripts under `Assets/Scripts/`.
- The client bootstraps a code-driven UGUI shell at runtime with:
  - task tabs
  - chat panel
  - backend-backed settings panel
  - degraded-runtime recovery guidance from health diagnostics
  - reminder text
  - avatar placeholder state handling
- A real production avatar asset is still not checked in.

### Python side
- `agent-platform/` still exists, but it is not the assistant MVP backend.
- `local-backend/` now implements:
  - `/v1` REST APIs
  - WebSocket events for reminders and assistant state
  - assistant streaming over `WS /v1/assistant/stream`
  - SQLite schema and repositories
  - task, planner, scheduler, conversation, and speech services
  - assistant orchestration with route selection, task validation, rolling summaries, long-term memory items, and route logs
  - rotating local log file output plus actionable health recovery actions
- Backend automated tests exist and pass in the current workspace.

### Documentation and planning
- Root docs now define the target assistant product, MVP scope, API contract, UI plan, and roadmap.
- Task tracking has been reset around the new assistant milestones.

## 3. Remaining Gaps
- No real avatar asset, animator controller, or blendshape face mesh is checked in yet.
- Whisper.cpp, Piper, and ChatTTS still depend on machine-local runtime setup or model download before end-to-end voice validation.
- The current default LLM route still depends on Groq and Gemini rather than a fully local Ollama path.
- Unity runtime has not been validated in the Unity Editor inside this terminal session.
- Windows packaging is scaffolded with scripts, but no final release build artifact is checked in.

## 4. Legacy or Adjacent Code Still Present
- `agent-platform/` still contains generic tool-platform code and some legacy integration assumptions.
- Those modules may remain useful later as optional tooling, but they should not be treated as the assistant core runtime.
- Voice runtime paths and release validation still depend on machine-local setup rather than checked-in assets.

## 5. Recommended Next Path
1. Validate the Unity client in-editor and replace the placeholder avatar with the real character asset.
2. Configure faster-whisper plus either Piper or ChatTTS on the target machine and smoke-test voice flows end-to-end.
3. Harden the Unity client scene/layout and migrate from runtime-generated placeholder UI to the intended production presentation where needed.
4. Keep `agent-platform/` optional until there is a real need for operator automation.

## 6. Known Gaps
- Backend flows are implemented and tested, but Unity-side integration still needs live validation.
- Voice runtime wiring is adapter-based and depends on external binaries not committed to this repo.
- Packaging scripts exist, but the final Windows build pipeline is not fully proven yet.
