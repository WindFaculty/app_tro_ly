# AI Runtime - Local Desktop Assistant

Updated: 2026-03-14

This document describes the AI stack implemented in the repo today. It is intentionally grounded in the current backend code, not the long-term local-only ideal.

## Core Principles

- The backend owns task state, session state, reminder state, and persistence.
- LLMs help with phrasing and deeper planning, but they do not write directly to SQLite.
- Every task mutation must go through validated backend actions.
- The assistant should degrade gracefully when LLM, STT, or TTS runtimes are unavailable.

## Implemented Execution Flow

1. The client sends a turn through `POST /v1/chat` or `WS /v1/assistant/stream`.
2. `AssistantOrchestrator` creates or resumes the conversation and assistant session.
3. `ActionValidator` classifies the request into a safe intent such as lookup, create, complete, reschedule, priority change, or planning.
4. `RouterService` chooses a response path based on routing mode, message complexity, notes length, planning keywords, voice mode, and provider health.
5. The backend composes the reply with:
   - `FastResponseService` for short voice-friendly answers
   - `PlanningService` for deeper structured reasoning
   - deterministic planner summaries when a task summary is enough
6. If task actions are required, `TaskService` applies them and emits task update events.
7. `MemoryService` refreshes the rolling summary and optionally stores long-term memory items.
8. The backend records route logs, task actions, and assistant state changes for the client.

## Current Route Types

- `groq_fast`
  - best for short queries, quick voice turns, and degraded fallback when the deep path is unavailable
- `gemini_deep`
  - used for long-context, planning-heavy, or more complex requests
- `hybrid_plan_then_groq`
  - builds a deeper plan first, then rewrites delivery into a shorter final answer

## Routing Inputs

- `assistant_routing_mode`
  - `auto`, `fast`, `deep`, or `hybrid`
- message complexity
- `notes_context` length
- planning keywords such as planning, strategy, optimize, or schedule-like phrases
- voice mode preference for low-latency answers
- provider health and recent provider failures

## Task Safety Contract

The current assistant is task-aware, not a general autonomous agent.

Supported validated intents today:

- day summary lookup
- week summary lookup
- overdue lookup
- urgency lookup
- free-time lookup
- create task
- complete task
- reschedule task
- increase priority
- planning from current task context

Important consequence:

- Natural-language model output is never trusted as a direct database command.
- The model can help explain or refine, but the backend decides what actually changes.

## Deterministic Planning Layer

`PlannerService` already provides deterministic summaries from real task data:

- `daily_summary`
- `weekly_summary`
- `overdue_summary`
- `urgency_summary`
- `free_slots`

These summaries feed both normal chat responses and deeper planning prompts.

## Memory Model

Short-term memory:

- recent messages for the active conversation
- rolling conversation summary persisted in SQLite

Long-term memory:

- optional auto-extraction from explicit user preference, routine, project, and goal statements
- simple token-overlap retrieval against stored memory items

Persistence tables used by the AI layer:

- `conversations`
- `messages`
- `assistant_sessions`
- `conversation_summaries`
- `memory_items`
- `route_logs`

## Streaming Assistant Path

`WS /v1/assistant/stream` supports:

- text turns
- partial transcript updates from streamed voice chunks
- final transcript confirmation on voice end
- live assistant state events
- chunked assistant text output
- sentence-level TTS readiness events
- final turn metadata including route, provider, token usage, and task actions

This path is the richest integration surface for the Unity client.

## Speech Runtime Notes

- STT defaults to `faster_whisper`.
- If `faster_whisper` is unavailable or fails, the backend can fall back to `whisper.cpp` when configured.
- TTS can use Piper or ChatTTS and caches generated audio by provider, text, and voice hash.

## LLM Runtime Notes

- Default LLM mode is `hybrid`.
- Default fast provider is `groq`.
- Default deep provider is `gemini`.
- Current LLM runtime is API-first and uses Groq or Gemini rather than Ollama.

## Current Limitations

- The default LLM path is not fully offline because it depends on Groq and Gemini.
- Intent parsing is still regex and keyword-driven rather than a full semantic parser.
- Memory extraction is intentionally narrow and conservative.
- Browser automation, desktop control, wake word, plugins, and cross-device sync are out of scope for the current implementation.
- Voice quality and end-to-end validation still depend on machine-local runtime setup for STT and TTS.

## Related Source Files

- `local-backend/app/services/assistant_orchestrator.py`
- `local-backend/app/services/action_validator.py`
- `local-backend/app/services/router.py`
- `local-backend/app/services/planning_engine.py`
- `local-backend/app/services/fast_response.py`
- `local-backend/app/services/memory.py`
- `local-backend/app/services/tasks.py`
- `local-backend/app/api/routes.py`
