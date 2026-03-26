# Task People - Local Desktop Assistant

Updated: 2026-03-26
Status values: TODO | DOING | DONE | BLOCKED
Ownership: `Pxx` = manual or off-repo work | `Axx` = AI-executable repo work tracked in `tasks/task-queue.md`

## How To Use This File

- Track work that cannot be completed safely from terminal-only repo access.
- Use this file for Unity Editor runs, target-machine checks, runtime installs, external assets, credentials, approvals, and sign-off.
- Keep unblock lists aligned with the current AI queue.

## Current Manual Tasks

- `P01 | TODO | Open `unity-client/` in Unity Editor, run EditMode and PlayMode tests, and capture pass or failure evidence. | Done when: editor test outcomes are recorded clearly enough to guide follow-up fixes. | Unblocks: A04 A05 A07 A11 A14 A15`
- `P02 | TODO | Run manual client smoke tests for task flows, chat, reminders, subtitles, degraded mode, and packaged-client behavior. | Done when: the main user flows have been exercised manually and results are written down. | Unblocks: A04 A07 A09 A10 A11 A14 A15`
- `P03 | TODO | Install and configure speech runtimes on the target machine as needed for end-to-end validation. | Done when: the machine has working binaries, models, paths, and environment values for the intended STT and TTS path. | Unblocks: A10 A11 A12 A13`
- `P04 | TODO | Provide the signed-off production avatar asset, animator expectations, and lip-sync expectations. | Done when: the repo has the non-placeholder inputs needed for final avatar integration work. | Unblocks: A14 A15 A19`
- `P05 | DONE | Validate the packaged release folder on a clean Windows machine and record follow-up issues. | Historical note: this is no longer an active blocker. Evidence recorded earlier included a successful release-folder run and follow-up runtime observations.`

## Future Manual Prerequisites

- `P06 | BLOCKED | When calendar sync work starts, provide credentials, a test calendar, and project setup details. | Unblocks: A16`
- `P07 | BLOCKED | When automation work starts, provide accounts, environments, and Windows permissions for safe validation. | Unblocks: A17 A20 A21`
- `P08 | BLOCKED | When sync work starts, define device topology, network assumptions, and storage location. | Unblocks: A22 A23`

## Rule

- If a task needs Unity visuals, a target machine, external binaries, external credentials, or an external asset handoff, keep it here instead of forcing it into the AI queue.
