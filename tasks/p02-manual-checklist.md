# P02 Manual Smoke Checklist

Updated: 2026-04-04

Use this file for the next clean P02 sign-off pass.
This checklist feeds `P02` in `tasks/task-people.md` and the live-smoke closure for `UI-1` through `UI-4` in `tasks/task-queue.md`.

## Evidence Layers

- `Repo regression evidence`: automated backend or Unity tests and script output from this repo.
- `P02 live UI evidence`: Unity Editor Game view and packaged-client screenshots or logs for task, chat, subtitle, reminder, degraded, and restart behavior.
- `P03 speech evidence`: target-machine STT or TTS runtime install and end-to-end voice validation. Do not treat this as closed from repo-side smoke alone.

## Required Artifacts

- Ready Game view capture
  `unity-client/Assets/Screenshots/p02-20260329-e2-backend-ready-gameview.png`
- Partial Game view capture
  Existing partial evidence is still the older unstable pair at `unity-client/Assets/Screenshots/p02-20260329-e3-backend-partial-gameview.png` and `unity-client/Assets/Screenshots/p02-20260329-e3-backend-partial-gameview-after-wait.png`; rerun still needed for clean closure.
- Unavailable Game view capture
  `unity-client/Assets/Screenshots/p02-20260329-e1-backend-unavailable-gameview.png`
- Restarted Game view capture with text still visible
  `unity-client/Assets/Screenshots/p02-20260329-e6-ready-restart-gameview.png`
- Packaged ready window capture
  `ai-dev-system/logs/p02/20260329T204609Z/packaged-ready-window-after-dismiss.png`
  Additional 2026-04-04 auto capture with backend responding:
  `ai-dev-system/logs/p02/20260404-packaged-ready-clean-auto.png`
- Packaged partial window capture on the correct app surface
  `ai-dev-system/logs/p02/20260329T151939Z-auto/packaged-partial-window-clean.png`
- Packaged unavailable window capture
  `ai-dev-system/logs/p02/20260329T204609Z/packaged-unavailable-startup-window.png`
  Additional 2026-04-04 auto capture with backend stopped:
  `ai-dev-system/logs/p02/20260404-packaged-unavailable-auto.png`
- Backend health JSON and smoke output for the same pass
  `ai-dev-system/logs/p02/20260329T151939Z-auto/backend-health-partial-auto.json`
  Additional 2026-04-04 ready-health capture:
  `ai-dev-system/logs/p02/20260404-backend-health-ready-auto.json`
- Restarted Game view capture with text still visible
  Additional 2026-04-04 Unity GUI-agent surface capture after playmode toggle:
  `ai-dev-system/logs/gui-agent/20260404T161040Z-inspect-unity-editor/screenshots/surface-game.png`

## Manual Sign-off Table

| Flow | Result | Surface | Artifact / Note |
| --- | --- | --- | --- |
| Selected-date navigation | TODO | Unity Editor Game view or packaged window | Repo PlayMode coverage exists, but clean live sign-off still missing. |
| Direct complete | TODO | Unity Editor Game view or packaged window | Repo PlayMode coverage exists, but clean live sign-off still missing. |
| Inbox scheduling | TODO | Unity Editor Game view or packaged window | Manual validation on 2026-03-29 found the `Inbox` button on the Schedule screen was non-responsive. Repo-side follow-up landed later on 2026-03-29 and Unity PlayMode re-verified with `35 passed`, so this row now needs a fresh clean rerun instead of staying in fail-only state. |
| Chat send | TODO | Unity Editor Game view or packaged window | Repo PlayMode coverage exists, but clean live sign-off still missing. |
| Subtitle overlay | TODO | Unity Editor Game view or packaged window | Repo PlayMode coverage exists, but clean live sign-off still missing. |
| Reminder overlay | TODO | Unity Editor Game view or packaged window | Overlay behavior still needs live sign-off. |
| Settings dirty | TODO | Unity Editor Game view or packaged window | Manual validation on 2026-03-29 found the top-level `Settings` tab did not switch screens. Repo-side follow-up landed later on 2026-03-29 and Unity PlayMode re-verified with `35 passed`, so dirty-state behavior now needs a fresh clean rerun. |
| Settings reload | TODO | Unity Editor Game view or packaged window | Manual validation on 2026-03-29 found the top-level `Settings` tab did not switch screens. Repo-side follow-up landed later on 2026-03-29 and Unity PlayMode re-verified with `35 passed`, so reload behavior now needs a fresh clean rerun. |
| Settings save | TODO | Unity Editor Game view or packaged window | Manual validation on 2026-03-29 found the top-level `Settings` tab did not switch screens. Repo-side follow-up landed later on 2026-03-29 and Unity PlayMode re-verified with `35 passed`, so save behavior now needs a fresh clean rerun. |

## Closure Rule

P02 is closure-ready only when:

- editor smoke does not remain in `playmode_transition` for more than 20 seconds
- packaged partial capture includes at least one clean screenshot of the correct app surface
- repo-side `Inbox`, planner-owned center-screen routing, `Settings`, and chat-side interactions that were fixed on 2026-03-29 still behave correctly during the rerun
- every flow in the table above has a clean sign-off entry
- text visibility after reload or playmode restart has explicit evidence
- any remaining speech issue is tracked under `P03`, not hidden inside P02 wording
