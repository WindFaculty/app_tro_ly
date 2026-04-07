# Task Queue - Desktop Assistant Rebuild

Updated: 2026-04-07
Status values: TODO | DOING | BLOCKED | REVIEW | DONE
Ownership: `Axx` = desktop app repo work | `Bxx` = Unity room runtime repo work | `Sxx` = integration sync repo work | `Pxx` = manual or off-repo work tracked in `tasks/task-people.md`

## How To Use This File

- Track only current repo-executable work here.
- Keep completion history in `tasks/done.md`.
- Use `tasks/task-people.md` for Unity Editor runs, target-machine checks, external credentials, approvals, external assets, or build artifacts.
- Use `docs/product/desktop-product-definition.md`, `docs/product/desktop-scope-v1.md`, and `docs/product/desktop-non-goals.md` as the A00 desktop scope baseline.
- Treat the older `RB-*` rebuild tracker and older modularization slices as historical planning context, not the active execution queue.

## Current Program State

- Current implementation: the active end-user runtime still lives in `ai-dev-system/clients/unity-client/` plus `local-backend/`.
- Current implementation: the canonical agent-platform core for the non-backend control plane now lives in `ai-dev-system/control-plane/catalog/`, `ai-dev-system/control-plane/orchestrator/`, and `ai-dev-system/control-plane/adapters/`, with generated surfaces under `.codex/`, `.agents/skills/`, and `.agent/`.
- Planned work: rebuild toward `Tauri Desktop Host + React/Vite Web UI + Unity Runtime + FastAPI Backend`.
- Workstream A and Workstream B may progress in parallel only after the shared execution surface is clear, and they must remain runtime-independent until the sync series.
- Workstream S is forbidden until `A15` and `B11` are both `DONE`.
- No new business UI belongs in Unity.
- No mini assistant mode or deep 3D interaction work belongs in pre-sync execution.

## Workstream A - Desktop App

