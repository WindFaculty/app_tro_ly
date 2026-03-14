# Test Plan - Local Desktop Assistant

Updated: 2026-03-13

## Target Automated Commands

Backend:

```powershell
cd local-backend
pytest -q
```

Unity:
- Run EditMode tests for data mapping, API client behavior, and view-model state.
- Run PlayMode tests for avatar state transitions, startup health flow, and reminder presentation.

## Smoke Tests
- ST1: Backend `GET /v1/health` reports `ready`, `partial`, or `error` without crashing.
- ST2: Task CRUD works against SQLite and survives app restart.
- ST3: `today`, `week`, `overdue`, `inbox`, and `completed` views return correct task grouping.
- ST4: Text chat can answer `What do I have today?` from real task data.
- ST5: Text chat can create, complete, and reschedule tasks through validated backend actions.
- ST6: Push-to-talk voice input returns a visible transcript and a correct routed action.
- ST7: TTS playback returns an audio asset and drives avatar `talking` state plus subtitles.
- ST8: Reminder polling emits a reminder event before a due task and the Unity client shows it.
- ST9: The app starts in offline mode with local runtimes only.
- ST10: Missing STT, TTS, or Ollama runtime degrades features cleanly without taking down the app.

## Functional Areas
- Data validation:
  - empty titles rejected
  - invalid time ranges rejected
  - contradictory schedule fields normalized or rejected
- Aggregation:
  - overdue logic
  - next-7-days grouping
  - due-soon detection
  - conflict detection
- Conversation:
  - intent routing
  - prompt context assembly
  - structured action application
  - conversation history persistence
- Voice:
  - STT transcription
  - transcript confirmation
  - TTS caching
  - temporary audio cleanup
- Unity:
  - startup health banner
  - avatar state transitions
  - reminder popup rendering
  - subtitle sync

## Manual Verification
- MV1: Add a task in the UI, restart the app, and confirm it persists.
- MV2: Ask for today's plan by text and confirm reply contents match the database.
- MV3: Speak a reschedule command and confirm both the transcript and resulting task update.
- MV4: Force TTS failure and confirm text replies still work.
- MV5: Set a task due within 15 minutes and confirm a reminder event appears.
- MV6: Launch with network disconnected and confirm the assistant still works locally.

## Packaging Checks
- Windows standalone build launches from a clean release folder.
- Startup script can bring up required local services in the right order.
- Release build reports missing runtimes with actionable guidance.

## Optional Secondary Checks
- If `agent-platform` later gains assistant-specific wrappers, add contract tests for task, chat, and speech tool calls there.
