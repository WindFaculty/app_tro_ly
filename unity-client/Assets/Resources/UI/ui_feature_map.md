# UI Feature Map - Design Target

## Warning

This file is a design target, not a statement of implemented reality.

- Not all elements described here exist in the current repo.
- The code remains the source of truth.
- Current implementation truth lives in:
  - `Assets/Resources/UI/MainUI.uxml`
  - `Assets/Resources/UI/Shell/AppShell.uxml`
  - `Assets/Resources/UI/Styles/*.uss`
  - `Assets/Scripts/Core/UiDocumentLoader.cs`
  - `Assets/Scripts/App/AppRouter.cs`
  - `Assets/Scripts/Features/`

## Purpose

Use this document to guide future UI polish work toward a more premium shell.
Do not use it to claim that the current app already has all of these screens, interactions, or dynamic panels.

## Target Layout Direction

The intended long-term shell direction is:

- a stronger top bar
- a more expressive left sidebar
- a richer center-stage presentation area
- a more dynamic right-side assistant panel

The current repo already has a three-region UI Toolkit shell, but several panels are still placeholders.

## Target-State Goals

Potential future polish areas include:

- stronger visual hierarchy in the top bar
- richer sidebar health and assistant identity presentation
- a more polished home-stage avatar presentation
- a real schedule surface instead of the current placeholder text panel
- a more useful right-side schedule assistant panel
- stronger settings presentation
- better shell styling and token usage

## Current-State Notes

- The active loader is `UiDocumentLoader`, not `UiFactory`.
- Current routing is handled by `AppRouter`.
- Current shell templates already include `HomeScreen`, `ScheduleScreen`, `SettingsScreen`, `ChatPanel`, `SubtitleOverlay`, and `ReminderOverlay`.
- `MainStyle.uss` is deprecated; active runtime styles are in `Styles/*.uss`.

## Migration Guidance

If future work uses this design target, treat it as polish on top of the existing shell rather than a rewrite from nothing.

Recommended approach:

1. Keep the existing `MainUI.uxml` and `AppShell.uxml` entry flow unless there is a proven reason to replace it.
2. Replace placeholder panels gradually with real dynamic content.
3. Preserve existing route names and screen wiring unless there is a concrete reason to change contracts.
4. Update docs and task trackers whenever design-target work becomes implemented code.
