# Phase 2 Layering Slice

Updated: 2026-04-05
Status: Current implementation notes for the first Presentation/Application/Domain split inside the existing Unity tree

This page documents the landed Phase 2 slice from `tai_cau_truc_tach_logic.md`. It describes only what exists in repo now.

## Layering Convention

- Presentation:
  - UI Toolkit controllers and module boundaries under `unity-client/Assets/Scripts/Features/`
  - accepts input, renders view state, and publishes feature events
- Application:
  - feature-owned use-case services that shape commands, transport plans, and action summaries
  - coordinates existing backend integrations or avatar runtime adapters
- Domain or infrastructure already owned elsewhere:
  - backend task rules stay in `local-backend/app/services/`
  - planner transport mapping stays in `unity-client/Assets/Scripts/Tasks/PlannerBackendIntegration.cs`
  - avatar outfit rules stay in `unity-client/Assets/AvatarSystem/Core/Scripts/AvatarEquipmentManager.cs` and `AvatarPresetManager.cs`

## Landed Flows

### Quick Add From Home

- Presentation:
  - `unity-client/Assets/Scripts/Features/Home/HomeScreenController.cs`
  - `unity-client/Assets/Scripts/Features/Home/HomeModule.cs`
- Application:
  - `unity-client/Assets/Scripts/Features/Home/HomeQuickAddApplicationService.cs`
- Current implementation:
  - the Home UI now emits raw quick-add input instead of formatting `"Add task ..."` inside the controller
  - quick-add command wording and quick-add status wording now live in the Home application service

### Chat Submit Routing

- Presentation:
  - `unity-client/Assets/Scripts/Features/Chat/ChatModule.cs`
  - `unity-client/Assets/Scripts/Features/Chat/ChatPanelController.cs`
- Application:
  - `unity-client/Assets/Scripts/Features/Chat/ChatTurnApplicationService.cs`
- Current implementation:
  - `AssistantApp` still owns runtime coordination, but it now asks the chat application service to build streaming versus compatibility request plans instead of formatting those payloads inline

### Planner Task Mutation

- Presentation:
  - `unity-client/Assets/Scripts/Features/Schedule/PlannerModule.cs`
  - `unity-client/Assets/Scripts/Features/Schedule/ScheduleScreenController.cs`
- Application:
  - `unity-client/Assets/Scripts/Features/Schedule/PlannerTaskCommandApplicationService.cs`
- Domain or infrastructure:
  - `unity-client/Assets/Scripts/Tasks/PlannerBackendIntegration.cs`
- Current implementation:
  - complete-task and inbox-schedule summaries now come from the planner application service instead of `AssistantApp`

### Avatar Outfit Contract

- Application:
  - `unity-client/Assets/Scripts/Avatar/AvatarOutfitApplicationService.cs`
- Domain or infrastructure:
  - `unity-client/Assets/AvatarSystem/Core/Scripts/AvatarEquipmentManager.cs`
  - `unity-client/Assets/AvatarSystem/Core/Scripts/AvatarPresetManager.cs`
- Current implementation:
  - the repo now has an application-facing outfit contract for future UI or shell integration
  - this is not a shipped wardrobe UI; no runtime Home or Settings or Shell surface equips outfits yet

## Verification Notes

- Repo-side static evidence in this session includes the new EditMode test sources for Home, chat-turn planning, planner task commands, and avatar outfit application services.
- Unity compile or EditMode execution was not re-run in this session because no Unity Editor instance was available through MCP.
- Manual validation is still required for real client behavior under `P02`.