- `A00 | DONE | Freeze desktop product definition. | Evidence: created docs/product/desktop-product-definition.md, docs/product/desktop-scope-v1.md, and docs/product/desktop-non-goals.md; refreshed docs/roadmap.md; reset the active queue to the A/B/S execution model.`
- `A01 | DONE | Reset desktop architecture. | Evidence: created docs/architecture/desktop-target.md, docs/architecture/desktop-runtime-flow.md, docs/architecture/local-storage.md, and ADRs under docs/adr/; locked ownership, runtime flow, config strategy, and the SQLite vs JSON split; refreshed docs/roadmap.md. | Depends on: A00`
- `A02 | DONE | Build repo skeleton and execution surface. | Evidence: added root package.json workspaces and repo-level rebuild scripts for apps/web-ui, apps/desktop-shell, and packages/contracts; added apps/web-ui/package.json and apps/desktop-shell/package.json check scripts; unified the default desktop and browser-preview backend URL at http://127.0.0.1:8096 in apps/web-ui/src/services/runtimeHost.ts and apps/desktop-shell/src-tauri/src/backend.rs; added scripts/validate_desktop_execution_surface.py; verified with python scripts/validate_desktop_execution_surface.py and npm run rebuild:check. | Depends on: A01`
- `A03 | DONE | Build Tauri desktop shell. | Evidence: extended apps/desktop-shell/src-tauri/src/backend.rs and apps/desktop-shell/src-tauri/src/lib.rs so the host now owns backend process state, startup status events, retry entry points, shutdown cleanup, shell runtime snapshots, and custom window-control commands; extended packages/contracts/src/tauri.ts plus apps/web-ui/src/services/runtimeHost.ts to expose those typed host surfaces; added desktop startup recovery and custom window chrome in apps/web-ui/src/components/StartupScreen.tsx and apps/web-ui/src/components/WindowChrome.tsx; updated apps/web-ui/src/components/ShellLayout.tsx and apps/web-ui/src/pages/StatusPage.tsx to surface host state; verified with npm run web:build, cargo check from apps/desktop-shell/src-tauri, and npm run rebuild:check. | Depends on: A02`
- `A04 | DONE | Implement local persistence and auto-restore. | Evidence: added apps/desktop-shell/src-tauri/src/persistence.rs plus new commands in apps/desktop-shell/src-tauri/src/lib.rs so the Tauri host now owns versioned JSON restore files, corrupted-file quarantine, and window-state capture or restore under the app data root; extended packages/contracts/src/tauri.ts plus apps/web-ui/src/services/runtimeHost.ts to expose typed desktop restore and persist surfaces with browser-preview localStorage fallback; updated apps/web-ui/src/App.tsx, apps/web-ui/src/components/ShellLayout.tsx, and apps/web-ui/src/pages/StatusPage.tsx so the shell restores the last route, auto-saves session state, and surfaces persistence diagnostics; verified with cargo test and cargo check from apps/desktop-shell/src-tauri, npm run web:build, python scripts/validate_desktop_execution_surface.py, and npm run rebuild:check. | Depends on: A03`
- `A05 | DONE | Build web UI shell and design system. | Evidence: rebuilt apps/web-ui/src/styles/globals.css into a shared desktop design system with new typography, tokens, surfaces, buttons, metrics, and utility primitives; expanded apps/web-ui/src/components/PageTemplate.tsx plus apps/web-ui/src/components/PageTemplate.module.css into the shared module-shell frame used by every page; refreshed apps/web-ui/src/components/ShellLayout.tsx plus apps/web-ui/src/components/ShellLayout.module.css and apps/web-ui/src/components/WindowChrome.module.css so the desktop shell now has the landed navigation, center-stage composition, and aligned chrome; restyled apps/web-ui/src/pages/DashboardPage.tsx, ChatPage.tsx, PlannerPage.tsx, WardrobePage.tsx, SettingsPage.tsx, and StatusPage.tsx so the existing backend-backed pages all consume the same design language; verified with npm run web:build, cargo check from apps/desktop-shell/src-tauri, and npm run rebuild:check. | Depends on: A03`
- `A06 | DONE | Build chat and assistant interaction module. | Evidence: replaced the A05 placeholder chat page with a real assistant workspace in apps/web-ui/src/pages/ChatPage.tsx, apps/web-ui/src/pages/ChatPage.module.css, apps/web-ui/src/features/chat/useAssistantWorkspace.ts, and apps/web-ui/src/services/assistantStream.ts so the desktop shell now supports backend-backed conversation threads, stream-first transcript state, retry, local search, diagnostics, structured action cards, and REST fallback; extended apps/web-ui/src/contracts/backend.ts plus apps/web-ui/src/services/backendClient.ts for typed chat-history and stream surfaces; added read-only chat history routes and persisted assistant-turn metadata in local-backend/app/api/routes.py, local-backend/app/services/conversation.py, local-backend/app/db/repository.py, local-backend/app/models/schemas.py, and local-backend/app/services/assistant_orchestrator.py so the UI can load real conversation threads and message history; refreshed docs/03-api.md to document the new current backend contract; verified with npm run web:build, npm run rebuild:check, and inline FastAPI TestClient verification from local-backend/.venv/Scripts/python.exe for /v1/chat/conversations plus /v1/chat/conversations/{conversation_id}. | Depends on: A04 A05`
- `A07 | DONE | Build planner, tasks, reminders, tags, and calendar views. | Evidence: replaced the placeholder planner page with a backend-backed planner workspace in apps/web-ui/src/pages/PlannerPage.tsx, apps/web-ui/src/pages/PlannerPage.module.css, apps/web-ui/src/features/planner/usePlannerWorkspace.ts, apps/web-ui/src/features/shell/DesktopSessionContext.tsx, and apps/web-ui/src/components/ShellLayout.tsx so the desktop shell now supports task create or edit or complete flows, today or week or inbox or overdue or completed views, derived reminders and tags views, a calendar lens, conflict summaries, and persisted planner-view restore; extended apps/web-ui/src/contracts/backend.ts plus apps/web-ui/src/services/backendClient.ts and packages/contracts/src/tauri.ts plus apps/web-ui/src/services/runtimeHost.ts plus apps/desktop-shell/src-tauri/src/persistence.rs so planner read-models, task mutations, and planner-view persistence are typed end-to-end; hardened local-backend/app/core/time.py and added local-backend/tests/test_tasks_api.py timezone-aware due-date regression coverage so `/v1/tasks/today` and `/v1/tasks/overdue` no longer crash on ISO timestamps with timezone offsets; verified with npm run web:build, npm run rebuild:check, and inline FastAPI TestClient verification from local-backend/.venv/Scripts/python.exe covering task create or update or complete or reschedule plus `/v1/tasks/today`, `/v1/tasks/week`, `/v1/tasks/overdue`, `/v1/tasks/inbox`, and `/v1/tasks/completed`. | Depends on: A04 A05`
- `A08 | DONE | Build notes, search, and personal knowledge module. | Evidence: added notes persistence plus note CRUD and personal-knowledge backend surfaces in local-backend/app/services/notes.py, local-backend/app/api/routes.py, local-backend/app/models/schemas.py, local-backend/app/db/repository.py, local-backend/app/container.py, local-backend/app/services/memory.py, and local-backend/app/services/tasks.py so the backend now exposes /v1/notes, /v1/memory/items, and /v1/tasks/active with SQLite-backed note records, link validation against tasks or chat conversations, and read-only memory retrieval; added regression coverage in local-backend/tests/test_notes_api.py; extended apps/web-ui/src/contracts/backend.ts plus apps/web-ui/src/services/backendClient.ts, apps/web-ui/src/App.tsx, and apps/web-ui/src/components/ShellLayout.tsx so the shell has typed notes and knowledge surfaces plus a dedicated notes route; built the real notes module in apps/web-ui/src/features/notes/useKnowledgeWorkspace.ts, apps/web-ui/src/pages/NotesPage.tsx, and apps/web-ui/src/pages/NotesPage.module.css with quick capture, richer note detail, task or chat linking, local cross-module search, and read-only personal knowledge cards; refreshed docs/03-api.md and docs/architecture/local-storage.md to keep current implementation docs aligned with the landed routes and SQLite scope; verified with npm run web:check, npm run web:build, npm run rebuild:check, inline FastAPI TestClient verification from local-backend/.venv/Scripts/python.exe covering notes plus memory plus active tasks, and py_compile verification for the new backend and test files. | Depends on: A04 A05`
- `A09 | TODO | Build Google email integration. | Depends on: A05 A14`
- `A10 | TODO | Build Google calendar integration. | Depends on: A05 A14`
- `A11 | TODO | Build browser automation module with multi-step approval. | Depends on: A05 A14`
- `A12 | TODO | Build push-to-talk voice system. | Depends on: A05 A06 A14`
- `A13 | TODO | Build wardrobe data system. | Depends on: A04 A05`
- `A14 | DONE | Build settings, privacy, diagnostics, and recovery center. | Evidence: replaced the A05 read-only placeholder in apps/web-ui/src/pages/SettingsPage.tsx plus apps/web-ui/src/pages/SettingsPage.module.css with a real settings center that now edits backend-backed voice, reminder, and memory defaults, persists local shell preferences, exposes local data-path and diagnostics visibility, and surfaces safe recovery actions; extended apps/web-ui/src/contracts/backend.ts plus apps/web-ui/src/services/backendClient.ts and docs/03-api.md so `/v1/settings` is typed end-to-end and now includes a reset flow; added backend reset support in local-backend/app/db/repository.py, local-backend/app/services/settings.py, local-backend/app/api/routes.py, and local-backend/tests/test_scheduler_and_health.py so persisted settings can be restored to runtime defaults; added desktop restore reset support in packages/contracts/src/tauri.ts, apps/web-ui/src/services/runtimeHost.ts, apps/desktop-shell/src-tauri/src/lib.rs, and apps/desktop-shell/src-tauri/src/persistence.rs so the host can rebuild restore files to defaults; updated apps/web-ui/src/components/ShellLayout.tsx, apps/web-ui/src/pages/StatusPage.tsx, and apps/web-ui/src/styles/globals.css so theme preferences apply live and host diagnostics visibility now affects the status lane; verified with npm run web:check, npm run web:build, cargo test from apps/desktop-shell/src-tauri, cargo check from apps/desktop-shell/src-tauri, py -3 scripts/validate_desktop_execution_surface.py, and inline settings-service reset verification from local-backend/.venv/Scripts/python.exe. | Depends on: A03 A04 A05`
- `A15 | TODO | Package and harden desktop app. | Depends on: A06 A07 A08 A09 A10 A11 A12 A13 A14`
- `A16 | DONE | Refactor the non-backend agent platform around a canonical catalog, shared workflow lifecycle, and generated Codex plus Antigravity surfaces. | Evidence: added ai-dev-system/control-plane/catalog/platform.json, ai-dev-system/control-plane/orchestrator/, ai-dev-system/control-plane/adapters/, docs/architecture/agent-platform-baseline.md, docs/architecture/ecc-inspired-gap-analysis.md, docs/architecture/target-agent-platform-architecture.md, generated .codex/ plus .agents/skills/ plus .agent/ surfaces, python -m unittest discover -s ai-dev-system/tests -p "test_*.py", python ai-dev-system/scripts/validate/validate_agent_platform_surfaces.py, and python ai-dev-system/scripts/sync-agent-surfaces.py --check.`

