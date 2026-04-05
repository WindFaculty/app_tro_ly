# P02 Manual Smoke Checklist

Updated: 2026-04-06

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

## P02a — Core Smoke (Ưu tiên cao — sign off trước)

Hoàn thành P02a trước để unblock A04, A07, A09, A10, A11 và đóng UI-1 đến UI-3.

| Flow | Result | Surface | Artifact / Note |
| --- | --- | --- | --- |
| Shell navigation (tab switching) | TODO | Unity Editor Game view or packaged window | Verify left rail routes to Home, Schedule, Chat, Settings without hanging. |
| Chat send | TODO | Unity Editor Game view or packaged window | Repo PlayMode coverage exists, but clean live sign-off still missing. |
| Backend ready state | PASS | Game view | `p02-20260329-e2-backend-ready-gameview.png` — prior evidence, reconfirm if needed. |
| Backend unavailable state | PASS | Game view | `p02-20260329-e1-backend-unavailable-gameview.png` — prior evidence. |
| Backend partial state | TODO | Game view or packaged window | Clean game view capture still needed after editor-state hardening. |
| Settings save | TODO | Unity Editor Game view or packaged window | Repo PlayMode `35 passed` covers it; clean live sign-off still missing. |
| Settings reload | TODO | Unity Editor Game view or packaged window | Same as above. |
| Settings dirty | TODO | Unity Editor Game view or packaged window | Same as above. |
| Subtitle overlay | TODO | Unity Editor Game view or packaged window | Repo PlayMode coverage exists, live sign-off missing. |
| Reminder overlay | TODO | Unity Editor Game view or packaged window | Overlay behavior still needs live sign-off. |

## P02b — Extended Smoke (Sau P02a — sign off để close UI-4 và A34)

Hoàn thành P02b để unblock A34, A31, A42 closure, A44 live-smoke closure.

| Flow | Result | Surface | Artifact / Note |
| --- | --- | --- | --- |
| Selected-date navigation | TODO | Unity Editor Game view or packaged window | Repo PlayMode coverage exists, but clean live sign-off still missing. |
| Direct complete | TODO | Unity Editor Game view or packaged window | Repo PlayMode coverage exists, but clean live sign-off still missing. |
| Inbox scheduling | TODO | Unity Editor Game view or packaged window | Repo-side follow-up landed 2026-03-29 and PlayMode re-verified; needs fresh clean rerun. |
| Character Space room overlay | TODO | Unity Editor Game view | Confirm selected-object card, current-activity strip, room action dock buttons, return-to-avatar behavior, and hotspot toggle all render and respond cleanly in the Home stage. |


## Closure Rule

P02 is closure-ready only when:

- editor smoke does not remain in `playmode_transition` for more than 20 seconds
- packaged partial capture includes at least one clean screenshot of the correct app surface
- repo-side `Inbox`, planner-owned center-screen routing, `Settings`, and chat-side interactions that were fixed on 2026-03-29 still behave correctly during the rerun
- every flow in the table above has a clean sign-off entry
- text visibility after reload or playmode restart has explicit evidence
- any remaining speech issue is tracked under `P03`, not hidden inside P02 wording