## Workstream B - Unity Room Runtime

- `B00 | TODO | Freeze room vision and art direction. | Unblocked by: A02`
- `B01 | BLOCKED | Reset Unity into standalone room runtime. | Depends on: B00`
- `B02 | BLOCKED | Build base avatar production pipeline. | Depends on: B01`
- `B03 | BLOCKED | Build room blockout and layout foundation. | Depends on: B00 B01`
- `B04 | BLOCKED | Build prop library and environment art pass. | Depends on: B03`
- `B05 | BLOCKED | Build lighting and atmosphere polish. | Depends on: B03 B04`
- `B06 | BLOCKED | Build avatar behavior runtime. | Depends on: B01 B02`
- `B07 | BLOCKED | Build lip sync and speech presentation runtime. | Depends on: B02 B06`
- `B08 | BLOCKED | Remove offline task viewer and lock offline mode scope. | Depends on: B01`
- `B09 | BLOCKED | Build wardrobe runtime foundation. | Depends on: B01 B02`
- `B10 | BLOCKED | Build camera direction and presentation system. | Depends on: B03 B05 B06`
- `B11 | BLOCKED | Package standalone Unity runtime. | Depends on: B05 B07 B08 B09 B10`

## Workstream S - Integration Sync

- `S00 | BLOCKED | Freeze sync contracts. | Hard gate: A15 and B11 must both be DONE`
- `S01 | BLOCKED | Build Tauri to Unity bridge. | Depends on: S00`
- `S02 | BLOCKED | Sync web UI context to Unity runtime. | Depends on: S01`
- `S03 | BLOCKED | Embed Unity runtime into desktop app. | Depends on: S02`
- `S04 | BLOCKED | Build mini assistant mode. | Depends on: S03`
- `S05 | BLOCKED | Build deeper room interactions. | Depends on: S03`

## Manual Gates And Future Unblocks

- `P02` remains the current manual smoke gate for the existing Unity-first runtime and packaged-client behavior.
- `P04` remains the production avatar asset handoff gate.
- `P11` remains the future Unity standalone build artifact gate for embed validation.
- `P12` remains the future typed React -> Tauri -> Unity bridge smoke gate.

## Next AI Focus

- `A09`, `A10`, `A11`, `A12`, and `A13` are now the remaining Workstream A feature builds before `A15`.
- `A12` is now unblocked by the completed settings and recovery center work in `A14`.
- `B00` can continue in parallel without breaking runtime independence.
